using AutoMapper;
using DocumentFormat.OpenXml.InkML;
using HManagSys.Data.DBContext;
using HManagSys.Data.Repositories;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Data.Repositories
{
    /// <summary>
    /// Repository spécialisé pour la gestion des affectations utilisateur-centre
    /// Comme un responsable RH expert en affectations et mutations
    /// Gère la complexité des rôles multiples et des changements d'affectation
    /// </summary>
    public class UserCenterAssignmentRepository : GenericRepository<UserCenterAssignment>, IUserCenterAssignmentRepository
    {
        private readonly IApplicationLogger _appLogger;

        public UserCenterAssignmentRepository(
            HospitalManagementContext context,
            IMapper mapper,
            ILogger<UserCenterAssignmentRepository> logger,
            IApplicationLogger appLogger)
            : base(context, mapper, logger)
        {
            _appLogger = appLogger;
        }

        // ===== RECHERCHES ET CONSULTATIONS =====

        /// <summary>
        /// Récupère toutes les affectations actives d'un utilisateur
        /// Vue complète des responsabilités de l'utilisateur
        /// </summary>
        public async Task<List<UserCenterAssignment>> GetUserActiveAssignmentsAsync(int userId)
        {
            try
            {
                return await GetAllAsync(q => q
                    .Include(uca => uca.HospitalCenter)
                    .Include(uca => uca.User)
                    .Where(uca => uca.UserId == userId && uca.IsActive)
                    .OrderBy(uca => uca.HospitalCenter.Name)) as List<UserCenterAssignment>;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "GetUserActiveAssignmentsFailed",
                    $"Erreur lors de la récupération des affectations actives pour utilisateur {userId}",
                    details: new { UserId = userId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère tous les utilisateurs affectés à un centre avec leurs rôles
        /// Liste d'équipe pour un centre spécifique
        /// </summary>
        public async Task<List<UserCenterAssignment>> GetCenterActiveAssignmentsAsync(int hospitalCenterId)
        {
            try
            {
                return await GetAllAsync(q => q
                    .Include(uca => uca.User)
                    .Include(uca => uca.HospitalCenter)
                    .Where(uca => uca.HospitalCenterId == hospitalCenterId &&
                                  uca.IsActive &&
                                  uca.User.IsActive)
                    .OrderBy(uca => uca.RoleType)
                    .ThenBy(uca => uca.User.LastName)) as List<UserCenterAssignment>;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "GetCenterActiveAssignmentsFailed",
                    $"Erreur lors de la récupération des affectations pour centre {hospitalCenterId}",
                    details: new { HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Vérifie si un utilisateur a une affectation spécifique
        /// Control d'autorisation essentialantage à la sécurité
        /// </summary>
        public async Task<bool> HasAssignmentAsync(int userId, int hospitalCenterId, string? roleType = null)
        {
            try
            {
                return await AnyAsync(q =>
                {
                    var query = q.Where(uca => uca.UserId == userId &&
                                              uca.HospitalCenterId == hospitalCenterId &&
                                              uca.IsActive);

                    if (!string.IsNullOrEmpty(roleType))
                    {
                        query = query.Where(uca => uca.RoleType == roleType);
                    }

                    return query;
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "HasAssignmentFailed",
                    $"Erreur lors de la vérification d'affectation pour utilisateur {userId}",
                    details: new { UserId = userId, HospitalCenterId = hospitalCenterId, RoleType = roleType, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère une affectation spécifique
        /// </summary>
        public async Task<UserCenterAssignment?> GetAssignmentAsync(int userId, int hospitalCenterId)
        {
            try
            {
                return await GetSingleAsync(q => q
                    .Include(uca => uca.User)
                    .Include(uca => uca.HospitalCenter)
                    .Where(uca => uca.UserId == userId &&
                                  uca.HospitalCenterId == hospitalCenterId));
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "GetAssignmentFailed",
                    $"Erreur lors de la récupération d'affectation spécifique",
                    details: new { UserId = userId, HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        // ===== GESTION DES AFFECTATIONS =====

        /// <summary>
        /// Crée une nouvelle affectation avec validation
        /// Processus contrôlé pour éviter les doublons et conflits
        /// </summary>
        public async Task<UserCenterAssignment> CreateAssignmentAsync(
            int userId, int hospitalCenterId, string roleType, int createdBy)
        {
            try
            {
                // Vérifier si l'affectation existe déjà
                var existingAssignment = await GetAssignmentAsync(userId, hospitalCenterId);

                if (existingAssignment != null)
                {
                    if (existingAssignment.IsActive)
                    {
                        throw new InvalidOperationException(
                            $"Utilisateur {userId} est déjà affecté au centre {hospitalCenterId}");
                    }

                    // Réactiver l'affectation existante si elle était désactivée
                    existingAssignment.IsActive = true;
                    existingAssignment.RoleType = roleType;
                    existingAssignment.AssignmentStartDate = TimeZoneHelper.GetCameroonTime();
                    existingAssignment.AssignmentEndDate = null;
                    existingAssignment.ModifiedBy = createdBy;
                    existingAssignment.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                    await _context.SaveChangesAsync();

                    await _appLogger.LogInfoAsync("UserCenterAssignmentRepository", "AssignmentReactivated",
                        $"Affectation réactivée: utilisateur {userId} au centre {hospitalCenterId} comme {roleType}",
                        createdBy, hospitalCenterId,
                        details: new { UserId = userId, HospitalCenterId = hospitalCenterId, RoleType = roleType });

                    return existingAssignment;
                }

                // Créer nouvelle affectation
                var newAssignment = new UserCenterAssignment
                {
                    UserId = userId,
                    HospitalCenterId = hospitalCenterId,
                    RoleType = roleType,
                    IsActive = true,
                    AssignmentStartDate = TimeZoneHelper.GetCameroonTime(),
                    CreatedBy = createdBy,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                var result = await AddAsync(newAssignment);

                await _appLogger.LogInfoAsync("UserCenterAssignmentRepository", "AssignmentCreated",
                    $"Nouvelle affectation créée: utilisateur {userId} au centre {hospitalCenterId} comme {roleType}",
                    createdBy, hospitalCenterId,
                    details: new { UserId = userId, HospitalCenterId = hospitalCenterId, RoleType = roleType });

                return result;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "CreateAssignmentFailed",
                    $"Erreur lors de la création d'affectation",
                    details: new { UserId = userId, HospitalCenterId = hospitalCenterId, RoleType = roleType, CreatedBy = createdBy, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Met à jour le rôle d'une affectation existante
        /// Changement de responsabilités avec audit complet
        /// </summary>
        public async Task<bool> UpdateRoleAsync(int userId, int hospitalCenterId, string newRoleType, int modifiedBy)
        {
            try
            {
                var assignment = await GetAssignmentAsync(userId, hospitalCenterId);
                if (assignment == null || !assignment.IsActive)
                {
                    await _appLogger.LogWarningAsync("UserCenterAssignmentRepository", "UpdateRoleAssignmentNotFound",
                        $"Tentative de modification de rôle pour affectation inexistante ou inactive",
                        details: new { UserId = userId, HospitalCenterId = hospitalCenterId, NewRoleType = newRoleType });
                    return false;
                }

                var oldRoleType = assignment.RoleType;
                assignment.RoleType = newRoleType;
                assignment.ModifiedBy = modifiedBy;
                assignment.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("UserCenterAssignmentRepository", "RoleUpdated",
                    $"Rôle modifié pour utilisateur {userId} au centre {hospitalCenterId}: {oldRoleType} → {newRoleType}",
                    modifiedBy, hospitalCenterId,
                    details: new
                    {
                        UserId = userId,
                        HospitalCenterId = hospitalCenterId,
                        OldRole = oldRoleType,
                        NewRole = newRoleType
                    });

                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "UpdateRoleFailed",
                    $"Erreur lors de la modification de rôle",
                    details: new { UserId = userId, HospitalCenterId = hospitalCenterId, NewRoleType = newRoleType, ModifiedBy = modifiedBy, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Termine une affectation avec date de fin
        /// Processus de départ ou mutation avec conservation de l'historique
        /// </summary>
        public async Task<bool> EndAssignmentAsync(int userId, int hospitalCenterId, int endedBy, DateTime? endDate = null)
        {
            try
            {
                var assignment = await GetAssignmentAsync(userId, hospitalCenterId);
                if (assignment == null || !assignment.IsActive)
                {
                    await _appLogger.LogWarningAsync("UserCenterAssignmentRepository", "EndAssignmentNotFound",
                        $"Tentative de fin d'affectation pour affectation inexistante ou déjà terminée",
                        details: new { UserId = userId, HospitalCenterId = hospitalCenterId });
                    return false;
                }

                assignment.IsActive = false;
                assignment.AssignmentEndDate = endDate ?? TimeZoneHelper.GetCameroonTime();
                assignment.ModifiedBy = endedBy;
                assignment.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("UserCenterAssignmentRepository", "AssignmentEnded",
                    $"Affectation terminée: utilisateur {userId} au centre {hospitalCenterId}",
                    endedBy, hospitalCenterId,
                    details: new
                    {
                        UserId = userId,
                        HospitalCenterId = hospitalCenterId,
                        EndDate = assignment.AssignmentEndDate
                    });

                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "EndAssignmentFailed",
                    $"Erreur lors de la fin d'affectation",
                    details: new { UserId = userId, HospitalCenterId = hospitalCenterId, EndedBy = endedBy, Error = ex.Message });
                throw;
            }
        }

        // ===== OPÉRATIONS EN BATCH =====

        /// <summary>
        /// Termine toutes les affectations d'un utilisateur
        /// Utilisé lors de la désactivation d'un compte
        /// </summary>
        public async Task<bool> EndAllUserAssignmentsAsync(int userId, int endedBy)
        {
            try
            {
                var activeAssignments = await GetUserActiveAssignmentsAsync(userId);

                foreach (var assignment in activeAssignments)
                {
                    assignment.IsActive = false;
                    assignment.AssignmentEndDate = TimeZoneHelper.GetCameroonTime();
                    assignment.ModifiedBy = endedBy;
                    assignment.ModifiedAt = TimeZoneHelper.GetCameroonTime();
                }

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("UserCenterAssignmentRepository", "AllUserAssignmentsEnded",
                    $"Toutes les affectations terminées pour utilisateur {userId} ({activeAssignments.Count} affectations)",
                    endedBy,
                    details: new { UserId = userId, AssignmentCount = activeAssignments.Count });

                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "EndAllUserAssignmentsFailed",
                    $"Erreur lors de la fin de toutes les affectations pour utilisateur {userId}",
                    details: new { UserId = userId, EndedBy = endedBy, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Termine toutes les affectations d'un centre
        /// Utilisé lors de la fermeture temporaire d'un centre
        /// </summary>
        public async Task<bool> EndAllCenterAssignmentsAsync(int hospitalCenterId, int endedBy)
        {
            try
            {
                var activeAssignments = await GetCenterActiveAssignmentsAsync(hospitalCenterId);

                foreach (var assignment in activeAssignments)
                {
                    assignment.IsActive = false;
                    assignment.AssignmentEndDate = TimeZoneHelper.GetCameroonTime();
                    assignment.ModifiedBy = endedBy;
                    assignment.ModifiedAt = TimeZoneHelper.GetCameroonTime();
                }

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("UserCenterAssignmentRepository", "AllCenterAssignmentsEnded",
                    $"Toutes les affectations terminées pour centre {hospitalCenterId} ({activeAssignments.Count} affectations)",
                    endedBy, hospitalCenterId,
                    details: new { HospitalCenterId = hospitalCenterId, AssignmentCount = activeAssignments.Count });

                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "EndAllCenterAssignmentsFailed",
                    $"Erreur lors de la fin de toutes les affectations pour centre {hospitalCenterId}",
                    details: new { HospitalCenterId = hospitalCenterId, EndedBy = endedBy, Error = ex.Message });
                throw;
            }
        }

        // ===== RECHERCHES AVANCÉES =====

        /// <summary>
        /// Recherche des affectations avec filtres multiples
        /// Outil d'analyse pour la gestion RH
        /// </summary>
        public async Task<(List<AssignmentDetails> Assignments, int TotalCount)> SearchAssignmentsAsync(
            string? searchTerm = null,
            string? roleType = null,
            int? hospitalCenterId = null,
            bool? isActive = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageIndex = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.UserCenterAssignments
                    .Include(uca => uca.User)
                    .Include(uca => uca.HospitalCenter)
                    .AsQueryable();

                // Filtrage par terme de recherche (nom utilisateur ou centre)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.ToLower();
                    query = query.Where(uca =>
                        uca.User.FirstName.ToLower().Contains(term) ||
                        uca.User.LastName.ToLower().Contains(term) ||
                        uca.User.Email.ToLower().Contains(term) ||
                        uca.HospitalCenter.Name.ToLower().Contains(term));
                }

                // Filtres spécifiques
                if (!string.IsNullOrEmpty(roleType))
                    query = query.Where(uca => uca.RoleType == roleType);

                if (hospitalCenterId.HasValue)
                    query = query.Where(uca => uca.HospitalCenterId == hospitalCenterId);

                if (isActive.HasValue)
                    query = query.Where(uca => uca.IsActive == isActive);

                if (fromDate.HasValue)
                    query = query.Where(uca => uca.AssignmentStartDate >= fromDate);

                if (toDate.HasValue)
                    query = query.Where(uca => uca.AssignmentEndDate <= toDate || uca.AssignmentEndDate == null);

                // Compter le total
                var totalCount = await query.CountAsync();

                // Paginer et projeter
                var assignments = await query
                    .OrderBy(uca => uca.HospitalCenter.Name)
                    .ThenBy(uca => uca.User.LastName)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(uca => new AssignmentDetails
                    {
                        Id = uca.Id,
                        UserId = uca.UserId,
                        UserName = $"{uca.User.FirstName} {uca.User.LastName}",
                        UserEmail = uca.User.Email,
                        HospitalCenterId = uca.HospitalCenterId,
                        HospitalCenterName = uca.HospitalCenter.Name,
                        RoleType = uca.RoleType,
                        IsActive = uca.IsActive,
                        AssignmentStartDate = uca.AssignmentStartDate,
                        AssignmentEndDate = uca.AssignmentEndDate,
                        CreatedAt = uca.CreatedAt
                    })
                    .ToListAsync();

                return (assignments, totalCount);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "SearchAssignmentsFailed",
                    "Erreur lors de la recherche d'affectations",
                    details: new { SearchTerm = searchTerm, RoleType = roleType, HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Génère des statistiques sur les affectations
        /// Pour les tableaux de bord RH
        /// </summary>
        public async Task<AssignmentStatistics> GetAssignmentStatisticsAsync(int? hospitalCenterId = null)
        {
            try
            {
                var query = _context.UserCenterAssignments.AsQueryable();

                if (hospitalCenterId.HasValue)
                    query = query.Where(uca => uca.HospitalCenterId == hospitalCenterId);

                var stats = await query
                    .GroupBy(uca => 1)
                    .Select(g => new AssignmentStatistics
                    {
                        TotalAssignments = g.Count(),
                        ActiveAssignments = g.Count(uca => uca.IsActive),
                        InactiveAssignments = g.Count(uca => !uca.IsActive),
                        SuperAdminAssignments = g.Count(uca => uca.RoleType == "SuperAdmin"),
                        MedicalStaffAssignments = g.Count(uca => uca.RoleType == "MedicalStaff"),
                        AssignmentsThisMonth = g.Count(uca => uca.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    })
                    .FirstOrDefaultAsync();

                return stats ?? new AssignmentStatistics();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("UserCenterAssignmentRepository", "GetAssignmentStatisticsFailed",
                    "Erreur lors du calcul des statistiques d'affectation",
                    details: new { HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        // ===== MÉTHODES PROTÉGÉES PERSONNALISÉES =====

        /// <summary>
        /// Personnalise la préparation d'insertion pour les affectations
        /// </summary>
        protected override void PrepareEntityForInsert(UserCenterAssignment entity)
        {
            base.PrepareEntityForInsert(entity);

            // S'assurer que les rôles sont dans les valeurs autorisées
            if (!IsValidRole(entity.RoleType))
            {
                throw new ArgumentException($"Rôle invalide: {entity.RoleType}");
            }

            // Date de début par défaut si non spécifiée
            if (entity.AssignmentStartDate == default)
            {
                entity.AssignmentStartDate = TimeZoneHelper.GetCameroonTime();
            }

            _logger.LogDebug("Affectation préparée pour insertion: User {UserId} → Center {CenterId} as {Role}",
                entity.UserId, entity.HospitalCenterId, entity.RoleType);
        }

        /// <summary>
        /// Personnalise la préparation de mise à jour pour les affectations
        /// </summary>
        protected override void PrepareEntityForUpdate(UserCenterAssignment entity)
        {
            base.PrepareEntityForUpdate(entity);

            // Valider le rôle si modifié
            if (!IsValidRole(entity.RoleType))
            {
                throw new ArgumentException($"Rôle invalide: {entity.RoleType}");
            }

            _logger.LogDebug("Affectation préparée pour mise à jour: ID {Id} - User {UserId} → Center {CenterId}",
                entity.Id, entity.UserId, entity.HospitalCenterId);
        }

        /// <summary>
        /// Personnalise les propriétés exclues pour les affectations
        /// </summary>
        protected override List<string> GetDefaultExcludedProperties()
        {
            var excluded = base.GetDefaultExcludedProperties();

            // Protéger les références clés
            excluded.Add(nameof(UserCenterAssignment.UserId));
            excluded.Add(nameof(UserCenterAssignment.HospitalCenterId));

            return excluded;
        }

        // ===== MÉTHODES UTILITAIRES PRIVÉES =====

        /// <summary>
        /// Valide qu'un rôle est dans les valeurs autorisées
        /// </summary>
        private static bool IsValidRole(string roleType)
        {
            return roleType is "SuperAdmin" or "MedicalStaff";
        }
    }

    // ===== CLASSES DE SUPPORT =====

    /// <summary>
    /// Détails enrichis d'une affectation
    /// Pour les interfaces et rapports
    /// </summary>
    public class AssignmentDetails
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime AssignmentStartDate { get; set; }
        public DateTime? AssignmentEndDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Propriétés calculées
        public int DaysActive => AssignmentEndDate.HasValue
            ? (AssignmentEndDate.Value - AssignmentStartDate).Days
            : (DateTime.UtcNow - AssignmentStartDate).Days;
    }

    /// <summary>
    /// Statistiques des affectations
    /// Pour les tableaux de bord RH
    /// </summary>
    public class AssignmentStatistics
    {
        public int TotalAssignments { get; set; }
        public int ActiveAssignments { get; set; }
        public int InactiveAssignments { get; set; }
        public int SuperAdminAssignments { get; set; }
        public int MedicalStaffAssignments { get; set; }
        public int AssignmentsThisMonth { get; set; }

        // Propriétés calculées
        public double ActivePercentage => TotalAssignments > 0
            ? (double)ActiveAssignments / TotalAssignments * 100
            : 0;
    }
}