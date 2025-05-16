using AutoMapper;
using DocumentFormat.OpenXml.InkML;
using HManagSys.Data.DBContext;
using HManagSys.Data.Repositories;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Users;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Data.Repositories
{
    /// <summary>
    /// Repository spécialisé pour la gestion des utilisateurs
    /// Comme un responsable RH expert qui connaît chaque détail
    /// de chaque employé de l'hôpital
    /// </summary>
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly IApplicationLogger _appLogger;
        private readonly IPasswordHasher _passwordHasher; // Service pour hasher les mots de passe

        public UserRepository(
            HospitalManagementContext context,
            IMapper mapper,
            ILogger<UserRepository> logger,
            IApplicationLogger appLogger,
            IPasswordHasher passwordHasher)
            : base(context, mapper, logger)
        {
            _appLogger = appLogger;
            _passwordHasher = passwordHasher;
        }

        // ===== RECHERCHES SPÉCIALISÉES =====

        /// <summary>
        /// Recherche un utilisateur par email avec gestion d'erreurs robuste
        /// Cette méthode est critique pour l'authentification
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                // Validation de base - comme vérifier qu'on a bien reçu un nom
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                // Recherche insensible à la casse avec chargement des relations
                // Comme chercher dans un annuaire sans se préoccuper des majuscules
                return await GetSingleAsync(q => q
                    .Include(u => u.UserCenterAssignments.Where(uca => uca.IsActive))
                    .ThenInclude(uca => uca.HospitalCenter)
                    .Where(u => u.Email.ToLower() == email.ToLower()));
            }
            catch (Exception ex)
            {
                // Log avec contexte spécifique utilisateur
                await _appLogger.LogErrorAsync("UserRepository", "GetByEmailFailed",
                    $"Erreur lors de la recherche par email: {email}",
                    details: new { Email = email, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Recherche avancée avec pagination et filtres multiples
        /// Comme un moteur de recherche intelligent pour les RH
        /// </summary>
        public async Task<(List<UserSummary> Users, int TotalCount)> SearchUsersAsync(
            string? searchTerm = null,
            bool? isActive = null,
            string? roleFilter = null,
            int? hospitalCenterId = null,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                // Construction de requête progressive - comme affiner une recherche
                var query = _context.Users
                    .Include(u => u.UserCenterAssignments.Where(uca => uca.IsActive))
                    .ThenInclude(uca => uca.HospitalCenter)
                    .AsQueryable();

                // Filtrage par terme de recherche (nom, email, téléphone)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.ToLower();
                    query = query.Where(u =>
                        u.FirstName.ToLower().Contains(term) ||
                        u.LastName.ToLower().Contains(term) ||
                        u.Email.ToLower().Contains(term) ||
                        u.PhoneNumber.Contains(term));
                }

                // Filtrage par statut actif/inactif
                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                // Filtrage par rôle dans un centre spécifique
                if (!string.IsNullOrWhiteSpace(roleFilter) || hospitalCenterId.HasValue)
                {
                    query = query.Where(u => u.UserCenterAssignments.Any(uca =>
                        uca.IsActive &&
                        (roleFilter == null || uca.RoleType == roleFilter) &&
                        (!hospitalCenterId.HasValue || uca.HospitalCenterId == hospitalCenterId)));
                }

                // Compter le total avant pagination - comme compter toutes les pages
                var totalCount = await query.CountAsync();

                // Projection vers UserSummary avec pagination
                // Comme créer un résumé de chaque dossier pour la vue d'ensemble
                var users = await query
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserSummary
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        IsActive = u.IsActive,
                        LastLoginDate = u.LastLoginDate,
                        MustChangePassword = u.MustChangePassword,
                        Assignments = u.UserCenterAssignments
                            .Where(uca => uca.IsActive)
                            .Select(uca => new AssignmentInfo
                            {
                                HospitalCenterId = uca.HospitalCenterId,
                                HospitalCenterName = uca.HospitalCenter.Name,
                                RoleType = uca.RoleType,
                                IsActive = uca.IsActive
                            }).ToList()
                    })
                    .ToListAsync();

                // Log de la recherche pour statistiques d'utilisation
                await _appLogger.LogInfoAsync("UserRepository", "SearchPerformed",
                    $"Recherche utilisateurs: {users.Count}/{totalCount} résultats",
                    details: new
                    {
                        SearchTerm = searchTerm,
                        IsActive = isActive,
                        RoleFilter = roleFilter,
                        HospitalCenterId = hospitalCenterId
                    });

                return (users, totalCount);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "SearchUsersFailed",
                    "Erreur lors de la recherche d'utilisateurs",
                    details: new { SearchTerm = searchTerm, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère les utilisateurs récemment actifs
        /// Utile pour les rapports de fréquentation et de performance
        /// </summary>
        public async Task<List<User>> GetRecentlyActiveUsersAsync(int days = 30, int? hospitalCenterId = null)
        {
            try
            {
                var cutoffDate = TimeZoneHelper.GetCameroonTime().AddDays(-days);

                var query = _context.Users
                    .Include(u => u.UserCenterAssignments.Where(uca => uca.IsActive))
                    .ThenInclude(uca => uca.HospitalCenter)
                    .Where(u => u.IsActive && u.LastLoginDate >= cutoffDate);

                // Filtrer par centre si spécifié
                if (hospitalCenterId.HasValue)
                {
                    query = query.Where(u => u.UserCenterAssignments
                        .Any(uca => uca.IsActive && uca.HospitalCenterId == hospitalCenterId));
                }

                return await query
                    .OrderByDescending(u => u.LastLoginDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "GetRecentlyActiveFailed",
                    $"Erreur lors de la récupération des utilisateurs actifs ({days} jours)",
                    details: new { Days = days, HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère les utilisateurs par rôle dans un centre
        /// Essential pour la gestion des équipes
        /// </summary>
        public async Task<List<User>> GetUsersByRoleAndCenterAsync(string roleType, int hospitalCenterId)
        {
            try
            {
                return await GetAllAsync(q => q
                    .Include(u => u.UserCenterAssignments.Where(uca => uca.IsActive))
                    .ThenInclude(uca => uca.HospitalCenter)
                    .Where(u => u.IsActive &&
                               u.UserCenterAssignments.Any(uca =>
                                   uca.IsActive &&
                                   uca.RoleType == roleType &&
                                   uca.HospitalCenterId == hospitalCenterId))
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)) as List<User>;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "GetUsersByRoleFailed",
                    $"Erreur lors de la récupération par rôle {roleType} au centre {hospitalCenterId}",
                    details: new { RoleType = roleType, HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        // ===== GESTION DES MOTS DE PASSE =====

        /// <summary>
        /// Réinitialise le mot de passe avec génération automatique
        /// Processus sécurisé avec audit complet
        /// </summary>
        public async Task<(bool Success, string TempPassword)> ResetPasswordAsync(int userId, int resetBy)
        {
            try
            {
                // Trouver l'utilisateur - comme localiser un dossier spécifique
                var user = await GetByIdAsync(userId);
                if (user == null)
                {
                    await _appLogger.LogWarningAsync("UserRepository", "ResetPasswordUserNotFound",
                        $"Tentative de réinitialisation pour utilisateur inexistant: {userId}",
                        details: new { UserId = userId, ResetBy = resetBy });
                    return (false, string.Empty);
                }

                // Générer un mot de passe temporaire simple
                // Format: HospXXXX où XXXX est un nombre aléatoire
                var tempPassword = GenerateSimplePassword();

                // Hasher le nouveau mot de passe
                var hashedPassword = _passwordHasher.HashPassword(tempPassword);

                // Mise à jour des champs concernés
                user.PasswordHash = hashedPassword;
                user.MustChangePassword = true; // Force le changement à la prochaine connexion
                user.ModifiedBy = resetBy;
                user.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                // Sauvegarder les changements
                await _context.SaveChangesAsync();

                // Log de l'opération pour audit et sécurité
                await _appLogger.LogInfoAsync("UserRepository", "PasswordReset",
                    $"Mot de passe réinitialisé pour {user.Email}",
                    resetBy, details: new
                    {
                        TargetUserId = userId,
                        TargetEmail = user.Email,
                        TempPasswordLength = tempPassword.Length
                    });

                return (true, tempPassword);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "ResetPasswordFailed",
                    $"Erreur lors de la réinitialisation pour utilisateur {userId}",
                    details: new { UserId = userId, ResetBy = resetBy, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Change le mot de passe avec validation
        /// Processus sécurisé avec vérification de l'ancien mot de passe
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                // Vérifier l'ancien mot de passe si fourni (pas pour changement forcé)
                if (!string.IsNullOrEmpty(currentPassword))
                {
                    if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
                    {
                        await _appLogger.LogWarningAsync("UserRepository", "PasswordChangeInvalidCurrent",
                            $"Tentative de changement avec mauvais mot de passe actuel pour {user.Email}",
                            userId);
                        return false;
                    }
                }

                // Hasher le nouveau mot de passe
                user.PasswordHash = _passwordHasher.HashPassword(newPassword);
                user.MustChangePassword = false; // L'utilisateur a changé son mot de passe
                user.ModifiedBy = userId; // L'utilisateur se modifie lui-même
                user.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("UserRepository", "PasswordChanged",
                    $"Mot de passe changé pour {user.Email}",
                    userId);

                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "ChangePasswordFailed",
                    $"Erreur lors du changement de mot de passe pour utilisateur {userId}",
                    details: new { UserId = userId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Force ou lève l'obligation de changer le mot de passe
        /// </summary>
        public async Task<bool> ForcePasswordChangeAsync(int userId, bool mustChange = true)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.MustChangePassword = mustChange;
                user.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("UserRepository", "ForcePasswordChange",
                    $"Obligation de changement de mot de passe {(mustChange ? "activée" : "levée")} pour {user.Email}",
                    details: new { UserId = userId, MustChange = mustChange });

                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "ForcePasswordChangeFailed",
                    $"Erreur lors de la modification de l'obligation de changement pour {userId}",
                    details: new { UserId = userId, Error = ex.Message });
                throw;
            }
        }

        // ===== GESTION D'ACTIVITÉ =====

        /// <summary>
        /// Enregistre une connexion réussie
        /// Met à jour la date de dernière connexion pour audit et statistiques
        /// </summary>
        public async Task<bool> RecordLoginAsync(int userId, string? ipAddress = null)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.LastLoginDate = TimeZoneHelper.GetCameroonTime();
                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("UserRepository", "LoginRecorded",
                    $"Connexion enregistrée pour {user.Email}",
                    userId, details: new { IpAddress = ipAddress });

                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "RecordLoginFailed",
                    $"Erreur lors de l'enregistrement de connexion pour {userId}",
                    details: new { UserId = userId, IpAddress = ipAddress, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Active ou désactive un compte utilisateur
        /// Soft delete/restore avec audit complet
        /// </summary>
        public async Task<bool> SetUserActiveStatusAsync(int userId, bool isActive, int modifiedBy)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                var oldStatus = user.IsActive;
                user.IsActive = isActive;
                user.ModifiedBy = modifiedBy;
                user.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("UserRepository", "UserStatusChanged",
                    $"Statut utilisateur {user.Email} changé: {oldStatus} → {isActive}",
                    modifiedBy, details: new
                    {
                        TargetUserId = userId,
                        TargetEmail = user.Email,
                        OldStatus = oldStatus,
                        NewStatus = isActive
                    });

                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "SetUserActiveStatusFailed",
                    $"Erreur lors du changement de statut pour utilisateur {userId}",
                    details: new { UserId = userId, IsActive = isActive, ModifiedBy = modifiedBy, Error = ex.Message });
                throw;
            }
        }

        // ===== CENTRES ET AFFECTATIONS =====

        /// <summary>
        /// Récupère un utilisateur avec toutes ses affectations
        /// Vision complète du profil utilisateur
        /// </summary>
        public async Task<User?> GetUserWithAssignmentsAsync(int userId)
        {
            try
            {
                return await GetSingleAsync(q => q
                    .Include(u => u.UserCenterAssignments)
                    .ThenInclude(uca => uca.HospitalCenter)
                    .Where(u => u.Id == userId));
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "GetUserWithAssignmentsFailed",
                    $"Erreur lors de la récupération des affectations pour {userId}",
                    details: new { UserId = userId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère les centres actifs d'un utilisateur
        /// Pour l'interface de sélection de centre
        /// </summary>
        public async Task<List<CenterAssignmentInfo>> GetUserActiveCentersAsync(int userId)
        {
            try
            {
                var userAssignments = await _context.UserCenterAssignments
                    .Include(uca => uca.HospitalCenter)
                    .Where(uca => uca.UserId == userId &&
                                  uca.IsActive &&
                                  uca.HospitalCenter.IsActive)
                    .OrderBy(uca => uca.HospitalCenter.Name)
                    .ToListAsync();

                // Récupérer le dernier centre sélectionné
                var lastSelected = await _context.UserLastSelectedCenters
                    .Where(ulsc => ulsc.UserId == userId)
                    .FirstOrDefaultAsync();

                // Convertir en DTO avec indication du dernier sélectionné
                return userAssignments.Select(uca => new CenterAssignmentInfo
                {
                    HospitalCenterId = uca.HospitalCenterId,
                    CenterName = uca.HospitalCenter.Name,
                    CenterAddress = uca.HospitalCenter.Address,
                    RoleType = uca.RoleType,
                    IsLastSelected = lastSelected?.LastSelectedHospitalCenterId == uca.HospitalCenterId,
                    AssignmentStartDate = uca.AssignmentStartDate,
                    AssignmentEndDate = uca.AssignmentEndDate
                }).ToList();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "GetUserActiveCentersFailed",
                    $"Erreur lors de la récupération des centres actifs pour {userId}",
                    details: new { UserId = userId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Vérifie l'accès d'un utilisateur à un centre
        /// Essential pour les contrôles d'autorisation
        /// </summary>
        public async Task<bool> HasAccessToCenterAsync(int userId, int hospitalCenterId, string? roleType = null)
        {
            try
            {
                var query = _context.UserCenterAssignments
                    .Where(uca => uca.UserId == userId &&
                                  uca.HospitalCenterId == hospitalCenterId &&
                                  uca.IsActive);

                if (!string.IsNullOrEmpty(roleType))
                {
                    query = query.Where(uca => uca.RoleType == roleType);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "HasAccessToCenterFailed",
                    $"Erreur lors de la vérification d'accès pour utilisateur {userId} au centre {hospitalCenterId}",
                    details: new { UserId = userId, HospitalCenterId = hospitalCenterId, RoleType = roleType, Error = ex.Message });
                throw;
            }
        }

        // ===== STATISTIQUES ET RAPPORTS =====

        /// <summary>
        /// Génère des statistiques sur les utilisateurs
        /// Pour les tableaux de bord administratifs
        /// </summary>
        public async Task<UserStatistics> GetUserStatisticsAsync(int? hospitalCenterId = null)
        {
            try
            {
                var today = TimeZoneHelper.GetCameroonTime().Date;
                var weekAgo = today.AddDays(-7);

                var query = _context.Users.AsQueryable();

                // Filtrer par centre si spécifié
                if (hospitalCenterId.HasValue)
                {
                    query = query.Where(u => u.UserCenterAssignments
                        .Any(uca => uca.IsActive && uca.HospitalCenterId == hospitalCenterId));
                }

                // Calculer toutes les statistiques en une seule requête pour optimiser
                var stats = await query
                    .GroupBy(u => 1) // Groupe tout ensemble pour faire les agrégations
                    .Select(g => new UserStatistics
                    {
                        TotalUsers = g.Count(),
                        ActiveUsers = g.Count(u => u.IsActive),
                        InactiveUsers = g.Count(u => !u.IsActive),
                        UsersLoggedToday = g.Count(u => u.LastLoginDate >= today),
                        UsersLoggedThisWeek = g.Count(u => u.LastLoginDate >= weekAgo),
                        UsersRequiringPasswordChange = g.Count(u => u.MustChangePassword)
                    })
                    .FirstOrDefaultAsync();

                // Calculer les statistiques par rôle séparément pour éviter la complexité
                if (hospitalCenterId.HasValue)
                {
                    stats.SuperAdmins = await _context.UserCenterAssignments
                        .Where(uca => uca.HospitalCenterId == hospitalCenterId &&
                                      uca.IsActive &&
                                      uca.RoleType == "SuperAdmin")
                        .CountAsync();

                    stats.MedicalStaff = await _context.UserCenterAssignments
                        .Where(uca => uca.HospitalCenterId == hospitalCenterId &&
                                      uca.IsActive &&
                                      uca.RoleType == "MedicalStaff")
                        .CountAsync();
                }

                // Retourner des statistiques vides si aucun utilisateur
                return stats ?? new UserStatistics();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "GetUserStatisticsFailed",
                    "Erreur lors du calcul des statistiques utilisateur",
                    details: new { HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère l'historique des connexions d'un utilisateur
        /// Pour l'audit de sécurité et le monitoring
        /// </summary>
        public async Task<List<UserLoginHistory>> GetUserLoginHistoryAsync(int userId, int days = 30)
        {
            try
            {
                var cutoffDate = TimeZoneHelper.GetCameroonTime().AddDays(-days);

                // Récupérer les sessions de l'utilisateur avec centre associé
                var sessions = await _context.UserSessions
                    .Include(us => us.CurrentHospitalCenter)
                    .Where(us => us.UserId == userId && us.LoginTime >= cutoffDate)
                    .OrderByDescending(us => us.LoginTime)
                    .ToListAsync();

                // Convertir en DTO pour l'historique
                return sessions.Select(s => new UserLoginHistory
                {
                    LoginTime = s.LoginTime,
                    IpAddress = s.IpAddress,
                    UserAgent = s.UserAgent,
                    CenterName = s.CurrentHospitalCenter.Name,
                    IsCurrentSession = s.IsActive && s.LogoutTime == null
                }).ToList();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserRepository", "GetUserLoginHistoryFailed",
                    $"Erreur lors de la récupération de l'historique pour utilisateur {userId}",
                    details: new { UserId = userId, Days = days, Error = ex.Message });
                throw;
            }
        }

        // ===== MÉTHODES PROTÉGÉES PERSONNALISÉES =====

        /// <summary>
        /// Personnalise la préparation d'insertion pour les utilisateurs
        /// Ajoute des validations et traitements spécifiques
        /// </summary>
        protected override void PrepareEntityForInsert(User entity)
        {
            base.PrepareEntityForInsert(entity);

            // Normaliser l'email en minuscules pour la cohérence
            entity.Email = entity.Email.ToLowerInvariant();

            // S'assurer que le mot de passe est fourni et hashé
            if (string.IsNullOrEmpty(entity.PasswordHash))
            {
                throw new InvalidOperationException("Le mot de passe est obligatoire pour créer un utilisateur");
            }

            // Par défaut, nouvel utilisateur doit changer son mot de passe
            entity.MustChangePassword = true;

            _logger.LogDebug("Utilisateur préparé pour insertion: {Email}", entity.Email);
        }

        /// <summary>
        /// Personnalise la préparation de mise à jour pour les utilisateurs
        /// Protège les champs sensibles et ajoute des validations
        /// </summary>
        protected override void PrepareEntityForUpdate(User entity)
        {
            base.PrepareEntityForUpdate(entity);

            // Normaliser l'email si modifié
            entity.Email = entity.Email.ToLowerInvariant();

            _logger.LogDebug("Utilisateur préparé pour mise à jour: {UserId} - {Email}", entity.Id, entity.Email);
        }

        /// <summary>
        /// Personnalise les propriétés exclues pour les utilisateurs
        /// Protège les champs critiques contre modifications accidentelles
        /// </summary>
        protected override List<string> GetDefaultExcludedProperties()
        {
            var excluded = base.GetDefaultExcludedProperties();

            // Protéger les champs sensibles
            excluded.Add(nameof(User.PasswordHash)); // Mot de passe modifié via méthodes spécialisées
            excluded.Add(nameof(User.LastLoginDate)); // Mis à jour via RecordLoginAsync

            return excluded;
        }

        // ===== MÉTHODES UTILITAIRES PRIVÉES =====

        /// <summary>
        /// Génère un mot de passe temporaire simple
        /// Format: HospXXXX où XXXX est un nombre aléatoire
        /// </summary>
        private static string GenerateSimplePassword()
        {
            var random = new Random();
            var number = random.Next(1000, 9999);
            return $"Hosp{number}";
        }
    }
}