using HManagSys.Attributes;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Users;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers;

/// <summary>
/// Contrôleur administrateur pour la gestion des utilisateurs et du système
/// Centre de contrôle pour les SuperAdmins
/// </summary>
[RequireAuthentication]
public class AdminController : BaseController
{
    private readonly IUserRepository _userRepository;
    private readonly IHospitalCenterRepository _hospitalCenterRepository;
    private readonly IUserCenterAssignmentRepository _assignmentRepository;
    private readonly IAuthenticationService _authService;
    private readonly IApplicationLogger _appLogger;
    private readonly IAuditService _auditService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserRepository userRepository,
        IHospitalCenterRepository hospitalCenterRepository,
        IUserCenterAssignmentRepository assignmentRepository,
        IAuthenticationService authService,
        IApplicationLogger appLogger,
        IAuditService auditService,
        ILogger<AdminController> logger)
    {
        _userRepository = userRepository;
        _hospitalCenterRepository = hospitalCenterRepository;
        _assignmentRepository = assignmentRepository;
        _authService = authService;
        _appLogger = appLogger;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Tableau de bord administrateur principal
    /// </summary>
    public async Task<IActionResult> Index(UserManagementFilters? filters = null)
    {

        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var currentCenterId = HttpContext.Session.GetInt32("CurrentCenterId");
            var currentRole = HttpContext.Session.GetString("CurrentRole");

            if (!currentUserId.HasValue || currentRole != "SuperAdmin")
            {
                return RedirectToAction("Login", "Auth");
            }

            // Initialiser les filtres si null
            filters ??= new UserManagementFilters();

            // Si SuperAdmin d'un centre spécifique, filtrer par défaut sur ce centre
            if (currentCenterId.HasValue && !filters.HospitalCenterId.HasValue)
            {
                // Vérifier si l'admin a une vision globale ou limitée à un centre
                if (!IsSuperAdmin)
                {
                    filters.HospitalCenterId = currentCenterId.Value;
                }
            }

            // Récupérer les utilisateurs avec critères
            var (users, totalCount) = await _userRepository.SearchUsersAsync(
                filters.SearchTerm,
                filters.IsActive,
                filters.RoleFilter,
                filters.HospitalCenterId,
                filters.PageIndex,
                filters.PageSize);

            // Récupérer les statistiques
            var statistics = await _userRepository.GetUserStatisticsAsync(filters.HospitalCenterId);

            // Récupérer les sessions actives
            var activeSessions = await _authService.GetAuthenticationStatisticsAsync();

            // Récupérer les centres pour le filtre
            var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive == true));

            var viewModel = new AdminDashboardViewModel
            {
                Users = users,
                Filters = filters,
                Statistics = new AdminStatistics
                {
                    TotalUsers = statistics.TotalUsers,
                    ActiveUsers = statistics.ActiveUsers,
                    UsersLoggedToday = statistics.UsersLoggedToday,
                    UsersRequiringPasswordChange = statistics.UsersRequiringPasswordChange,
                    SuperAdmins = statistics.SuperAdmins,
                    MedicalStaff = statistics.MedicalStaff,
                    TotalActiveSessions = activeSessions.UniqueUsersLoggedIn
                },
                Pagination = new PaginationInfo
                {
                    CurrentPage = filters.PageIndex,
                    PageSize = filters.PageSize,
                    TotalCount = totalCount,
                    //TotalPages = (int)Math.Ceiling((double)totalCount / filters.PageSize)
                },
                AvailableCenters = centers.ToList()
            };

            // Log de l'accès au tableau de bord admin
            await _appLogger.LogInfoAsync("Admin", "DashboardAccess",
                "Accès au tableau de bord administrateur",
                currentUserId.Value, currentCenterId);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement du tableau de bord administrateur");
            TempData["ErrorMessage"] = "Erreur lors du chargement du tableau de bord";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    /// <summary>
    /// Réinitialise le mot de passe d'un utilisateur
    /// </summary>
    [SuperAdmin]
    [PreventSelfAction]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int userId)
    {

        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");

            // Réinitialiser le mot de passe
            var result = await _authService.ResetPasswordAsync(userId, currentUserId.Value);

            if (result.IsSuccess)
            {
                await _appLogger.LogInfoAsync("Admin", "PasswordReset",
                    $"Mot de passe réinitialisé pour l'utilisateur {userId}",
                    currentUserId.Value);

                return Json(new
                {
                    success = true,
                    message = "Mot de passe réinitialisé avec succès",
                    temporaryPassword = result.TemporaryPassword
                });
            }
            else
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la réinitialisation du mot de passe pour l'utilisateur {UserId}", userId);
            return Json(new { success = false, message = "Erreur lors de la réinitialisation" });
        }
    }

    /// <summary>
    /// Active ou désactive un compte utilisateur
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(int userId, bool isActive)
    {
        if (!IsSuperAdmin)
        {
            return Json(new { success = false, message = "Accès refusé" });
        }

        // Empêcher de modifier son propre statut
        if (userId == CurrentUserId.Value)
        {
            return Json(new { success = false, message = "Vous ne pouvez pas modifier votre propre statut" });
        }

        try
        {
            var success = await _userRepository.SetUserActiveStatusAsync(userId, isActive, CurrentUserId.Value);

            if (success)
            {
                var action = isActive ? "activé" : "désactivé";
                await _appLogger.LogInfoAsync("Admin", "UserStatusChanged",
                    $"Compte utilisateur {userId} {action}",
                    CurrentUserId.Value);

                return Json(new { success = true, message = $"Compte {action} avec succès" });
            }

            return Json(new { success = false, message = "Erreur lors de la modification" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la modification du statut de l'utilisateur {UserId}", userId);
            return Json(new { success = false, message = "Erreur lors de la modification du statut" });
        }
    }

    /// <summary>
    /// Force la déconnexion de toutes les sessions d'un utilisateur
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceLogout(int userId)
    {
        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return Json(new { success = false, message = "Session expirée" });
            }

            // Vérifier les permissions
            if (!IsSuperAdmin)
            {
                return Json(new { success = false, message = "Permissions insuffisantes" });
            }

            // Ne pas permettre de se déconnecter soi-même
            if (userId == currentUserId.Value)
            {
                return Json(new { success = false, message = "Vous ne pouvez pas forcer votre propre déconnexion" });
            }

            // Forcer la déconnexion
            var success = await _authService.LogoutAsync(userId);

            if (success)
            {
                await _appLogger.LogWarningAsync("Admin", "ForcedLogout",
                    $"Déconnexion forcée de l'utilisateur {userId}",
                    currentUserId.Value);

                return Json(new
                {
                    success = true,
                    message = "Utilisateur déconnecté avec succès"
                });
            }
            else
            {
                return Json(new { success = false, message = "Erreur lors de la déconnexion" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déconnexion forcée de l'utilisateur {UserId}", userId);
            return Json(new { success = false, message = "Erreur lors de la déconnexion" });
        }
    }

    /// <summary>
    /// Affiche l'historique des connexions d'un utilisateur
    /// </summary>
    public async Task<IActionResult> UserLoginHistory(int userId)
    {
        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return Json(new { success = false, message = "Session expirée" });
            }

            // Récupérer l'historique des connexions
            var history = await _userRepository.GetUserLoginHistoryAsync(userId, 30);

            return Json(new { success = true, data = history });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'historique pour l'utilisateur {UserId}", userId);
            return Json(new { success = false, message = "Erreur lors de la récupération de l'historique" });
        }
    }

    /// <summary>
    /// Récupère les sessions actives d'un utilisateur
    /// </summary>
    public async Task<IActionResult> GetActiveSessions(int userId)
    {
        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return Json(new { success = false, message = "Session expirée" });
            }

            // Récupérer les sessions actives
            var sessions = await _authService.GetUserActiveSessionsAsync(userId);

            return Json(new { success = true, data = sessions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des sessions actives pour l'utilisateur {UserId}", userId);
            return Json(new { success = false, message = "Erreur lors de la récupération des sessions" });
        }
    }

    /// <summary>
    /// Exporte la liste des utilisateurs en Excel
    /// </summary>
    public async Task<IActionResult> ExportUsers(UserManagementFilters? filters = null)
    {
        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Récupérer tous les utilisateurs selon les filtres (sans pagination)
            filters ??= new UserManagementFilters { PageSize = int.MaxValue };
            var (users, _) = await _userRepository.SearchUsersAsync(
                filters.SearchTerm,
                filters.IsActive,
                filters.RoleFilter,
                filters.HospitalCenterId,
                1,
                int.MaxValue);

            // Log de l'export
            await _appLogger.LogInfoAsync("Admin", "UsersExport",
                $"Export de {users.Count} utilisateurs",
                currentUserId.Value);

            // Créer le fichier Excel (à implémenter avec ClosedXML)
            // Pour l'instant, on simule avec un CSV
            var csv = GenerateUsersCsv(users);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", $"utilisateurs_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'export des utilisateurs");
            TempData["ErrorMessage"] = "Erreur lors de l'export des utilisateurs";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Génère un CSV des utilisateurs (temporaire, à remplacer par Excel)
    /// </summary>
    private string GenerateUsersCsv(List<UserSummary> users)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Nom,Prénom,Email,Téléphone,Statut,Dernière Connexion,Centres/Rôles");

        foreach (var user in users)
        {
            var centerRoles = string.Join("; ", user.Assignments.Select(a => $"{a.HospitalCenterName} ({a.RoleType})"));
            csv.AppendLine($"{user.LastName},{user.FirstName},{user.Email},{user.PhoneNumber}," +
                          $"{(user.IsActive ? "Actif" : "Inactif")},{user.LastLoginDate:yyyy-MM-dd HH:mm},{centerRoles}");
        }

        return csv.ToString();
    }


    /// <summary>
    /// Affiche le formulaire de création d'un utilisateur
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {

        try
        {
            

            // Récupérer tous les centres actifs
            var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));

            var model = new CreateUserViewModel
            {
                AvailableCenters = centers.ToList(),
                CenterAssignments = new List<UserCenterAssignmentDto>()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'affichage du formulaire de création d'utilisateur");
            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Traite la création d'un nouvel utilisateur
    /// </summary>
    [SuperAdmin]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            //if (!currentUserId.HasValue)
            //{
            //    return RedirectToAction("Login", "Auth");
            //}


            // Valider le modèle
            if (!ModelState.IsValid)
            {
                // Recharger les centres disponibles
                var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));
                model.AvailableCenters = centers.ToList();
                return View(model);
            }

            // Vérifier que l'email n'existe pas déjà
            var existingUser = await _userRepository.GetByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Un utilisateur avec cet email existe déjà");
                var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));
                model.AvailableCenters = centers.ToList();
                return View(model);
            }

            // Vérifier qu'au moins une affectation est définie
            if (model.CenterAssignments == null || !model.CenterAssignments.Any())
            {
                ModelState.AddModelError("CenterAssignments", "Au moins une affectation de centre est requise");
                var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));
                model.AvailableCenters = centers.ToList();
                return View(model);
            }

            // Utiliser une transaction pour créer l'utilisateur et ses affectations
            var result = await _userRepository.TransactionAsync(async () =>
            {
                // Générer mot de passe temporaire
                var tempPassword = await _authService.GenerateTemporaryPasswordAsync();
                var hashedPassword = _authService.HashPassword(tempPassword);

                // Créer l'utilisateur
                var newUser = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = hashedPassword,
                    MustChangePassword = true,
                    IsActive = true,
                    CreatedBy = currentUserId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _userRepository.AddAsync(newUser);

                // Créer les affectations de centres
                foreach (var assignment in model.CenterAssignments.Where(a => a.IsSelected))
                {
                    await _assignmentRepository.CreateAssignmentAsync(
                        createdUser.Id,
                        assignment.HospitalCenterId,
                        assignment.RoleType,
                        currentUserId.Value
                    );
                }

                // Enregistrer l'audit
                await _auditService.LogUserCreatedAsync(
                    createdUser.Id,
                    currentUserId.Value,
                    createdUser,
                    GetClientIpAddress()
                );

                // Log applicatif
                await _appLogger.LogInfoAsync("Admin", "UserCreated",
                    $"Nouvel utilisateur créé: {createdUser.Email}",
                    currentUserId.Value);

                return new { Success = true, User = createdUser, TempPassword = tempPassword };
            });

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Utilisateur créé avec succès. Mot de passe temporaire: {result.TempPassword}";
                TempData["TempPassword"] = result.TempPassword;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Erreur lors de la création de l'utilisateur");
                var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));
                model.AvailableCenters = centers.ToList();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'utilisateur");
            ModelState.AddModelError("", "Erreur lors de la création de l'utilisateur");

            // Recharger les centres
            var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));
            model.AvailableCenters = centers.ToList();
            return View(model);
        }
    }

    /// <summary>
    /// Affiche le formulaire de modification d'un utilisateur
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }


            if (!IsSuperAdmin)
            {
                TempData["ErrorMessage"] = "Permissions insuffisantes";
                return RedirectToAction("Index");
            }

            // Récupérer l'utilisateur avec ses affectations
            var user = await _userRepository.GetUserWithAssignmentsAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilisateur introuvable";
                return RedirectToAction("Index");
            }

            // Récupérer tous les centres actifs
            var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));

            // Mapper vers le modèle de vue
            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                AvailableCenters = centers.ToList(),
                CenterAssignments = centers.Select(c => new UserCenterAssignmentDto
                {
                    HospitalCenterId = c.Id,
                    CenterName = c.Name,
                    RoleType = user.UserCenterAssignments
                        .FirstOrDefault(a => a.HospitalCenterId == c.Id && a.IsActive)?.RoleType ?? "MedicalStaff",
                    IsSelected = user.UserCenterAssignments
                        .Any(a => a.HospitalCenterId == c.Id && a.IsActive)
                }).ToList()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement de l'utilisateur {UserId}", id);
            TempData["ErrorMessage"] = "Erreur lors du chargement de l'utilisateur";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Traite la modification d'un utilisateur
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Vérifier les permissions
            if (!IsSuperAdmin)
            {
                TempData["ErrorMessage"] = "Permissions insuffisantes";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                // Recharger les centres
                var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));
                model.AvailableCenters = centers.ToList();
                return View(model);
            }

            // Récupérer l'utilisateur existant
            var existingUser = await _userRepository.GetUserWithAssignmentsAsync(model.Id);
            if (existingUser == null)
            {
                TempData["ErrorMessage"] = "Utilisateur introuvable";
                return RedirectToAction("Index");
            }

            // Sauvegarder les anciennes valeurs pour l'audit
            var oldUser = new User
            {
                FirstName = existingUser.FirstName,
                LastName = existingUser.LastName,
                Email = existingUser.Email,
                PhoneNumber = existingUser.PhoneNumber,
                IsActive = existingUser.IsActive
            };

            // Utiliser une transaction pour la modification
            var result = await _userRepository.TransactionAsync<bool>(async () =>
            {
                // Mettre à jour les informations de base
                existingUser.FirstName = model.FirstName;
                existingUser.LastName = model.LastName;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.IsActive = model.IsActive;
                existingUser.ModifiedBy = currentUserId.Value;
                existingUser.ModifiedAt = DateTime.UtcNow;

                if (IsSuperAdmin && existingUser.Email != model.Email)
                {
                    // Vérifier que le nouvel email n'existe pas
                    var emailExists = await _userRepository.GetByEmailAsync(model.Email);
                    if (emailExists != null && emailExists.Id != model.Id)
                    {
                        return  false;
                    }
                    existingUser.Email = model.Email;
                }

                await _userRepository.UpdateAsync(existingUser);

                // Gérer les affectations de centres
                var currentAssignments = existingUser.UserCenterAssignments.Where(a => a.IsActive).ToList();

                // Terminer les affectations qui ne sont plus sélectionnées
                foreach (var assignment in currentAssignments)
                {
                    var stillSelected = model.CenterAssignments
                        .Any(ca => ca.HospitalCenterId == assignment.HospitalCenterId && ca.IsSelected);

                    if (!stillSelected)
                    {
                        await _assignmentRepository.EndAssignmentAsync(
                            assignment.UserId,
                            assignment.HospitalCenterId,
                            currentUserId.Value
                        );
                    }
                    else
                    {
                        // Vérifier si le rôle a changé
                        var newAssignment = model.CenterAssignments
                            .First(ca => ca.HospitalCenterId == assignment.HospitalCenterId);

                        if (assignment.RoleType != newAssignment.RoleType)
                        {
                            await _assignmentRepository.UpdateRoleAsync(
                                assignment.UserId,
                                assignment.HospitalCenterId,
                                newAssignment.RoleType,
                                currentUserId.Value
                            );
                        }
                    }
                }

                // Créer les nouvelles affectations
                foreach (var newAssignment in model.CenterAssignments.Where(ca => ca.IsSelected))
                {
                    var existingAssignment = currentAssignments
                        .Any(a => a.HospitalCenterId == newAssignment.HospitalCenterId);

                    if (!existingAssignment)
                    {
                        await _assignmentRepository.CreateAssignmentAsync(
                            model.Id,
                            newAssignment.HospitalCenterId,
                            newAssignment.RoleType,
                            currentUserId.Value
                        );
                    }
                }

                return true;
            });

            if (result)
            {
                // Enregistrer l'audit
                await _auditService.LogUserModifiedAsync(
                    model.Id,
                    currentUserId.Value,
                    oldUser,
                    existingUser,
                    GetClientIpAddress()
                );

                await _appLogger.LogInfoAsync("Admin", "UserModified",
                    $"Utilisateur modifié: {existingUser.Email}",
                    currentUserId.Value);

                TempData["SuccessMessage"] = "Utilisateur modifié avec succès";
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Erreur lors de la modification");
                var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));
                model.AvailableCenters = centers.ToList();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la modification de l'utilisateur {UserId}", model.Id);
            ModelState.AddModelError("", "Erreur lors de la modification");

            // Recharger les centres
            var centers = await _hospitalCenterRepository.GetAllAsync(q => q.Where(c => c.IsActive));
            model.AvailableCenters = centers.ToList();
            return View(model);
        }
    }

    /// <summary>
    /// Affiche les détails d'un utilisateur
    /// </summary>
    public async Task<IActionResult> UserDetails(int id)
    {
        try
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Récupérer l'utilisateur avec ses affectations et historique
            var user = await _userRepository.GetUserWithAssignmentsAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilisateur introuvable";
                return RedirectToAction("Index");
            }

            // Récupérer l'historique des connexions
            var loginHistory = await _userRepository.GetUserLoginHistoryAsync(id, 30);

            // Récupérer les sessions actives
            var activeSessions = await _authService.GetUserActiveSessionsAsync(id);

            var model = new UserDetailsViewModel
            {
                User = user,
                LoginHistory = loginHistory,
                ActiveSessions = activeSessions,
                CanEdit = IsSuperAdmin,
                CanResetPassword = IsSuperAdmin
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des détails de l'utilisateur {UserId}", id);
            TempData["ErrorMessage"] = "Erreur lors du chargement des détails";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Récupère l'adresse IP du client
    /// </summary>
    private string GetClientIpAddress()
    {
        var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(ipAddress) && ipAddress.Contains(','))
            ipAddress = ipAddress.Split(',')[0].Trim();

        return ipAddress ?? "Unknown";
    }
}