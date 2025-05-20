using HManagSys.Attributes;
using HManagSys.Models.Enums;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Users;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace HManagSys.Controllers;



public class AuthController : Controller
{
    private readonly IAuthenticationService _authService;
    private readonly IAuditService _auditService;
    private readonly IApplicationLogger _appLogger;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authService,
        IAuditService auditService,
        IApplicationLogger appLogger,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _auditService = auditService;
        _appLogger = appLogger;
        _logger = logger;
    }

    // ===== AUTHENTIFICATION DE BASE =====

    /// <summary>
    /// Affiche la page de connexion
    /// Point d'entrée principal du système
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si déjà connecté avec une session valide, rediriger
        var sessionToken = HttpContext.Session.GetString("SessionToken");
        if (!string.IsNullOrEmpty(sessionToken))
        {
            var validation = _authService.ValidateSessionAsync(sessionToken).Result;
            if (validation.IsValid)
            {
                return RedirectToLocal(returnUrl);
            }
        }

        // Préparer le modèle de vue avec des informations contextuelles
        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl,
            //RememberDevice = false // Pourrait être configuré selon les besoins
        };

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    /// <summary>
    /// Traite la tentative de connexion
    /// Coeur du processus d'authentification avec logging complet
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Capturer les informations de contexte pour l'audit
        var clientIp = GetClientIpAddress();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        try
        {

            // Tentative d'authentification avec logging automatique
            var result = await _authService.LoginAsync(model.Email, model.Password, clientIp);

            // Enregistrement détaillé de la tentative
            await _auditService.LogAuthenticationEventAsync(
                result.User?.Id ?? 0,
                AuthenticationEvent.Login,
                result.IsSuccess,
                clientIp,
                userAgent,
                result.ErrorMessage);

            if (result.IsSuccess && result.User != null)
            {
                // Vérifier si l'utilisateur doit changer son mot de passe
                if (result.RequiresPasswordChange)
                {
                    // Stocker temporairement l'ID pour le changement de mot de passe
                    HttpContext.Session.SetInt32("TempUserId", result.User.Id);
                    HttpContext.Session.SetString("TempUserEmail", result.User.Email);

                    await _appLogger.LogWarningAsync("Authentication", "ForcePasswordChange",
                        $"Utilisateur {result.User.Email} dirigé vers changement de mot de passe obligatoire",
                        result.User.Id);

                    return RedirectToAction("ChangePassword");
                }

                // Connexion réussie - procéder à la sélection de centre
                HttpContext.Session.SetInt32("UserId", result.User.Id);
                HttpContext.Session.SetString("UserEmail", result.User.Email);
                HttpContext.Session.SetString("UserName", $"{result.User.FirstName} {result.User.LastName}");

                await _appLogger.LogInfoAsync("Authentication", "LoginSuccess",
                    $"Connexion réussie pour {result.User.Email}",
                    result.User.Id);

                return RedirectToAction("SelectCenter");
            }
            else
            {
                // Gestion des échecs avec messages spécifiques
                if (result.AccountLocked)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Compte verrouillé. {(result.LockoutUntil?.ToString("HH:mm") ?? "Contactez un administrateur")}");
                }
                else
                {
                    ModelState.AddModelError(string.Empty,
                        result.ErrorMessage ?? "Email ou mot de passe incorrect");
                }

                // Logging de l'échec avec détails
                await _appLogger.LogWarningAsync("Authentication", "LoginFailed",
                    $"Échec de connexion pour {model.Email}: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la tentative de connexion pour {Email}", model.Email);

            //await _auditService.LogSystemErrorAsync(ex, "AuthController.Login",
            //    contextData: new { Email = model.Email, IpAddress = clientIp });

            ModelState.AddModelError(string.Empty,
                "Une erreur s'est produite. Veuillez réessayer ou contacter le support.");
        }

        // En cas d'échec, réafficher le formulaire
        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    // ===== SÉLECTION DE CENTRE =====

    /// <summary>
    /// Affiche l'écran de sélection de centre
    /// Interface critique pour le workflow multi-centres
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SelectCenter()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login");
        }

        try
        {
            // Récupérer les centres accessibles à l'utilisateur
            var centers = await _authService.GetUserAccessibleCentersAsync(userId.Value);

            if (!centers.Any())
            {
                await _appLogger.LogErrorAsync("Authentication", "NoCentersAssigned",
                    $"Utilisateur {userId} n'a aucun centre assigné",
                    userId.Value);

                ModelState.AddModelError(string.Empty,
                    "Aucun centre hospitalier assigné. Contactez votre administrateur.");
                return RedirectToAction("Login");
            }

            // Si un seul centre, y diriger automatiquement
            if (centers.Count == 1)
            {
                await _appLogger.LogInfoAsync("Authentication", "AutoCenterSelection",
                    $"Sélection automatique du centre unique {centers[0].CenterName}",
                    userId.Value);

                return await ProcessCenterSelection(userId.Value, centers[0].HospitalCenterId);
            }

            // Préparer le modèle pour l'interface de sélection
            var userEmail = HttpContext.Session.GetString("UserEmail") ?? "Utilisateur";
            var userName = HttpContext.Session.GetString("UserName") ?? userEmail;

            var model = new CenterSelectionViewModel
            {
                UserId = userId.Value,
                UserName = userName,
                AvailableCenters = centers.ToList(),
                LastSelectedCenterId = await _authService.GetLastSelectedCenterAsync(userId.Value)
            };

            await _appLogger.LogInfoAsync("Authentication", "CenterSelectionDisplayed",
                $"Affichage de la sélection de centre pour {userEmail} ({centers.Count} centres disponibles)",
                userId.Value);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'affichage de la sélection de centre pour l'utilisateur {UserId}", userId);

            await _auditService.LogSystemErrorAsync(ex, "AuthController.SelectCenter", userId,
                contextData: new { UserId = userId });

            ModelState.AddModelError(string.Empty,
                "Erreur lors du chargement des centres. Veuillez réessayer.");
            return RedirectToAction("Login");
        }
    }

    /// <summary>
    /// Traite la sélection d'un centre
    /// Établit le contexte de travail complet
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectCenter(int centerId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login");
        }

        return await ProcessCenterSelection(userId.Value, centerId);
    }

    /// <summary>
    /// Méthode privée pour traiter la sélection de centre
    /// Centralise la logique complexe de création de session
    /// </summary>
    private async Task<IActionResult> ProcessCenterSelection(int userId, int centerId)
    {
        try
        {
            var clientIp = GetClientIpAddress();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Créer la session avec le centre sélectionné
            var sessionInfo = await _authService.CreateSessionAsync(userId, centerId, clientIp, userAgent);

            // Configurer la session HTTP
            HttpContext.Session.SetString("SessionToken", sessionInfo.SessionToken);
            HttpContext.Session.SetInt32("CurrentCenterId", centerId);
            HttpContext.Session.SetString("CurrentCenterName", sessionInfo.CurrentCenterName);
            HttpContext.Session.SetString("CurrentRole", sessionInfo.CurrentRole);

            // Enregistrement de l'événement pour audit
            await _auditService.LogCenterSwitchAsync(userId, 0, centerId, clientIp);

            await _appLogger.LogInfoAsync("Authentication", "CenterSelected",
                $"Centre {sessionInfo.CurrentCenterName} sélectionné par utilisateur {userId}",
                userId, centerId);

            // Redirection vers le tableau de bord approprié selon le rôle
            //var returnAction = sessionInfo.CurrentRole == "SuperAdmin" ? "Admin" : "Dashboard";
            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sélection du centre {CenterId} pour l'utilisateur {UserId}",
                centerId, userId);

            await _auditService.LogSystemErrorAsync(ex, "AuthController.ProcessCenterSelection", userId,
                contextData: new { UserId = userId, CenterId = centerId });

            ModelState.AddModelError(string.Empty,
                "Erreur lors de la sélection du centre. Veuillez réessayer.");
            return RedirectToAction("SelectCenter");
        }
    }

    // ===== CHANGEMENT DE CENTRE EN COURS DE SESSION =====

    /// <summary>
    /// Change le centre actif sans déconnexion
    /// API AJAX pour changement dynamique de contexte
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireAuthentication]
    [RequireCenterAccess]
    public async Task<IActionResult> SwitchCenter(int centerId)
    {
        var sessionToken = HttpContext.Session.GetString("SessionToken");

        try
        {
            var success = await _authService.SwitchCenterAsync(sessionToken, centerId);

            if (success)
            {
                // Mettre à jour la session HTTP
                var sessionDetails = await _authService.GetSessionDetailsAsync(sessionToken);
                if (sessionDetails != null)
                {
                    HttpContext.Session.SetInt32("CurrentCenterId", centerId);
                    HttpContext.Session.SetString("CurrentCenterName", sessionDetails.CurrentCenter.Name);
                    HttpContext.Session.SetString("CurrentRole", sessionDetails.SessionInfo.CurrentRole);

                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    var clientIp = GetClientIpAddress();

                    await _auditService.LogCenterSwitchAsync(userId,
                        HttpContext.Session.GetInt32("CurrentCenterId") ?? 0,
                        centerId, clientIp);

                    await _appLogger.LogInfoAsync("Authentication", "CenterSwitched",
                        $"Changement vers centre {sessionDetails.CurrentCenter.Name}",
                        userId, centerId);

                    return Json(new
                    {
                        success = true,
                        centerName = sessionDetails.CurrentCenter.Name,
                        role = sessionDetails.SessionInfo.CurrentRole
                    });
                }
            }

            return Json(new { success = false, message = "Impossible de changer de centre" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de centre vers {CenterId}", centerId);

            var userId = HttpContext.Session.GetInt32("UserId");
            await _auditService.LogSystemErrorAsync(ex, "AuthController.SwitchCenter", userId,
                contextData: new { CenterId = centerId });

            return Json(new { success = false, message = "Erreur lors du changement de centre" });
        }
    }

    // ===== DÉCONNEXION =====

    /// <summary>
    /// Déconnecte l'utilisateur avec nettoyage complet
    /// Processus de sortie sécurisé avec audit
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var sessionToken = HttpContext.Session.GetString("SessionToken");
        var userEmail = HttpContext.Session.GetString("UserEmail");
        var clientIp = GetClientIpAddress();

        try
        {
            // Déconnexion via le service d'authentification
            if (userId.HasValue)
            {
                await _authService.LogoutAsync(userId.Value, clientIp);

                // Audit de la déconnexion
                await _auditService.LogAuthenticationEventAsync(userId.Value,
                    AuthenticationEvent.Logout, true, clientIp);

                await _appLogger.LogInfoAsync("Authentication", "Logout",
                    $"Déconnexion réussie pour {userEmail}",
                    userId.Value);
            }

            // Nettoyage complet de la session
            HttpContext.Session.Clear();

            // Message de confirmation
            TempData["SuccessMessage"] = "Déconnexion réussie";

            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déconnexion pour l'utilisateur {UserId}", userId);

            await _auditService.LogSystemErrorAsync(ex, "AuthController.Logout", userId,
                contextData: new { UserId = userId, Email = userEmail });

            // Même en cas d'erreur, nettoyer la session locale
            HttpContext.Session.Clear();
            TempData["ErrorMessage"] = "Erreur lors de la déconnexion, mais vous êtes déconnecté localement";

            return RedirectToAction("Login");
        }
    }

    // ===== GESTION DES MOTS DE PASSE =====

    /// <summary>
    /// Affiche l'écran de changement de mot de passe obligatoire
    /// Après réinitialisation administrative
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ChangePassword()
    {
        var tempUserId = HttpContext.Session.GetInt32("TempUserId");
        var tempUserEmail = HttpContext.Session.GetString("TempUserEmail");

        if (tempUserId == null || string.IsNullOrEmpty(tempUserEmail))
        {
            return RedirectToAction("Login");
        }

        var model = new ChangePasswordViewModel
        {
            UserId = tempUserId.Value,
            Email = tempUserEmail,
            IsForced = true
        };

        return View(model);
    }

    /// <summary>
    /// Traite le changement de mot de passe obligatoire
    /// Processus sécurisé avec validation complète
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var tempUserId = HttpContext.Session.GetInt32("TempUserId");
        if (tempUserId != model.UserId)
        {
            ModelState.AddModelError(string.Empty, "Session invalide. Reconnectez-vous.");
            return RedirectToAction("Login");
        }

        try
        {
            var clientIp = GetClientIpAddress();

            // Validation du nouveau mot de passe
            var validation = await _authService.ValidatePasswordAsync(model.NewPassword, model.UserId);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                return View(model);
            }

            // Changement du mot de passe (forcé, donc sans vérification de l'ancien)
            var result = await _authService.ChangePasswordAsync(model.UserId,
                string.Empty,
                model.NewPassword);

            if (result.IsSuccess)
            {
                // Audit du changement de mot de passe
                await _auditService.LogPasswordChangeAsync(model.UserId,
                    PasswordChangeType.UserInitiated, null, clientIp);

                await _appLogger.LogInfoAsync("Authentication", "PasswordChanged",
                    $"Mot de passe changé avec succès après réinitialisation",
                    model.UserId);

                // Nettoyer la session temporaire
                HttpContext.Session.Remove("TempUserId");
                HttpContext.Session.Remove("TempUserEmail");

                TempData["SuccessMessage"] = "Mot de passe changé avec succès. Vous pouvez maintenant vous connecter.";
                return RedirectToAction("Login");
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Erreur lors du changement");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de mot de passe pour l'utilisateur {UserId}", model.UserId);

            await _auditService.LogSystemErrorAsync(ex, "AuthController.ChangePassword", model.UserId,
                contextData: new { UserId = model.UserId });

            ModelState.AddModelError(string.Empty, "Erreur lors du changement de mot de passe. Réessayez.");
            return View(model);
        }
    }

    // ===== MÉTHODES UTILITAIRES =====

    /// <summary>
    /// Récupère l'adresse IP du client
    /// Gère les cas de proxy et load balancer
    /// </summary>
    private string GetClientIpAddress()
    {
        // Vérifier les headers de proxy communs
        var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();

        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Nettoyer l'IP en cas de liste (prendre la première)
        if (!string.IsNullOrEmpty(ipAddress) && ipAddress.Contains(','))
            ipAddress = ipAddress.Split(',')[0].Trim();

        return ipAddress ?? "Unknown";
    }

    /// <summary>
    /// Redirection locale sécurisée
    /// Empêche les attaques de redirection ouverte
    /// </summary>
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Redirection par défaut vers le tableau de bord approprié
        var currentRole = HttpContext.Session.GetString("CurrentRole");
        var defaultAction = currentRole == "SuperAdmin" ? "Admin" : "Dashboard";

        return RedirectToAction("Index", defaultAction);
    }

    // ===== API POUR AJAX =====

    /// <summary>
    /// Vérifie la validité de la session via AJAX
    /// Utilisé pour maintenir la session active côté client
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ValidateSession()
    {
        var sessionToken = HttpContext.Session.GetString("SessionToken");
        if (string.IsNullOrEmpty(sessionToken))
        {
            return Json(new { valid = false, expired = true });
        }

        try
        {
            var validation = await _authService.ValidateSessionAsync(sessionToken);
            return Json(new
            {
                valid = validation.IsValid,
                expired = validation.IsExpired,
                expiresAt = validation.SessionInfo?.ExpiresAt.ToString("HH:mm")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la validation de session {SessionToken}", sessionToken);
            return Json(new { valid = false, error = true });
        }
    }

    /// <summary>
    /// Prolonge la session active
    /// API pour éviter les déconnexions inattendues
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExtendSession()
    {
        var sessionToken = HttpContext.Session.GetString("SessionToken");
        if (string.IsNullOrEmpty(sessionToken))
        {
            return Json(new { success = false, message = "Aucune session active" });
        }

        try
        {
            var success = await _authService.ExtendSessionAsync(sessionToken);

            if (success)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                await _appLogger.LogInfoAsync("Authentication", "SessionExtended",
                    "Session prolongée automatiquement", userId);
            }

            return Json(new { success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la prolongation de session {SessionToken}", sessionToken);
            return Json(new { success = false, message = "Erreur lors de la prolongation" });
        }
    }

    /// <summary>
    /// API pour récupérer les centres accessibles pour le changement de contexte
    /// Utilisé pour alimenter le menu déroulant de changement de centre
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAccessibleCenters()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Json(new { success = false, message = "Session expirée" });
        }

        try
        {
            // Récupérer tous les centres accessibles à l'utilisateur
            var centers = await _authService.GetUserAccessibleCentersAsync(userId.Value);
            var currentCenterId = HttpContext.Session.GetInt32("CurrentCenterId");

            // Mapper vers le modèle approprié pour l'affichage
            var centerOptions = centers.Select(c => new CenterOption
            {
                HospitalCenterId = c.HospitalCenterId,
                CenterName = c.CenterName,
                Address = c.CenterAddress,
                RoleInCenter = c.RoleType,
                IsCurrent = c.HospitalCenterId == currentCenterId
            }).ToList();

            var model = new CenterSwitchModel
            {
                CurrentCenterId = currentCenterId ?? 0,
                CurrentCenterName = HttpContext.Session.GetString("CurrentCenterName") ?? "",
                CurrentRole = HttpContext.Session.GetString("CurrentRole") ?? "",
                AvailableCenters = centerOptions
            };

            return Json(new { success = true, data = model });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des centres accessibles pour l'utilisateur {UserId}", userId);
            return Json(new { success = false, message = "Erreur lors du chargement des centres" });
        }
    }

    /// <summary>
    /// Met à jour le centre sélectionné dans le menu déroulant
    /// Appelé en AJAX pour mettre à jour l'affichage du centre actuel
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateCenterDisplay()
    {
        var sessionToken = HttpContext.Session.GetString("SessionToken");
        if (string.IsNullOrEmpty(sessionToken))
        {
            return Json(new { success = false, message = "Session expirée" });
        }

        try
        {
            // Récupérer les détails de la session actuelle
            var sessionDetails = await _authService.GetSessionDetailsAsync(sessionToken);
            if (sessionDetails == null)
            {
                return Json(new { success = false, message = "Session invalide" });
            }

            // Récupérer les centres accessibles pour le menu
            var centers = await _authService.GetUserAccessibleCentersAsync(sessionDetails.User.Id);
            var centerOptions = centers.Select(c => new CenterOption
            {
                HospitalCenterId = c.HospitalCenterId,
                CenterName = c.CenterName,
                Address = c.CenterAddress,
                RoleInCenter = c.RoleType,
                IsCurrent = c.HospitalCenterId == sessionDetails.SessionInfo.CurrentHospitalCenterId
            }).ToList();

            return Json(new
            {
                success = true,
                currentCenter = new
                {
                    id = sessionDetails.SessionInfo.CurrentHospitalCenterId,
                    name = sessionDetails.CurrentCenter.Name,
                    role = sessionDetails.SessionInfo.CurrentRole
                },
                availableCenters = centerOptions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'affichage du centre");
            return Json(new { success = false, message = "Erreur lors de la mise à jour" });
        }
    }
}
