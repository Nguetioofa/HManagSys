using HManagSys.Data.DBContext;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.Enums;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Users;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HManagSys.Services
{
    /// <summary>
    /// Service d'authentification complet
    /// Le cerveau de sécurité de notre hôpital numérique
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HospitalManagementContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IHospitalCenterRepository _hospitalCenterRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IApplicationLogger _appLogger;
        private readonly ILogger<AuthenticationService> _logger;

        // Durée de session par défaut (12 heures)
        private readonly TimeSpan _defaultSessionDuration = TimeSpan.FromHours(12);

        public AuthenticationService(
            HospitalManagementContext context,
            IUserRepository userRepository,
            IHospitalCenterRepository hospitalCenterRepository,
            IPasswordHasher passwordHasher,
            IApplicationLogger appLogger,
            ILogger<AuthenticationService> logger)
        {
            _context = context;
            _userRepository = userRepository;
            _hospitalCenterRepository = hospitalCenterRepository;
            _passwordHasher = passwordHasher;
            _appLogger = appLogger;
            _logger = logger;
        }

        // ===== AUTHENTIFICATION DE BASE =====

        public async Task<AuthenticationResult> LoginAsync(string email, string password, string? ipAddress = null)
        {
            try
            {
                // Rechercher l'utilisateur par email
                var user = await _userRepository.GetByEmailAsync(email);

                if (user == null)
                {
                    await _appLogger.LogWarningAsync("Authentication", "LoginAttemptInvalidEmail",
                        $"Tentative de connexion avec email inexistant: {email}",
                        details: new { Email = email, IpAddress = ipAddress });

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Identifiants invalides",
                        ErrorCode = "INVALID_CREDENTIALS"
                    };
                }

                // Vérifier si le compte est actif
                if (!user.IsActive)
                {
                    await _appLogger.LogWarningAsync("Authentication", "LoginAttemptInactiveUser",
                        $"Tentative de connexion sur compte inactif: {email}",
                        details: new { Email = email, UserId = user.Id, IpAddress = ipAddress });

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Compte désactivé",
                        ErrorCode = "ACCOUNT_INACTIVE"
                    };
                }

                // Vérifier le mot de passe
                if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
                {
                    await _appLogger.LogWarningAsync("Authentication", "LoginAttemptInvalidPassword",
                        $"Tentative de connexion avec mot de passe incorrect: {email}",
                        user.Id, details: new { Email = email, IpAddress = ipAddress });

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Identifiants invalides",
                        ErrorCode = "INVALID_CREDENTIALS"
                    };
                }

                // Mettre à jour la date de dernière connexion
                await _userRepository.RecordLoginAsync(user.Id, ipAddress);

                // Vérifier s'il y a des affectations actives
                var activeAssignments = await _userRepository.GetUserActiveCentersAsync(user.Id);
                if (!activeAssignments.Any())
                {
                    await _appLogger.LogWarningAsync("Authentication", "LoginNoActiveAssignments",
                        $"Utilisateur sans affectations actives: {email}",
                        user.Id, details: new { Email = email });

                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Aucun centre assigné. Contactez votre administrateur.",
                        ErrorCode = "NO_ASSIGNMENTS"
                    };
                }

                // Succès de connexion
                await _appLogger.LogInfoAsync("Authentication", "LoginSuccess",
                    $"Connexion réussie pour {email}",
                    user.Id, details: new { Email = email, IpAddress = ipAddress });

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    User = user,
                    RequiresPasswordChange = user.MustChangePassword
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'authentification pour {Email}", email);

                await _appLogger.LogErrorAsync("Authentication", "LoginError",
                    $"Erreur système lors de la connexion pour {email}",
                    details: new { Email = email, Error = ex.Message });

                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Erreur système. Veuillez réessayer.",
                    ErrorCode = "SYSTEM_ERROR"
                };
            }
        }

        public async Task<bool> LogoutAsync(int userId, string? ipAddress = null)
        {
            try
            {
                // Terminer toutes les sessions actives de l'utilisateur
                var activeSessions = await _context.UserSessions
                    .Where(us => us.UserId == userId && us.IsActive)
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    session.IsActive = false;
                    session.LogoutTime = TimeZoneHelper.GetCameroonTime();
                }

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("Authentication", "Logout",
                    $"Déconnexion réussie pour utilisateur ID {userId}",
                    userId, details: new { IpAddress = ipAddress, SessionsTerminated = activeSessions.Count });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion pour utilisateur {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> LogoutSessionAsync(string sessionToken, string? ipAddress = null)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(us => us.SessionToken == sessionToken && us.IsActive);

                if (session == null) return false;

                session.IsActive = false;
                session.LogoutTime = TimeZoneHelper.GetCameroonTime();

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("Authentication", "SessionLogout",
                    $"Session terminée: {sessionToken}",
                    session.UserId, details: new { SessionToken = sessionToken, IpAddress = ipAddress });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion de session {SessionToken}", sessionToken);
                return false;
            }
        }

        // ===== GESTION DES SESSIONS =====

        public async Task<SessionInfo> CreateSessionAsync(
            int userId, int hospitalCenterId, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                // Générer un token de session unique
                var sessionToken = GenerateSecureToken();

                // Créer la session
                var session = new UserSession
                {
                    UserId = userId,
                    CurrentHospitalCenterId = hospitalCenterId,
                    SessionToken = sessionToken,
                    LoginTime = TimeZoneHelper.GetCameroonTime(),
                    IpAddress = ipAddress,
                    IsActive = true
                };

                _context.UserSessions.Add(session);

                // Mettre à jour le dernier centre sélectionné
                await SaveLastSelectedCenterAsync(userId, hospitalCenterId);

                await _context.SaveChangesAsync();

                // Récupérer les informations détaillées pour la réponse
                var sessionDetails = await GetSessionDetailsAsync(sessionToken);

                var sessionInfo = new SessionInfo
                {
                    SessionToken = sessionToken,
                    UserId = userId,
                    UserName = sessionDetails?.User != null
                        ? $"{sessionDetails.User.FirstName} {sessionDetails.User.LastName}"
                        : "Utilisateur",
                    CurrentHospitalCenterId = hospitalCenterId,
                    CurrentCenterName = sessionDetails?.CurrentCenter?.Name ?? "Centre",
                    CurrentRole = sessionDetails?.SessionInfo?.CurrentRole ?? "Utilisateur",
                    LoginTime = session.LoginTime,
                    ExpiresAt = session.LoginTime.Add(_defaultSessionDuration),
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                await _appLogger.LogInfoAsync("Authentication", "SessionCreated",
                    $"Session créée pour utilisateur {userId} au centre {hospitalCenterId}",
                    userId, hospitalCenterId, details: new { SessionToken = sessionToken });

                return sessionInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de session pour utilisateur {UserId}", userId);
                throw;
            }
        }

        public async Task<SessionValidationResult> ValidateSessionAsync(string sessionToken)
        {
            try
            {
                var session = await _context.UserSessions
                    .Include(us => us.User)
                    .Include(us => us.CurrentHospitalCenter)
                    .FirstOrDefaultAsync(us => us.SessionToken == sessionToken);

                if (session == null)
                {
                    return new SessionValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Session introuvable"
                    };
                }

                if (!session.IsActive)
                {
                    return new SessionValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Session terminée"
                    };
                }

                // Vérifier l'expiration
                var expiryTime = session.LoginTime.Add(_defaultSessionDuration);
                var now = TimeZoneHelper.GetCameroonTime();

                if (now > expiryTime)
                {
                    // Marquer la session comme expirée
                    session.IsActive = false;
                    session.LogoutTime = now;
                    await _context.SaveChangesAsync();

                    return new SessionValidationResult
                    {
                        IsValid = false,
                        IsExpired = true,
                        ErrorMessage = "Session expirée"
                    };
                }

                // Récupérer le rôle de l'utilisateur dans le centre actuel
                var assignment = await _context.UserCenterAssignments
                    .FirstOrDefaultAsync(uca => uca.UserId == session.UserId &&
                                               uca.HospitalCenterId == session.CurrentHospitalCenterId &&
                                               uca.IsActive);

                var sessionInfo = new SessionInfo
                {
                    SessionToken = sessionToken,
                    UserId = session.UserId,
                    UserName = $"{session.User.FirstName} {session.User.LastName}",
                    CurrentHospitalCenterId = session.CurrentHospitalCenterId,
                    CurrentCenterName = session.CurrentHospitalCenter.Name,
                    CurrentRole = assignment?.RoleType ?? "Utilisateur",
                    LoginTime = session.LoginTime,
                    ExpiresAt = expiryTime,
                    IpAddress = session.IpAddress
                };

                return new SessionValidationResult
                {
                    IsValid = true,
                    SessionInfo = sessionInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation de session {SessionToken}", sessionToken);

                return new SessionValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Erreur de validation"
                };
            }
        }

        public async Task<SessionDetails?> GetSessionDetailsAsync(string sessionToken)
        {
            try
            {
                var session = await _context.UserSessions
                    .Include(us => us.User)
                        .ThenInclude(u => u.UserCenterAssignments.Where(uca => uca.IsActive))
                        .ThenInclude(uca => uca.HospitalCenter)
                    .Include(us => us.CurrentHospitalCenter)
                    .FirstOrDefaultAsync(us => us.SessionToken == sessionToken);

                if (session == null || !session.IsActive) return null;

                // Récupérer l'affectation dans le centre actuel
                var currentAssignment = session.User.UserCenterAssignments
                    .FirstOrDefault(uca => uca.HospitalCenterId == session.CurrentHospitalCenterId && uca.IsActive);

                // Construire les informations de session
                var sessionInfo = new SessionInfo
                {
                    SessionToken = sessionToken,
                    UserId = session.UserId,
                    UserName = $"{session.User.FirstName} {session.User.LastName}",
                    CurrentHospitalCenterId = session.CurrentHospitalCenterId,
                    CurrentCenterName = session.CurrentHospitalCenter.Name,
                    CurrentRole = currentAssignment?.RoleType ?? "Utilisateur",
                    LoginTime = session.LoginTime,
                    ExpiresAt = session.LoginTime.Add(_defaultSessionDuration),
                    IpAddress = session.IpAddress
                };

                // Récupérer tous les centres accessibles
                var accessibleCenters = session.User.UserCenterAssignments
                    .Where(uca => uca.IsActive)
                    .Select(uca => new CenterAssignmentInfo
                    {
                        HospitalCenterId = uca.HospitalCenterId,
                        CenterName = uca.HospitalCenter.Name,
                        CenterAddress = uca.HospitalCenter.Address,
                        RoleType = uca.RoleType,
                        AssignmentStartDate = uca.AssignmentStartDate,
                        AssignmentEndDate = uca.AssignmentEndDate
                    }).ToList();

                return new SessionDetails
                {
                    SessionInfo = sessionInfo,
                    User = session.User,
                    CurrentCenter = session.CurrentHospitalCenter,
                    AccessibleCenters = accessibleCenters
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des détails de session {SessionToken}", sessionToken);
                return null;
            }
        }

        public async Task<bool> ExtendSessionAsync(string sessionToken, int additionalMinutes = 720)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(us => us.SessionToken == sessionToken && us.IsActive);

                if (session == null) return false;

                // Mettre à jour l'heure de connexion pour prolonger la session
                session.LoginTime = TimeZoneHelper.GetCameroonTime();
                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("Authentication", "SessionExtended",
                    $"Session prolongée: {sessionToken}",
                    session.UserId, details: new { SessionToken = sessionToken, AdditionalMinutes = additionalMinutes });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la prolongation de session {SessionToken}", sessionToken);
                return false;
            }
        }

        public async Task<List<SessionInfo>> GetUserActiveSessionsAsync(int userId)
        {
            try
            {
                var sessions = await _context.UserSessions
                    .Include(us => us.User)
                    .Include(us => us.CurrentHospitalCenter)
                    .Where(us => us.UserId == userId && us.IsActive)
                    .ToListAsync();

                var result = new List<SessionInfo>();

                foreach (var session in sessions)
                {
                    // Vérifier si la session n'est pas expirée
                    var expiryTime = session.LoginTime.Add(_defaultSessionDuration);
                    if (TimeZoneHelper.GetCameroonTime() <= expiryTime)
                    {
                        var assignment = await _context.UserCenterAssignments
                            .FirstOrDefaultAsync(uca => uca.UserId == session.UserId &&
                                                       uca.HospitalCenterId == session.CurrentHospitalCenterId &&
                                                       uca.IsActive);

                        result.Add(new SessionInfo
                        {
                            SessionToken = session.SessionToken,
                            UserId = session.UserId,
                            UserName = $"{session.User.FirstName} {session.User.LastName}",
                            CurrentHospitalCenterId = session.CurrentHospitalCenterId,
                            CurrentCenterName = session.CurrentHospitalCenter.Name,
                            CurrentRole = assignment?.RoleType ?? "Utilisateur",
                            LoginTime = session.LoginTime,
                            ExpiresAt = expiryTime,
                            IpAddress = session.IpAddress
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sessions actives pour utilisateur {UserId}", userId);
                return new List<SessionInfo>();
            }
        }

        // ===== GESTION DES CENTRES =====

        public async Task<bool> SwitchCenterAsync(string sessionToken, int newCenterId)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(us => us.SessionToken == sessionToken && us.IsActive);

                if (session == null) return false;

                //// Vérifier que l'utilisateur a accès au nouveau centre
                //var hasAccess = await _userRepository.HasAccessToCenterAsync(session.UserId, newCenterId);
                //if (!hasAccess) return false;

                var oldCenterId = session.CurrentHospitalCenterId;
                session.CurrentHospitalCenterId = newCenterId;
                await _context.SaveChangesAsync();

                // Mettre à jour le dernier centre sélectionné
                await SaveLastSelectedCenterAsync(session.UserId, newCenterId);

                await _appLogger.LogInfoAsync("Authentication", "CenterSwitched",
                    $"Changement de centre pour utilisateur {session.UserId}: {oldCenterId} → {newCenterId}",
                    session.UserId, newCenterId, details: new { SessionToken = sessionToken, OldCenterId = oldCenterId });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de centre pour session {SessionToken}", sessionToken);
                return false;
            }
        }

        public async Task<List<CenterAssignmentInfo>> GetUserAccessibleCentersAsync(int userId)
        {
            try
            {
                return await _userRepository.GetUserActiveCentersAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des centres accessibles pour utilisateur {UserId}", userId);
                return new List<CenterAssignmentInfo>();
            }
        }

        public async Task<bool> SaveLastSelectedCenterAsync(int userId, int centerId)
        {
            try
            {
                var lastSelected = await _context.UserLastSelectedCenters
                    .FirstOrDefaultAsync(ulsc => ulsc.UserId == userId);

                if (lastSelected == null)
                {
                    lastSelected = new UserLastSelectedCenter
                    {
                        UserId = userId,
                        LastSelectedHospitalCenterId = centerId,
                        LastSelectionDate = TimeZoneHelper.GetCameroonTime()
                    };
                    _context.UserLastSelectedCenters.Add(lastSelected);
                }
                else
                {
                    lastSelected.LastSelectedHospitalCenterId = centerId;
                    lastSelected.LastSelectionDate = TimeZoneHelper.GetCameroonTime();
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde du dernier centre sélectionné pour utilisateur {UserId}", userId);
                return false;
            }
        }

        public async Task<int?> GetLastSelectedCenterAsync(int userId)
        {
            try
            {
                var lastSelected = await _context.UserLastSelectedCenters
                    .FirstOrDefaultAsync(ulsc => ulsc.UserId == userId);

                return lastSelected?.LastSelectedHospitalCenterId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dernier centre sélectionné pour utilisateur {UserId}", userId);
                return null;
            }
        }

        // ===== GESTION DES MOTS DE PASSE =====

        public async Task<PasswordChangeResult> ChangePasswordAsync(
            int userId, string currentPassword, string newPassword)
        {
            try
            {
                // Valider la force du nouveau mot de passe
                var validation = await ValidatePasswordAsync(newPassword, userId);
                if (!validation.IsValid)
                {
                    return new PasswordChangeResult
                    {
                        IsSuccess = false,
                        ValidationErrors = validation.Errors
                    };
                }

                // Changer le mot de passe via le repository
                var success = await _userRepository.ChangePasswordAsync(userId, currentPassword, newPassword);

                if (success)
                {
                    await _appLogger.LogInfoAsync("Authentication", "PasswordChanged",
                        $"Mot de passe changé avec succès pour utilisateur {userId}",
                        userId);

                    return new PasswordChangeResult { IsSuccess = true };
                }

                return new PasswordChangeResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Mot de passe actuel incorrect ou erreur lors du changement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de mot de passe pour utilisateur {UserId}", userId);

                return new PasswordChangeResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Erreur système lors du changement de mot de passe"
                };
            }
        }

        public async Task<PasswordResetResult> ResetPasswordAsync(int userId, int resetBy)
        {
            try
            {
                var result = await _userRepository.ResetPasswordAsync(userId, resetBy);

                if (result.Success)
                {
                    await _appLogger.LogInfoAsync("Authentication", "PasswordReset",
                        $"Mot de passe réinitialisé pour utilisateur {userId} par {resetBy}",
                        resetBy, details: new { TargetUserId = userId });

                    return new PasswordResetResult
                    {
                        IsSuccess = true,
                        TemporaryPassword = result.TempPassword,
                        ExpiresAt = TimeZoneHelper.GetCameroonTime().AddDays(7) // Expiration 7 jours
                    };
                }

                return new PasswordResetResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Erreur lors de la réinitialisation du mot de passe"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la réinitialisation de mot de passe pour utilisateur {UserId}", userId);

                return new PasswordResetResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Erreur système lors de la réinitialisation"
                };
            }
        }

        public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, int? userId = null)
        {
            try
            {
                var result = _passwordHasher.ValidatePasswordStrength(password);

                // Ajouts de validations spécifiques si nécessaire
                if (userId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(userId.Value);
                    if (user != null)
                    {
                        // Vérifier que le nouveau mot de passe n'est pas identique au nom/email
                        var lowerPassword = password.ToLower();
                        if (lowerPassword.Contains(user.FirstName.ToLower()) ||
                            lowerPassword.Contains(user.LastName.ToLower()) ||
                            lowerPassword.Contains(user.Email.Split('@')[0].ToLower()))
                        {
                            result.IsValid = false;
                            result.Errors.Add("Le mot de passe ne doit pas contenir votre nom ou email");
                        }
                    }
                }

                return new PasswordValidationResult
                {
                    IsValid = result.IsValid,
                    Errors = result.Errors,
                    StrengthScore = result.StrengthScore,
                    StrengthLevel = result.StrengthLevel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation de mot de passe");

                return new PasswordValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "Erreur lors de la validation" }
                };
            }
        }

        public async Task<bool> ForcePasswordChangeAsync(int userId, int forcedBy)
        {
            try
            {
                var success = await _userRepository.ForcePasswordChangeAsync(userId, true);

                if (success)
                {
                    await _appLogger.LogInfoAsync("Authentication", "PasswordChangeForced",
                        $"Changement de mot de passe forcé pour utilisateur {userId} par {forcedBy}",
                        forcedBy, details: new { TargetUserId = userId });
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la contrainte de changement de mot de passe pour utilisateur {UserId}", userId);
                return false;
            }
        }


        public async Task<UserStatusCheck> CheckUserStatusAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new UserStatusCheck
                    {
                        IsActive = false,
                        Status = "UserNotFound"
                    };
                }

                var now = TimeZoneHelper.GetCameroonTime();
                var daysSinceLastLogin = user.LastLoginDate.HasValue
                    ? (now.Date - user.LastLoginDate.Value.Date).Days
                    : int.MaxValue;

                return new UserStatusCheck
                {
                    IsActive = user.IsActive,
                    IsLocked = false, // Pas de système de verrouillage dans cette implémentation
                    RequiresPasswordChange = user.MustChangePassword,
                    LastLoginDate = user.LastLoginDate,
                    DaysSinceLastLogin = daysSinceLastLogin,
                    Status = user.IsActive ? "Active" : "Inactive"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du statut pour utilisateur {UserId}", userId);

                return new UserStatusCheck
                {
                    IsActive = false,
                    Status = "Error"
                };
            }
        }

        public async Task LogUnauthorizedAccessAttemptAsync(int? userId, string action, string? ipAddress = null)
        {
            try
            {
                await _appLogger.LogWarningAsync("Security", "UnauthorizedAccess",
                    $"Tentative d'accès non autorisé à l'action: {action}",
                    userId, details: new { Action = action, IpAddress = ipAddress });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement de tentative d'accès non autorisé");
            }
        }

        // ===== UTILITAIRES CRYPTOGRAPHIQUES =====

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hash)
        {
            return _passwordHasher.VerifyPassword(password, hash);
        }

        public async Task<string> GenerateTemporaryPasswordAsync()
        {
            return await _passwordHasher.GenerateTemporaryPasswordAsync();
        }

        // ===== MONITORING ET AUDIT =====

        public async Task<int> CleanExpiredSessionsAsync()
        {
            try
            {
                var cutoffTime = TimeZoneHelper.GetCameroonTime().Subtract(_defaultSessionDuration);

                var expiredSessions = await _context.UserSessions
                    .Where(us => us.IsActive && us.LoginTime < cutoffTime)
                    .ToListAsync();

                foreach (var session in expiredSessions)
                {
                    session.IsActive = false;
                    session.LogoutTime = TimeZoneHelper.GetCameroonTime();
                }

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("Authentication", "ExpiredSessionsCleaned",
                    $"{expiredSessions.Count} sessions expirées nettoyées",
                    details: new { expiredSessions.Count });

                return expiredSessions.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage des sessions expirées");
                return 0;
            }
        }

        public async Task<AuthenticationStatistics> GetAuthenticationStatisticsAsync(
            DateTime? fromDate = null, DateTime? toDate = null, int? hospitalCenterId = null)
        {
            try
            {
                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                // Pour cette implémentation, on simule les statistiques
                // Dans un vrai système, ces données viendraient des logs d'audit
                var stats = new AuthenticationStatistics
                {
                    TotalLoginAttempts = await _context.UserSessions
                        .Where(us => us.LoginTime >= from && us.LoginTime <= to)
                        .CountAsync(),
                    SuccessfulLogins = await _context.UserSessions
                        .Where(us => us.LoginTime >= from && us.LoginTime <= to)
                        .CountAsync(),
                    UniqueUsersLoggedIn = await _context.UserSessions
                        .Where(us => us.LoginTime >= from && us.LoginTime <= to)
                        .Select(us => us.UserId)
                        .Distinct()
                        .CountAsync()
                };

                stats.FailedLogins = stats.TotalLoginAttempts - stats.SuccessfulLogins;

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération des statistiques d'authentification");
                return new AuthenticationStatistics();
            }
        }

        // ===== MÉTHODES PRIVÉES =====

        /// <summary>
        /// Génère un token de session sécurisé
        /// </summary>
        private static string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes);
        }
    }
}