using AutoMapper;
using DocumentFormat.OpenXml.InkML;
using HManagSys.Data.DBContext;
using HManagSys.Data.Repositories;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.HospitalCenter;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Data.Repositories
{
    /// <summary>
    /// Repository spécialisé pour la gestion des centres hospitaliers
    /// Comme un directeur des opérations qui supervise tous les établissements
    /// du réseau hospitalier
    /// </summary>
    public class HospitalCenterRepository : GenericRepository<HospitalCenter>, IHospitalCenterRepository
    {
        private readonly IApplicationLogger _appLogger;

        public HospitalCenterRepository(
            HospitalManagementContext context,
            ILogger<HospitalCenterRepository> logger,
            IApplicationLogger appLogger)
            : base(context, logger)
        {
            _appLogger = appLogger;
        }

        // ===== RECHERCHES SPÉCIALISÉES =====

        /// <summary>
        /// Récupère un centre avec ses statistiques de base
        /// Comme obtenir un rapport d'activité d'un établissement
        /// </summary>
        public async Task<HospitalCenterWithStats?> GetCenterWithStatsAsync(int centerId)
        {
            try
            {
                var center = await GetByIdAsync(centerId);
                if (center == null)
                    return null;

                // Calculer les statistiques en parallèle pour optimiser
                var tasks = new[]
                {
                    _context.UserCenterAssignments.CountAsync(uca =>
                        uca.HospitalCenterId == centerId && uca.IsActive),
                    _context.StockInventories.CountAsync(si => si.HospitalCenterId == centerId),
                    _context.Sales.CountAsync(s => s.HospitalCenterId == centerId),
                    _context.CareEpisodes.CountAsync(ce => ce.HospitalCenterId == centerId)
                };

                var results = await Task.WhenAll(tasks);

                var centerWithStats = new HospitalCenterWithStats
                {
                    Id = center.Id,
                    Name = center.Name,
                    Address = center.Address,
                    PhoneNumber = center.PhoneNumber,
                    Email = center.Email,
                    IsActive = center.IsActive,
                    CreatedAt = center.CreatedAt,
                    ActiveUsers = results[0],
                    ProductsInStock = results[1],
                    TotalSales = results[2],
                    ActiveCareEpisodes = results[3]
                };

                await _appLogger.LogInfoAsync("HospitalCenterRepository", "CenterStatsRetrieved",
                    $"Statistiques récupérées pour centre {center.Name}",
                    details: new { CenterId = centerId, Stats = centerWithStats });

                return centerWithStats;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("HospitalCenterRepository", "GetCenterWithStatsFailed",
                    $"Erreur lors de la récupération des statistiques pour centre {centerId}",
                    details: new { CenterId = centerId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Recherche des centres avec filtres
        /// Permet de filtrer par statut, région, etc.
        /// </summary>
        public async Task<List<HospitalCenter>> SearchCentersAsync(
            string? searchTerm = null,
            bool? isActive = null,
            string? region = null)
        {
            try
            {
                return (await GetAllAsync(query =>
                {

                    // Recherche textuelle sur nom et adresse
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        var term = searchTerm.ToLower();
                        query = query.Where(hc =>
                            hc.Name.ToLower().Contains(term) ||
                            hc.Address.ToLower().Contains(term));
                    }

                    // Filtrage par statut
                    if (isActive.HasValue)
                    {
                        query = query.Where(hc => hc.IsActive == isActive.Value);
                    }

                    // Filtrage par région (basé sur l'adresse)
                    if (!string.IsNullOrWhiteSpace(region))
                    {
                        query = query.Where(hc => hc.Address.ToLower().Contains(region.ToLower()));
                    }

                    return query.OrderBy(hc => hc.Name);
                })).ToList();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("HospitalCenterRepository", "SearchCentersFailed",
                    "Erreur lors de la recherche de centres",
                    details: new { SearchTerm = searchTerm, IsActive = isActive, Region = region, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère les centres accessibles à un utilisateur
        /// Essential pour les interfaces de sélection de centre
        /// </summary>
        public async Task<List<HospitalCenter>> GetUserAccessibleCentersAsync(int userId)
        {
            try
            {
                // Récupérer les centres via les affectations actives
                var accessibleCenters = await _context.UserCenterAssignments
                    .Include(uca => uca.HospitalCenter)
                    .Where(uca => uca.UserId == userId &&
                                  uca.IsActive &&
                                  uca.HospitalCenter.IsActive)
                    .Select(uca => uca.HospitalCenter)
                    .Distinct()
                    .OrderBy(hc => hc.Name)
                    .ToListAsync();

                await _appLogger.LogInfoAsync("HospitalCenterRepository", "UserAccessibleCentersRetrieved",
                    $"Centres accessibles récupérés pour utilisateur {userId}: {accessibleCenters.Count}",
                    userId, details: new { UserId = userId, CenterCount = accessibleCenters.Count });

                return accessibleCenters;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("HospitalCenterRepository", "GetUserAccessibleCentersFailed",
                    $"Erreur lors de la récupération des centres accessibles pour utilisateur {userId}",
                    details: new { UserId = userId, Error = ex.Message });
                throw;
            }
        }

        // ===== GESTION DES CENTRES =====

        /// <summary>
        /// Active ou désactive un centre avec gestion des dépendances
        /// Processus sécurisé qui vérifie les impacts avant modification
        /// </summary>
        public async Task<(bool Success, string? Warning)> SetCenterActiveStatusAsync(
            int centerId, bool isActive, int modifiedBy)
        {
            try
            {
                var center = await GetByIdAsync(centerId);
                if (center == null)
                    return (false, "Centre introuvable");

                // Vérifier les impacts si on désactive le centre
                if (!isActive && center.IsActive)
                {
                    var impacts = await CheckCenterDeactivationImpactsAsync(centerId);
                    if (impacts.HasBlockingIssues)
                    {
                        return (false, impacts.BlockingMessage);
                    }
                }

                var oldStatus = center.IsActive;
                center.IsActive = isActive;
                center.ModifiedBy = modifiedBy;
                center.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _context.SaveChangesAsync();

                await _appLogger.LogInfoAsync("HospitalCenterRepository", "CenterStatusChanged",
                    $"Statut du centre {center.Name} changé: {oldStatus} → {isActive}",
                    modifiedBy, centerId, details: new
                    {
                        CenterId = centerId,
                        CenterName = center.Name,
                        OldStatus = oldStatus,
                        NewStatus = isActive
                    });

                var warning = await CheckCenterDeactivationImpactsAsync(centerId);
                return (true, warning.WarningMessage);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("HospitalCenterRepository", "SetCenterActiveStatusFailed",
                    $"Erreur lors du changement de statut pour centre {centerId}",
                    details: new { CenterId = centerId, IsActive = isActive, ModifiedBy = modifiedBy, Error = ex.Message });
                throw;
            }
        }

        // ===== RAPPORTS ET STATISTIQUES =====

        /// <summary>
        /// Génère un rapport d'activité pour un centre
        /// Vue d'ensemble des opérations sur une période
        /// </summary>
        public async Task<HManagSys.Models.ViewModels.HospitalCenter.CenterActivityReport> GenerateActivityReportAsync(
            int centerId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var center = await GetByIdAsync(centerId);
                if (center == null)
                    throw new ArgumentException($"Centre {centerId} introuvable");

                // Convertir les dates en fuseau camerounais pour cohérence
                var fromDateCameroon = TimeZoneHelper.ConvertToCameroonTime(fromDate);
                var toDateCameroon = TimeZoneHelper.ConvertToCameroonTime(toDate);

                // Ventes
                var   results1 = await _context.Sales
                     .Where(s => s.HospitalCenterId == centerId &&
                                 s.SaleDate >= fromDateCameroon &&
                                 s.SaleDate <= toDateCameroon)
                     .SumAsync(s => s.FinalAmount);

                // Épisodes de soins
                var results2 = await _context.CareEpisodes
                     .Where(ce => ce.HospitalCenterId == centerId &&
                                  ce.EpisodeStartDate >= fromDateCameroon &&
                                  ce.EpisodeStartDate <= toDateCameroon)
                     .CountAsync();

                // Examens
                var results3 = await _context.Examinations
                     .Where(e => e.HospitalCenterId == centerId &&
                                 e.RequestDate >= fromDateCameroon &&
                                 e.RequestDate <= toDateCameroon)
                     .CountAsync();

                // Patients uniques
                var results4 = await _context.CareEpisodes
                      .Where(ce => ce.HospitalCenterId == centerId &&
                                   ce.EpisodeStartDate >= fromDateCameroon &&
                                   ce.EpisodeStartDate <= toDateCameroon)
                      .Select(ce => ce.PatientId)
                      .Distinct()
                      .CountAsync();
                //var results = await Task.WhenAll(metricsTask.Select(t => t.ConfigureAwait(false)));

                var report = new HManagSys.Models.ViewModels.HospitalCenter.CenterActivityReport
                {
                    HospitalCenterId = centerId,
                    CenterName = center.Name,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalRevenue = (decimal)results1,
                    CareEpisodesCreated = (int)results2,
                    ExaminationsPerformed = (int)results3,
                    UniquePatients = (int)results4,
                    ReportGeneratedAt = TimeZoneHelper.GetCameroonTime()
                };

                await _appLogger.LogInfoAsync("HospitalCenterRepository", "ActivityReportGenerated",
                    $"Rapport d'activité généré pour centre {center.Name}",
                    details: new
                    {
                        CenterId = centerId,
                        FromDate = fromDate,
                        ToDate = toDate,
                        Revenue = report.TotalRevenue
                    });

                return report;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("HospitalCenterRepository", "GenerateActivityReportFailed",
                    $"Erreur lors de la génération du rapport pour centre {centerId}",
                    details: new { CenterId = centerId, FromDate = fromDate, ToDate = toDate, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère les statistiques globales de tous les centres
        /// Pour les tableaux de bord de direction
        /// </summary>
        public async Task<NetworkStatistics> GetNetworkStatisticsAsync()
        {
            try
            {
                var today = TimeZoneHelper.GetCameroonTime().Date;

                var stats = await _context.HospitalCenters
                    .Where(hc => hc.IsActive)
                    .GroupBy(hc => 1)
                    .Select(g => new NetworkStatistics
                    {
                        TotalCenters = g.Count(),
                        ActiveCenters = g.Count(hc => hc.IsActive),
                        TotalUsersNetwork = _context.UserCenterAssignments
                            .Where(uca => uca.IsActive)
                            .Select(uca => uca.UserId)
                            .Distinct()
                            .Count(),
                        TotalSalesToday = _context.Sales
                            .Where(s => s.SaleDate >= today)
                            .Sum(s => s.FinalAmount),
                        ActiveCareEpisodesNetwork = _context.CareEpisodes
                            .Count(ce => ce.Status == "Active")
                    })
                    .FirstOrDefaultAsync();

                return stats ?? new NetworkStatistics();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("HospitalCenterRepository", "GetNetworkStatisticsFailed",
                    "Erreur lors du calcul des statistiques réseau",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        // ===== MÉTHODES PROTÉGÉES PERSONNALISÉES =====

        /// <summary>
        /// Personnalise la préparation d'insertion pour les centres
        /// </summary>
        protected override void PrepareEntityForInsert(HospitalCenter entity)
        {
            base.PrepareEntityForInsert(entity);

            // Normaliser le nom et l'email
            entity.Name = entity.Name.Trim();
            if (!string.IsNullOrEmpty(entity.Email))
            {
                entity.Email = entity.Email.ToLowerInvariant();
            }

            _logger.LogDebug("Centre hospitalier préparé pour insertion: {Name}", entity.Name);
        }

        /// <summary>
        /// Personnalise la préparation de mise à jour pour les centres
        /// </summary>
        protected override void PrepareEntityForUpdate(HospitalCenter entity)
        {
            base.PrepareEntityForUpdate(entity);

            // Normaliser le nom et l'email
            entity.Name = entity.Name.Trim();
            if (!string.IsNullOrEmpty(entity.Email))
            {
                entity.Email = entity.Email.ToLowerInvariant();
            }

            _logger.LogDebug("Centre hospitalier préparé pour mise à jour: {CenterId} - {Name}",
                entity.Id, entity.Name);
        }

        // ===== MÉTHODES UTILITAIRES PRIVÉES =====

        /// <summary>
        /// Vérifie les impacts de la désactivation d'un centre
        /// Analyse les dépendances et les conflits potentiels
        /// </summary>
        private async Task<CenterDeactivationImpact> CheckCenterDeactivationImpactsAsync(int centerId)
        {
            try
            {
                var impact = new CenterDeactivationImpact();

                // Vérifier les utilisateurs actifs
                var activeUsers = await _context.UserCenterAssignments
                    .CountAsync(uca => uca.HospitalCenterId == centerId && uca.IsActive);

                if (activeUsers > 0)
                {
                    impact.WarningMessages.Add($"{activeUsers} utilisateur(s) actif(s) dans ce centre");
                }

                // Vérifier les épisodes de soins actifs
                var activeCareEpisodes = await _context.CareEpisodes
                    .CountAsync(ce => ce.HospitalCenterId == centerId && ce.Status == "Active");

                if (activeCareEpisodes > 0)
                {
                    impact.BlockingIssues.Add($"{activeCareEpisodes} épisode(s) de soin actif(s)");
                    impact.HasBlockingIssues = true;
                }

                // Vérifier les sessions actives
                var activeSessions = await _context.UserSessions
                    .CountAsync(us => us.CurrentHospitalCenterId == centerId && us.IsActive);

                if (activeSessions > 0)
                {
                    impact.WarningMessages.Add($"{activeSessions} session(s) utilisateur active(s)");
                }

                return impact;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des impacts pour centre {CenterId}", centerId);
                throw;
            }
        }
    }

}