using ClosedXML.Excel;
using HManagSys.Data.DBContext;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Reports;
using HManagSys.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;
using System.Text.Json;
using StockValuationReportViewModel = HManagSys.Models.ViewModels.Reports.StockValuationReportViewModel;

namespace HManagSys.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly HospitalManagementContext _context;
        private readonly IApplicationLogger _logger;
        private readonly IAuditService _auditService;
        private readonly IMemoryCache _cache;
        private readonly IHostEnvironment _environment;

        public ReportService(
            HospitalManagementContext context,
            IApplicationLogger logger,
            IAuditService auditService,
            IMemoryCache cache,
            IHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
            _cache = cache;
            _environment = environment;
        }

        #region Rapports Utilisateurs et Centres

        /// <summary>
        /// Génère un rapport des utilisateurs par centre
        /// </summary>
        public async Task<UserCenterReportViewModel> GenerateUserCenterReportAsync(UserCenterReportFilters filters)
        {
            try
            {
                // Vérifier si le rapport est en cache
                string cacheKey = $"UserCenterReport_{JsonSerializer.Serialize(filters)}";
                if (_cache.TryGetValue(cacheKey, out UserCenterReportViewModel cachedReport))
                {
                    return cachedReport;
                }

                // Construire la requête de base
                var query = _context.RptUserCenterDetails.AsQueryable();

                // Appliquer les filtres
                if (filters.HospitalCenterId.HasValue)
                {
                    query = query.Where(r => r.HospitalCenterId == filters.HospitalCenterId);
                }

                if (!string.IsNullOrWhiteSpace(filters.RoleType))
                {
                    query = query.Where(r => r.RoleType == filters.RoleType);
                }

                if (filters.IsAssignmentActive.HasValue)
                {
                    query = query.Where(r => r.AssignmentIsActive == filters.IsAssignmentActive);
                }

                if (filters.IsUserActive.HasValue)
                {
                    query = query.Where(r => r.UserIsActive == filters.IsUserActive);
                }

                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    query = query.Where(r =>
                        r.FirstName.ToLower().Contains(searchTerm) ||
                        r.LastName.ToLower().Contains(searchTerm) ||
                        r.Email.ToLower().Contains(searchTerm) ||
                        (r.HospitalCenterName != null && r.HospitalCenterName.ToLower().Contains(searchTerm)));
                }

                // Appliquer la période
                if (filters.FromDate.HasValue)
                {
                    query = query.Where(r =>
                        r.AssignmentStartDate == null ||
                        r.AssignmentStartDate >= filters.FromDate);
                }

                if (filters.ToDate.HasValue)
                {
                    var toDateEnd = filters.ToDate.Value.Date.AddDays(1).AddSeconds(-1);
                    query = query.Where(r =>
                        r.AssignmentStartDate == null ||
                        r.AssignmentStartDate <= toDateEnd);
                }

                // Récupérer les données
                var reportItems = await query.Select(r => new UserCenterReportItem
                {
                    UserId = r.UserId,
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    Email = r.Email,
                    PhoneNumber = r.PhoneNumber,
                    UserIsActive = r.UserIsActive,
                    LastLoginDate = r.LastLoginDate,
                    AssignmentId = r.AssignmentId,
                    RoleType = r.RoleType,
                    AssignmentIsActive = r.AssignmentIsActive ?? false,
                    HospitalCenterName = r.HospitalCenterName,
                    AssignmentStartDate = r.AssignmentStartDate,
                    AssignmentEndDate = r.AssignmentEndDate
                }).ToListAsync();

                // Calculer les statistiques
                var report = new UserCenterReportViewModel
                {
                    ReportTitle = "Rapport des Utilisateurs et Centres",
                    ReportDescription = "Affectations des utilisateurs aux centres hospitaliers",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Items = reportItems,
                    Filters = filters,
                    TotalCount = reportItems.Count,
                };

                // Calculer les statistiques supplémentaires
                report.TotalUsers = reportItems.Select(r => r.UserId).Distinct().Count();
                report.ActiveUsers = reportItems.Where(r => r.UserIsActive).Select(r => r.UserId).Distinct().Count();
                report.TotalAssignments = reportItems.Count(r => r.AssignmentId.HasValue);
                report.ActiveAssignments = reportItems.Count(r => r.AssignmentIsActive);

                // Distribution par rôle
                report.UsersByRole = reportItems
                    .Where(r => !string.IsNullOrEmpty(r.RoleType))
                    .GroupBy(r => r.RoleType)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Distribution par centre
                report.UsersByCenter = reportItems
                    .Where(r => !string.IsNullOrEmpty(r.HospitalCenterName))
                    .GroupBy(r => r.HospitalCenterName)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Utilisateur",
                    "Email",
                    "Statut",
                    "Centre",
                    "Rôle",
                    "Statut Affectation",
                    "Date Début",
                    "Date Fin"
                };

                // Mettre en cache le rapport (15 minutes)
                _cache.Set(cacheKey, report, TimeSpan.FromMinutes(15));

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GenerateUserCenterReportError",
                    "Erreur lors de la génération du rapport utilisateurs-centres",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        /// <summary>
        /// Génère un rapport des sessions actives
        /// </summary>
        public async Task<ActiveSessionsReportViewModel> GenerateActiveSessionsReportAsync(ActiveSessionsReportFilters filters)
        {
            try
            {
                // Vérifier si le rapport est en cache
                string cacheKey = $"ActiveSessionsReport_{JsonSerializer.Serialize(filters)}";
                if (_cache.TryGetValue(cacheKey, out ActiveSessionsReportViewModel cachedReport))
                {
                    return cachedReport;
                }

                // Construire la requête de base
                var query = _context.RptActiveSessions.AsQueryable();

                // Appliquer les filtres
                if (filters.HospitalCenterId.HasValue)
                {
                    query = query.Where(r => r.CurrentHospitalCenter.Contains(
                        _context.HospitalCenters
                            .Where(h => h.Id == filters.HospitalCenterId)
                            .Select(h => h.Name)
                            .FirstOrDefault() ?? ""));
                }

                if (filters.MinHoursConnected.HasValue)
                {
                    query = query.Where(r => r.HoursConnected >= filters.MinHoursConnected);
                }

                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    query = query.Where(r =>
                        r.UserName.ToLower().Contains(searchTerm) ||
                        r.Email.ToLower().Contains(searchTerm) ||
                        r.CurrentHospitalCenter.ToLower().Contains(searchTerm));
                }

                // Récupérer les données
                var reportItems = await query.Select(r => new ActiveSessionReportItem
                {
                    SessionId = r.SessionId,
                    UserId = r.UserId,
                    UserName = r.UserName,
                    Email = r.Email,
                    CurrentHospitalCenter = r.CurrentHospitalCenter,
                    LoginTime = r.LoginTime,
                    IpAddress = r.IpAddress ?? "",
                    HoursConnected = r.HoursConnected
                }).ToListAsync();

                // Créer le rapport
                var report = new ActiveSessionsReportViewModel
                {
                    ReportTitle = "Rapport des Sessions Actives",
                    ReportDescription = "Sessions utilisateurs actuellement connectées",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Items = reportItems,
                    Filters = filters,
                    TotalCount = reportItems.Count,
                };

                // Calculer les statistiques
                report.TotalActiveSessions = reportItems.Count;
                report.AverageSessionDuration = reportItems.Any() ?
                    reportItems.Sum(r => r.HoursConnected * 60) / reportItems.Count : 0;

                // Distribution par centre
                report.SessionsByCenter = reportItems
                    .GroupBy(r => r.CurrentHospitalCenter)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Utilisateur",
                    "Email",
                    "Centre",
                    "Connexion",
                    "IP",
                    "Durée"
                };

                // Mettre en cache le rapport (5 minutes)
                _cache.Set(cacheKey, report, TimeSpan.FromMinutes(5));

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GenerateActiveSessionsReportError",
                    "Erreur lors de la génération du rapport des sessions actives",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        #endregion

        #region Rapports Stock et Inventaire

        /// <summary>
        /// Génère un rapport sur l'état des stocks
        /// </summary>
        public async Task<StockStatusReportViewModel> GenerateStockStatusReportAsync(StockStatusReportFilters filters)
        {
            try
            {
                // Vérifier si le rapport est en cache
                string cacheKey = $"StockStatusReport_{JsonSerializer.Serialize(filters)}";
                if (_cache.TryGetValue(cacheKey, out StockStatusReportViewModel cachedReport))
                {
                    return cachedReport;
                }

                // Construire la requête de base
                var query = _context.RptStockStatuses.AsQueryable();

                // Récupérer les prix de vente des produits
                var productPrices = await _context.Products
                    .ToDictionaryAsync(p => p.Id, p => p.SellingPrice);

                // Appliquer les filtres
                if (filters.HospitalCenterId.HasValue)
                {
                    query = query.Where(r => r.HospitalCenterId == filters.HospitalCenterId);
                }

                if (filters.ProductCategoryId.HasValue)
                {
                    query = query.Where(r =>
                        _context.Products
                            .Where(p => p.ProductCategoryId == filters.ProductCategoryId)
                            .Select(p => p.Id)
                            .Contains(r.ProductId));
                }

                if (!string.IsNullOrWhiteSpace(filters.StockStatus))
                {
                    query = query.Where(r => r.StockStatus == filters.StockStatus);
                }

                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    query = query.Where(r =>
                        r.ProductName.ToLower().Contains(searchTerm) ||
                        r.ProductCategory.ToLower().Contains(searchTerm) ||
                        r.HospitalCenterName.ToLower().Contains(searchTerm));
                }

                // Récupérer les données
                var reportItems = await query.ToListAsync();

                // Convertir en ViewModel
                var items = reportItems.Select(r => new StockStatusReportItem
                {
                    ProductId = r.ProductId,
                    ProductName = r.ProductName,
                    ProductCategory = r.ProductCategory,
                    HospitalCenterId = r.HospitalCenterId,
                    HospitalCenterName = r.HospitalCenterName,
                    CurrentQuantity = r.CurrentQuantity,
                    MinimumThreshold = r.MinimumThreshold,
                    MaximumThreshold = r.MaximumThreshold,
                    StockStatus = r.StockStatus,
                    LastMovementDate = r.LastMovementDate,
                    UnitPrice = productPrices.TryGetValue(r.ProductId, out var price) ? price : 0,
                    TotalValue = productPrices.TryGetValue(r.ProductId, out var itemPrice) ? itemPrice * r.CurrentQuantity : 0
                }).ToList();

                // Créer le rapport
                var report = new StockStatusReportViewModel
                {
                    ReportTitle = "Rapport d'État des Stocks",
                    ReportDescription = "État actuel des stocks par produit et centre",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Items = items,
                    Filters = filters,
                    TotalCount = items.Count,
                };

                // Calculer les statistiques
                report.TotalProducts = items.Count;
                report.ProductsWithCriticalStock = items.Count(i => i.StockStatus == "Critical");
                report.ProductsWithLowStock = items.Count(i => i.StockStatus == "Low");
                report.ProductsWithNormalStock = items.Count(i => i.StockStatus == "Normal");
                report.ProductsWithOverstock = items.Count(i => i.StockStatus == "Overstock");
                report.TotalStockValue = items.Sum(i => i.TotalValue);

                // Distribution par catégorie
                report.ProductsByCategory = items
                    .GroupBy(i => i.ProductCategory)
                    .ToDictionary(g => g.Key, g => g.Count());

                report.ValueByCategory = items
                    .GroupBy(i => i.ProductCategory)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalValue));

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Produit",
                    "Catégorie",
                    "Centre",
                    "Quantité",
                    "Seuil Min",
                    "Seuil Max",
                    "Statut",
                    "Dernier Mouvement",
                    "Valeur"
                };

                // Mettre en cache le rapport (15 minutes)
                _cache.Set(cacheKey, report, TimeSpan.FromMinutes(15));

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GenerateStockStatusReportError",
                    "Erreur lors de la génération du rapport d'état des stocks",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        /// <summary>
        /// Génère un rapport des mouvements de stock
        /// </summary>
        public async Task<StockMovementReportViewModel> GenerateStockMovementReportAsync(StockMovementReportFilters filters)
        {
            try
            {
                // Construire la requête de base
                var query = _context.StockMovements
                    .Include(sm => sm.Product)
                    .Include(sm => sm.HospitalCenter)
                    .AsQueryable();

                // Appliquer les filtres
                if (filters.HospitalCenterId.HasValue)
                {
                    query = query.Where(sm => sm.HospitalCenterId == filters.HospitalCenterId);
                }

                if (filters.ProductId.HasValue)
                {
                    query = query.Where(sm => sm.ProductId == filters.ProductId);
                }

                if (!string.IsNullOrWhiteSpace(filters.MovementType))
                {
                    query = query.Where(sm => sm.MovementType == filters.MovementType);
                }

                if (!string.IsNullOrWhiteSpace(filters.ReferenceType))
                {
                    query = query.Where(sm => sm.ReferenceType == filters.ReferenceType);
                }

                if (filters.FromDate.HasValue)
                {
                    query = query.Where(sm => sm.MovementDate >= filters.FromDate);
                }

                if (filters.ToDate.HasValue)
                {
                    var toDateEnd = filters.ToDate.Value.Date.AddDays(1).AddSeconds(-1);
                    query = query.Where(sm => sm.MovementDate <= toDateEnd);
                }

                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    query = query.Where(sm =>
                        sm.Product.Name.ToLower().Contains(searchTerm) ||
                        sm.HospitalCenter.Name.ToLower().Contains(searchTerm) ||
                        (sm.Notes != null && sm.Notes.ToLower().Contains(searchTerm)));
                }

                // Récupérer les créateurs pour les noms
                var userIds = await query.Select(sm => sm.CreatedBy).Distinct().ToListAsync();
                var users = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
                    .ToDictionaryAsync(u => u.Id, u => u.Name);

                // Récupérer les mouvements avec pagination
                var movements = await query
                    .OrderByDescending(sm => sm.MovementDate)
                    .Select(sm => new
                    {
                        sm.Id,
                        sm.ProductId,
                        ProductName = sm.Product.Name,
                        sm.HospitalCenterId,
                        HospitalCenterName = sm.HospitalCenter.Name,
                        sm.MovementType,
                        sm.Quantity,
                        sm.ReferenceType,
                        sm.ReferenceId,
                        sm.Notes,
                        sm.MovementDate,
                        sm.CreatedBy
                    })
                    .ToListAsync();

                // Convertir en ViewModel
                var items = movements.Select(m => new StockMovementReportItem
                {
                    MovementId = m.Id,
                    ProductName = m.ProductName,
                    HospitalCenterName = m.HospitalCenterName,
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    ReferenceType = m.ReferenceType,
                    ReferenceId = m.ReferenceId,
                    Notes = m.Notes,
                    MovementDate = m.MovementDate,
                    CreatedByName = users.TryGetValue(m.CreatedBy, out var name) ? name : "Système"
                }).ToList();

                // Créer le rapport
                var report = new StockMovementReportViewModel
                {
                    ReportTitle = "Rapport des Mouvements de Stock",
                    ReportDescription = "Historique des mouvements de stock par produit et centre",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Items = items,
                    Filters = filters,
                    TotalCount = items.Count
                };

                // Calculer les statistiques
                report.MovementsByType = items
                    .GroupBy(i => i.MovementType)
                    .ToDictionary(g => g.Key, g => g.Count());

                report.QuantityByType = items
                    .GroupBy(i => i.MovementType)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

                report.TotalMovements = items.Count;
                report.TotalInQuantity = items
                    .Where(i => IsPositiveMovement(i.MovementType))
                    .Sum(i => i.Quantity);
                report.TotalOutQuantity = items
                    .Where(i => !IsPositiveMovement(i.MovementType))
                    .Sum(i => i.Quantity);
                report.NetChange = report.TotalInQuantity - report.TotalOutQuantity;

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Date",
                    "Produit",
                    "Centre",
                    "Type",
                    "Quantité",
                    "Référence",
                    "Notes",
                    "Utilisateur"
                };

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GenerateStockMovementReportError",
                    "Erreur lors de la génération du rapport des mouvements de stock",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        /// <summary>
        /// Génère un rapport de valorisation du stock
        /// </summary>
        public async Task<StockValuationReportViewModel> GenerateStockValuationReportAsync(StockValuationReportFilters filters)
        {
            try
            {
                // Construire la requête de base pour les stocks
                var query = _context.StockInventories
                    .Include(si => si.Product)
                        .ThenInclude(p => p.ProductCategory)
                    .Include(si => si.HospitalCenter)
                    .AsQueryable();

                // Appliquer les filtres
                if (filters.HospitalCenterId.HasValue)
                {
                    query = query.Where(si => si.HospitalCenterId == filters.HospitalCenterId);
                }

                if (filters.ProductCategoryId.HasValue)
                {
                    query = query.Where(si => si.Product.ProductCategoryId == filters.ProductCategoryId);
                }

                // Récupérer les données
                var inventoryItems = await query.ToListAsync();

                // Valoriser selon la méthode choisie
                var items = new List<StockValuationReportItem>();
                foreach (var item in inventoryItems)
                {
                    if (item.CurrentQuantity <= 0)
                        continue;

                    decimal unitPrice = 0;
                    switch (filters.ValuationType)
                    {
                        case "SellingPrice":
                            unitPrice = item.Product.SellingPrice;
                            break;
                        // Les autres types de valorisation ne sont pas implémentés dans ce prototype
                        default:
                            unitPrice = item.Product.SellingPrice;
                            break;
                    }

                    items.Add(new StockValuationReportItem
                    {
                        ProductName = item.Product.Name,
                        CategoryName = item.Product.ProductCategory.Name,
                        HospitalCenterName = item.HospitalCenter.Name,
                        CurrentQuantity = item.CurrentQuantity,
                        UnitOfMeasure = item.Product.UnitOfMeasure,
                        UnitPrice = unitPrice,
                        TotalValue = item.CurrentQuantity * unitPrice
                    });
                }

                // Créer le rapport
                var report = new StockValuationReportViewModel
                {
                    ReportTitle = "Rapport de Valorisation des Stocks",
                    ReportDescription = "Valeur actuelle des stocks par produit et centre",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Items = items,
                    Filters = filters,
                    TotalCount = items.Count,
                    TotalValue = items.Sum(i => i.TotalValue)
                };

                // Valeur par catégorie
                report.ValueByCategory = items
                    .GroupBy(i => i.CategoryName)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalValue));

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Produit",
                    "Catégorie",
                    "Centre",
                    "Quantité",
                    "Prix unitaire",
                    "Valeur totale"
                };

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GenerateStockValuationReportError",
                    "Erreur lors de la génération du rapport de valorisation des stocks",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        #endregion

        #region Rapports Financiers

        /// <summary>
        /// Génère un rapport d'activité financière
        /// </summary>
        public async Task<FinancialActivityReportViewModel> GenerateFinancialActivityReportAsync(FinancialActivityReportFilters filters)
        {
            try
            {
                // Vérifier si le rapport est en cache
                string cacheKey = $"FinancialActivityReport_{JsonSerializer.Serialize(filters)}";
                if (_cache.TryGetValue(cacheKey, out FinancialActivityReportViewModel cachedReport))
                {
                    return cachedReport;
                }

                // Construire la requête de base
                var query = _context.RptFinancialActivities.AsQueryable();

                // Appliquer les filtres
                if (filters.HospitalCenterId.HasValue)
                {
                    query = query.Where(r => r.HospitalCenterId == filters.HospitalCenterId);
                }

                if (filters.FromDate.HasValue)
                {
                    query = query.Where(r => r.ReportDate >= DateOnly.FromDateTime(filters.FromDate.Value));
                }

                if (filters.ToDate.HasValue)
                {
                    query = query.Where(r => r.ReportDate <= DateOnly.FromDateTime(filters.ToDate.Value));
                }

                // Récupérer les données
                var financialData = await query.ToListAsync();

                // Regrouper selon le paramètre choisi
                var groupedData = new List<FinancialActivityReportItem>();

                foreach (var data in financialData)
                {
                    DateOnly reportDate = data.ReportDate;

                    // Trouver le groupe existant ou créer un nouveau
                    var existingGroup = groupedData.FirstOrDefault(g => DateMatches(g.ReportDate.Date, reportDate, filters.GroupBy));

                    if (existingGroup != null)
                    {
                        // Ajouter au groupe existant
                        existingGroup.TotalSales += data.TotalSales;
                        existingGroup.TotalCareRevenue += data.TotalCareRevenue;
                        existingGroup.TotalExaminationRevenue += data.TotalExaminationRevenue;
                        existingGroup.TotalRevenue += data.TotalRevenue;
                        existingGroup.TotalCashPayments += data.TotalCashPayments;
                        existingGroup.TotalMobilePayments += data.TotalMobilePayments;
                        existingGroup.TransactionCount += data.TransactionCount;
                        existingGroup.PatientCount += data.PatientCount;
                    }
                    else
                    {
                        // Créer un nouveau groupe
                        DateTime groupDate = GetGroupDate(reportDate, filters.GroupBy);

                        groupedData.Add(new FinancialActivityReportItem
                        {
                            ReportDate = groupDate,
                            HospitalCenterName = data.HospitalCenterName,
                            TotalSales = data.TotalSales,
                            TotalCareRevenue = data.TotalCareRevenue,
                            TotalExaminationRevenue = data.TotalExaminationRevenue,
                            TotalRevenue = data.TotalRevenue,
                            TotalCashPayments = data.TotalCashPayments,
                            TotalMobilePayments = data.TotalMobilePayments,
                            TransactionCount = data.TransactionCount,
                            PatientCount = data.PatientCount
                        });
                    }
                }

                // Trier par date
                groupedData = groupedData.OrderBy(g => g.ReportDate).ToList();

                // Créer le rapport
                var report = new FinancialActivityReportViewModel
                {
                    ReportTitle = "Rapport d'Activité Financière",
                    ReportDescription = $"Activité financière par {GetGroupByText(filters.GroupBy)}",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Items = groupedData,
                    Filters = filters,
                    TotalCount = groupedData.Count,
                };

                // Calculer les totaux
                report.TotalSales = groupedData.Sum(g => g.TotalSales);
                report.TotalCareRevenue = groupedData.Sum(g => g.TotalCareRevenue);
                report.TotalExaminationRevenue = groupedData.Sum(g => g.TotalExaminationRevenue);
                report.TotalRevenue = groupedData.Sum(g => g.TotalRevenue);
                report.TotalCashPayments = groupedData.Sum(g => g.TotalCashPayments);
                report.TotalMobilePayments = groupedData.Sum(g => g.TotalMobilePayments);
                report.TotalTransactionCount = groupedData.Sum(g => g.TransactionCount);
                report.TotalPatientCount = groupedData.Sum(g => g.PatientCount);

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Date",
                    "Centre",
                    "Ventes",
                    "Revenus Soins",
                    "Revenus Examens",
                    "Total Revenus",
                    "Paiements Espèces",
                    "Paiements Mobile",
                    "Transactions",
                    "Patients"
                };

                // Mettre en cache le rapport (15 minutes)
                _cache.Set(cacheKey, report, TimeSpan.FromMinutes(15));

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GenerateFinancialActivityReportError",
                    "Erreur lors de la génération du rapport d'activité financière",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        /// <summary>
        /// Génère un rapport des paiements
        /// </summary>
        public async Task<PaymentReportViewModel> GeneratePaymentReportAsync(PaymentReportFilters filters)
        {
            try
            {
                // Construire la requête de base
                var query = _context.Payments
                    .Include(p => p.Patient)
                    .Include(p => p.HospitalCenter)
                    .Include(p => p.PaymentMethod)
                    .Include(p => p.ReceivedByNavigation)
                    .AsQueryable();

                // Appliquer les filtres
                if (filters.HospitalCenterId.HasValue)
                {
                    query = query.Where(p => p.HospitalCenterId == filters.HospitalCenterId);
                }

                if (filters.PaymentMethodId.HasValue)
                {
                    query = query.Where(p => p.PaymentMethodId == filters.PaymentMethodId);
                }

                if (!string.IsNullOrWhiteSpace(filters.ReferenceType))
                {
                    query = query.Where(p => p.ReferenceType == filters.ReferenceType);
                }

                if (filters.PatientId.HasValue)
                {
                    query = query.Where(p => p.PatientId == filters.PatientId);
                }

                if (filters.ReceivedBy.HasValue)
                {
                    query = query.Where(p => p.ReceivedBy == filters.ReceivedBy);
                }

                if (filters.MinAmount.HasValue)
                {
                    query = query.Where(p => p.Amount >= filters.MinAmount);
                }

                if (filters.MaxAmount.HasValue)
                {
                    query = query.Where(p => p.Amount <= filters.MaxAmount);
                }

                if (filters.FromDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate >= filters.FromDate);
                }

                if (filters.ToDate.HasValue)
                {
                    var toDateEnd = filters.ToDate.Value.Date.AddDays(1).AddSeconds(-1);
                    query = query.Where(p => p.PaymentDate <= toDateEnd);
                }

                // Récupérer les données
                var payments = await query
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new PaymentReportItem
                    {
                        PaymentId = p.Id,
                        ReferenceType = p.ReferenceType,
                        ReferenceId = p.ReferenceId,
                        PatientName = p.Patient != null ? p.Patient.FirstName + " " + p.Patient.LastName : "Non assigné",
                        HospitalCenterName = p.HospitalCenter.Name,
                        PaymentMethodName = p.PaymentMethod.Name,
                        Amount = p.Amount,
                        PaymentDate = p.PaymentDate,
                        ReceivedByName = p.ReceivedByNavigation.FirstName + " " + p.ReceivedByNavigation.LastName,
                        TransactionReference = p.TransactionReference
                    })
                    .ToListAsync();

                // Créer le rapport
                var report = new PaymentReportViewModel
                {
                    ReportTitle = "Rapport des Paiements",
                    ReportDescription = "Détails des paiements reçus",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Items = payments,
                    Filters = filters,
                    TotalCount = payments.Count,
                    TotalPayments = payments.Sum(p => p.Amount)
                };

                // Paiements par méthode
                report.PaymentsByMethod = payments
                    .GroupBy(p => p.PaymentMethodName)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

                // Paiements par type de référence
                report.PaymentsByReferenceType = payments
                    .GroupBy(p => p.ReferenceType)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Date",
                    "Patient",
                    "Centre",
                    "Méthode",
                    "Référence",
                    "Montant",
                    "Reçu par",
                    "Transaction"
                };

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GeneratePaymentReportError",
                    "Erreur lors de la génération du rapport des paiements",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        /// <summary>
        /// Génère un rapport des ventes
        /// </summary>
        public async Task<SalesReportViewModel> GenerateSalesReportAsync(SalesReportFilters filters)
        {
            try
            {
                // Construire la requête de base
                var query = _context.Sales
                    .Include(s => s.Patient)
                    .Include(s => s.HospitalCenter)
                    .Include(s => s.SoldByNavigation)
                    .Include(s => s.SaleItems)
                        .ThenInclude(si => si.Product)
                    .AsQueryable();

                // Appliquer les filtres
                if (filters.HospitalCenterId.HasValue)
                {
                    query = query.Where(s => s.HospitalCenterId == filters.HospitalCenterId);
                }

                if (!string.IsNullOrWhiteSpace(filters.PaymentStatus))
                {
                    query = query.Where(s => s.PaymentStatus == filters.PaymentStatus);
                }

                if (filters.PatientId.HasValue)
                {
                    query = query.Where(s => s.PatientId == filters.PatientId);
                }

                if (filters.SoldBy.HasValue)
                {
                    query = query.Where(s => s.SoldBy == filters.SoldBy);
                }

                if (filters.ProductId.HasValue)
                {
                    query = query.Where(s => s.SaleItems.Any(si => si.ProductId == filters.ProductId));
                }

                if (filters.FromDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate >= filters.FromDate);
                }

                if (filters.ToDate.HasValue)
                {
                    var toDateEnd = filters.ToDate.Value.Date.AddDays(1).AddSeconds(-1);
                    query = query.Where(s => s.SaleDate <= toDateEnd);
                }

                // Récupérer les données
                var sales = await query
                    .OrderByDescending(s => s.SaleDate)
                    .ToListAsync();

                // Convertir en ViewModel
                var saleItems = new List<SaleReportItem>();
                foreach (var sale in sales)
                {
                    var saleItem = new SaleReportItem
                    {
                        SaleId = sale.Id,
                        SaleNumber = sale.SaleNumber,
                        PatientName = sale.Patient != null ? $"{sale.Patient.FirstName} {sale.Patient.LastName}" : "Non assigné",
                        HospitalCenterName = sale.HospitalCenter.Name,
                        SoldByName = $"{sale.SoldByNavigation.FirstName} {sale.SoldByNavigation.LastName}",
                        SaleDate = sale.SaleDate,
                        TotalAmount = sale.TotalAmount,
                        DiscountAmount = sale.DiscountAmount,
                        FinalAmount = sale.FinalAmount,
                        PaymentStatus = sale.PaymentStatus,
                        Items = sale.SaleItems.Select(si => new SaleItemDetail
                        {
                            ProductName = si.Product.Name,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            TotalPrice = si.TotalPrice
                        }).ToList()
                    };
                    saleItems.Add(saleItem);
                }

                // Créer le rapport
                var report = new SalesReportViewModel
                {
                    ReportTitle = "Rapport des Ventes",
                    ReportDescription = "Détails des ventes effectuées",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Items = saleItems,
                    Filters = filters,
                    TotalCount = saleItems.Count
                };

                // Calculer les totaux
                report.TotalSalesAmount = saleItems.Sum(s => s.TotalAmount);
                report.TotalDiscountAmount = saleItems.Sum(s => s.DiscountAmount);
                report.TotalFinalAmount = saleItems.Sum(s => s.FinalAmount);
                report.TotalProductsSold = saleItems.Sum(s => s.Items.Count);

                // Ventes par statut de paiement
                report.SalesByPaymentStatus = saleItems
                    .GroupBy(s => s.PaymentStatus)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.FinalAmount));

                // Top produits les plus vendus
                var allSaleItems = sales.SelectMany(s => s.SaleItems).ToList();
                var topProducts = allSaleItems
                    .GroupBy(si => si.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        ProductName = g.First().Product.Name,
                        QuantitySold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.TotalPrice)
                    })
                    .OrderByDescending(g => g.Revenue)
                    .Take(10) // Top 10
                    .Select(g => new TopSellingProductItem
                    {
                        ProductName = g.ProductName,
                        QuantitySold = g.QuantitySold,
                        Revenue = g.Revenue
                    })
                    .ToList();

                report.TopSellingProducts = new Dictionary<string, List<TopSellingProductItem>>
                {
                    { "Par Revenus", topProducts }
                };

                // Top produits par quantité
                var topProductsByQuantity = allSaleItems
                    .GroupBy(si => si.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        ProductName = g.First().Product.Name,
                        QuantitySold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.TotalPrice)
                    })
                    .OrderByDescending(g => g.QuantitySold)
                    .Take(10) // Top 10
                    .Select(g => new TopSellingProductItem
                    {
                        ProductName = g.ProductName,
                        QuantitySold = g.QuantitySold,
                        Revenue = g.Revenue
                    })
                    .ToList();

                report.TopSellingProducts.Add("Par Quantité", topProductsByQuantity);

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Numéro",
                    "Date",
                    "Patient",
                    "Centre",
                    "Vendeur",
                    "Montant",
                    "Remise",
                    "Total",
                    "Statut",
                    "Articles"
                };

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GenerateSalesReportError",
                    "Erreur lors de la génération du rapport des ventes",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        #endregion

        #region Rapports Performances

        /// <summary>
        /// Génère un rapport de performance des soignants
        /// </summary>
        public async Task<CaregiverPerformanceReportViewModel> GenerateCaregiverPerformanceReportAsync(CaregiverPerformanceReportFilters filters)
        {
            try
            {
                // Vérifier si le rapport est en cache
                string cacheKey = $"CaregiverPerformanceReport_{JsonSerializer.Serialize(filters)}";
                if (_cache.TryGetValue(cacheKey, out CaregiverPerformanceReportViewModel cachedReport))
                {
                    return cachedReport;
                }

                // Construire la requête de base
                var query = _context.RptCaregiverPerformances.AsQueryable();

                // Appliquer les filtres
                if (filters.HospitalCenterId.HasValue)
                {
                    query = query.Where(r => r.HospitalCenterId == filters.HospitalCenterId);
                }

                if (filters.UserId.HasValue)
                {
                    query = query.Where(r => r.UserId == filters.UserId);
                }

                if (filters.FromDate.HasValue)
                {
                    DateOnly fromDateOnly = DateOnly.FromDateTime(filters.FromDate.Value);
                    query = query.Where(r => r.ReportDate >= fromDateOnly);
                }

                if (filters.ToDate.HasValue)
                {
                    DateOnly toDateOnly = DateOnly.FromDateTime(filters.ToDate.Value);
                    query = query.Where(r => r.ReportDate <= toDateOnly);
                }

                // Récupérer les données
                var performanceData = await query.ToListAsync();

                // Convertir en ViewModel
                var items = performanceData.Select(p => new CaregiverPerformanceReportItem
                {
                    UserId = p.UserId,
                    CaregiverName = p.CaregiverName,
                    HospitalCenterName = p.HospitalCenterName,
                    ReportDate = p.ReportDate.ToDateTime(new TimeOnly(0, 0)),
                    PatientsServed = p.PatientsServed,
                    CareServicesProvided = p.CareServicesProvided,
                    ExaminationsRequested = p.ExaminationsRequested,
                    PrescriptionsIssued = p.PrescriptionsIssued,
                    SalesMade = p.SalesMade,
                    TotalRevenueGenerated = p.TotalRevenueGenerated
                }).ToList();

                // Créer le rapport
                var report = new CaregiverPerformanceReportViewModel
                {
                    ReportTitle = "Rapport de Performance des Soignants",
                    ReportDescription = "Performance des soignants par période",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Items = items,
                    Filters = filters,
                    TotalCount = items.Count
                };

                // Calculer les statistiques
                report.TotalCaregivers = items.Select(i => i.UserId).Distinct().Count();
                report.TotalPatientsServed = items.Sum(i => i.PatientsServed);
                report.TotalCareServicesProvided = items.Sum(i => i.CareServicesProvided);
                report.TotalExaminationsRequested = items.Sum(i => i.ExaminationsRequested);
                report.TotalPrescriptionsIssued = items.Sum(i => i.PrescriptionsIssued);
                report.TotalSalesMade = items.Sum(i => i.SalesMade);
                report.TotalRevenueGenerated = items.Sum(i => i.TotalRevenueGenerated);

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Date",
                    "Soignant",
                    "Centre",
                    "Patients",
                    "Soins",
                    "Examens",
                    "Prescriptions",
                    "Ventes",
                    "Revenus"
                };

                // Mettre en cache le rapport (15 minutes)
                _cache.Set(cacheKey, report, TimeSpan.FromMinutes(15));

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GenerateCaregiverPerformanceReportError",
                    "Erreur lors de la génération du rapport de performance des soignants",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        /// <summary>
        /// Génère un rapport d'activité médicale
        /// </summary>
        public async Task<MedicalActivityReportViewModel> GenerateMedicalActivityReportAsync(MedicalActivityReportFilters filters)
        {
            try
            {
                // Préparation des dates
                DateTime fromDate = filters.FromDate ?? DateTime.Now.AddMonths(-1);
                DateTime toDate = filters.ToDate ?? DateTime.Now;

                // Liste des jours dans la période
                List<DateTime> dateRange = Enumerable.Range(0, (toDate - fromDate).Days + 1)
                    .Select(offset => fromDate.AddDays(offset))
                    .ToList();

                // Créer un modèle de rapport
                var report = new MedicalActivityReportViewModel
                {
                    ReportTitle = "Rapport d'Activité Médicale",
                    ReportDescription = "Activité médicale par jour",
                    GeneratedAt = TimeZoneHelper.GetCameroonTime(),
                    Filters = filters,
                    Items = new List<MedicalActivityReportItem>(),
                    PatientTrend = new List<TrendPoint>(),
                    RevenueTrend = new List<TrendPoint>()
                };

                // Construire des requêtes pour chaque type de données
                var hospitalCenterIds = filters.HospitalCenterId.HasValue
                    ? new[] { filters.HospitalCenterId.Value }
                    : await _context.HospitalCenters.Select(hc => hc.Id).ToArrayAsync();

                foreach (var hospitalCenterId in hospitalCenterIds)
                {
                    var centerName = await _context.HospitalCenters
                        .Where(hc => hc.Id == hospitalCenterId)
                        .Select(hc => hc.Name)
                        .FirstOrDefaultAsync() ?? "Inconnu";

                    foreach (var date in dateRange)
                    {
                        var dateOnly = DateOnly.FromDateTime(date);

                        // Nouveaux patients
                        var newPatients = await _context.Patients
                            .CountAsync(p => p.CreatedAt.Date == date.Date &&
                                p.CareEpisodes.Any(ce => ce.HospitalCenterId == hospitalCenterId));

                        // Épisodes de soins
                        var careEpisodes = await _context.CareEpisodes
                            .CountAsync(ce => ce.EpisodeStartDate.Date == date.Date &&
                                ce.HospitalCenterId == hospitalCenterId);

                        // Services de soins
                        var careServices = await _context.CareServices
                            .CountAsync(cs => cs.ServiceDate.Date == date.Date &&
                                cs.CareEpisode.HospitalCenterId == hospitalCenterId);

                        // Examens
                        var examinations = await _context.Examinations
                            .CountAsync(e => e.RequestDate.Date == date.Date &&
                                e.HospitalCenterId == hospitalCenterId);

                        if (filters.ExaminationTypeId.HasValue)
                        {
                            examinations = await _context.Examinations
                                .CountAsync(e => e.RequestDate.Date == date.Date &&
                                    e.HospitalCenterId == hospitalCenterId &&
                                    e.ExaminationTypeId == filters.ExaminationTypeId);
                        }

                        // Prescriptions
                        var prescriptions = await _context.Prescriptions
                            .CountAsync(p => p.PrescriptionDate.Date == date.Date &&
                                p.HospitalCenterId == hospitalCenterId);

                        // Revenus
                        decimal totalRevenue = await _context.Payments
                            .Where(p => p.PaymentDate.Date == date.Date &&
                                p.HospitalCenterId == hospitalCenterId)
                            .SumAsync(p => p.Amount);

                        // Si aucune activité ce jour, passer au suivant
                        if (newPatients == 0 && careEpisodes == 0 && careServices == 0 &&
                            examinations == 0 && prescriptions == 0 && totalRevenue == 0)
                        {
                            continue;
                        }

                        // Ajouter l'élément au rapport
                        report.Items.Add(new MedicalActivityReportItem
                        {
                            ActivityDate = date,
                            HospitalCenterName = centerName,
                            NewPatients = newPatients,
                            CareEpisodes = careEpisodes,
                            CareServices = careServices,
                            Examinations = examinations,
                            Prescriptions = prescriptions,
                            TotalRevenue = totalRevenue
                        });

                        // Ajouter aux tendances
                        report.PatientTrend.Add(new TrendPoint
                        {
                            Date = date,
                            Value = newPatients
                        });

                        report.RevenueTrend.Add(new TrendPoint
                        {
                            Date = date,
                            Value = totalRevenue
                        });
                    }
                }

                // Trier les items par date
                report.Items = report.Items.OrderBy(i => i.ActivityDate).ToList();
                report.TotalCount = report.Items.Count;

                // Calculer les statistiques globales
                report.TotalEpisodes = report.Items.Sum(i => i.CareEpisodes);

                // Récupérer les épisodes actifs
                report.TotalActiveEpisodes = await _context.CareEpisodes
                    .CountAsync(ce => ce.Status == "Active" &&
                        (filters.HospitalCenterId == null || ce.HospitalCenterId == filters.HospitalCenterId));

                report.TotalCompletedEpisodes = await _context.CareEpisodes
                    .CountAsync(ce => ce.Status == "Completed" &&
                        (filters.HospitalCenterId == null || ce.HospitalCenterId == filters.HospitalCenterId) &&
                        ce.EpisodeStartDate >= fromDate && ce.EpisodeStartDate <= toDate);

                report.TotalExaminations = report.Items.Sum(i => i.Examinations);
                report.TotalPrescriptions = report.Items.Sum(i => i.Prescriptions);
                report.TotalPatients = report.Items.Sum(i => i.NewPatients);

                // Épisodes par diagnostic
                var episodesByDiagnosis = await _context.CareEpisodes
                    .Where(ce => ce.EpisodeStartDate >= fromDate && ce.EpisodeStartDate <= toDate &&
                        (filters.HospitalCenterId == null || ce.HospitalCenterId == filters.HospitalCenterId))
                    .GroupBy(ce => ce.Diagnosis.DiagnosisName)
                    .Select(g => new { DiagnosisName = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10) // Top 10
                    .ToDictionaryAsync(g => g.DiagnosisName, g => g.Count);

                report.EpisodesByDiagnosis = episodesByDiagnosis;

                // Définir les en-têtes de colonnes
                report.ColumnHeaders = new List<string>
                {
                    "Date",
                    "Centre",
                    "Nouveaux Patients",
                    "Épisodes",
                    "Services",
                    "Examens",
                    "Prescriptions",
                    "Revenus"
                };

                return report;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "GenerateMedicalActivityReportError",
                    "Erreur lors de la génération du rapport d'activité médicale",
                    details: new { Error = ex.Message, Filters = filters });
                throw;
            }
        }

        #endregion

        #region Exports

        /// <summary>
        /// Exporte un rapport au format Excel
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(ExportParameters parameters)
        {
            try
            {
                // Récupérer les données du rapport selon le type
                object reportData = await GetReportDataAsync(parameters.ReportType, parameters.Filters);

                // Créer le fichier Excel
                using (var workbook = new XLWorkbook())
                {
                    // Ajouter une feuille pour le rapport
                    var worksheet = workbook.Worksheets.Add(parameters.FileName ?? parameters.ReportType);

                    // Ajouter le titre et la date
                    var title = parameters.CustomTitle ?? GetReportTitle(parameters.ReportType);
                    worksheet.Cell("A1").Value = title;
                    worksheet.Cell("A1").Style.Font.Bold = true;
                    worksheet.Cell("A1").Style.Font.FontSize = 16;
                    worksheet.Cell("A2").Value = $"Généré le {DateTime.Now:dd/MM/yyyy HH:mm}";
                    worksheet.Cell("A3").Value = string.Empty; // Ligne vide

                    // Ajouter les filtres appliqués
                    int rowIndex = 4;
                    var filters = GetFilterDescription(parameters.ReportType, parameters.Filters);
                    foreach (var filter in filters)
                    {
                        worksheet.Cell($"A{rowIndex}").Value = filter.Key;
                        worksheet.Cell($"B{rowIndex}").Value = filter.Value;
                        rowIndex++;
                    }

                    rowIndex++; // Ligne vide

                    // Ajouter les en-têtes
                    var headers = GetReportHeaders(parameters.ReportType);
                    for (int i = 0; i < headers.Count; i++)
                    {
                        worksheet.Cell(rowIndex, i + 1).Value = headers[i];
                        worksheet.Cell(rowIndex, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(rowIndex, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    rowIndex++;

                    // Ajouter les données
                    var data = GetReportExcelData(parameters.ReportType, reportData);
                    foreach (var row in data)
                    {
                        for (int i = 0; i < row.Count; i++)
                        {
                            worksheet.Cell(rowIndex, i + 1).Value =  (row[i]).ToString();
                        }
                        rowIndex++;
                    }

                    // Ajouter les statistiques selon le type de rapport
                    rowIndex++;
                    var stats = GetReportStatistics(parameters.ReportType, reportData);
                    foreach (var stat in stats)
                    {
                        worksheet.Cell(rowIndex, 1).Value = stat.Key;
                        worksheet.Cell(rowIndex, 2).Value = (stat.Value).ToString();
                        worksheet.Cell(rowIndex, 1).Style.Font.Bold = true;
                        rowIndex++;
                    }

                    // Ajuster la largeur des colonnes
                    worksheet.Columns().AdjustToContents();

                    // Convertir en bytes
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "ExportToExcelError",
                    "Erreur lors de l'export du rapport en Excel",
                    details: new { Error = ex.Message, Parameters = parameters });
                throw;
            }
        }

        /// <summary>
        /// Exporte un rapport au format PDF
        /// </summary>
        public async Task<byte[]> ExportToPdfAsync(ExportParameters parameters)
        {
            try
            {
                // Récupérer les données du rapport selon le type
                object reportData = await GetReportDataAsync(parameters.ReportType, parameters.Filters);

                // Créer le document PDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(style => style.FontSize(10));

                        // Entête
                        page.Header().Element(CreateHeader(parameters));

                        // Contenu
                        page.Content().Element(CreateContent(parameters.ReportType, reportData));

                        // Pied de page
                        page.Footer().Element(CreateFooter);
                    });
                });

                // Générer le PDF
                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "ExportToPdfError",
                    "Erreur lors de l'export du rapport en PDF",
                    details: new { Error = ex.Message, Parameters = parameters });
                throw;
            }
        }

        #endregion

        #region Rapports Planifiés

        /// <summary>
        /// Planifie un rapport récurrent
        /// </summary>
        public async Task<bool> ScheduleRecurringReportAsync(RecurringReportSchedule schedule)
        {
            // Dans un projet réel, on implémenterait ici la logique de planification


            // Pour ce prototype, on simule juste le succès
            await _logger.LogInfoAsync("ReportService", "ScheduleRecurringReport",
                $"Planification du rapport {schedule.ReportName} ({schedule.Frequency})");

            return true;
        }

        /// <summary>
        /// Récupère les rapports planifiés
        /// </summary>
        public async Task<List<RecurringReportViewModel>> GetScheduledReportsAsync(int? userId = null)
        {
            // Dans un projet réel, on récupérerait ici les rapports planifiés depuis la base

            // Pour ce prototype, on retourne une liste fictive
            var reports = new List<RecurringReportViewModel>
            {
                new RecurringReportViewModel
                {
                    Id = 1,
                    ReportType = "StockStatusReport",
                    ReportName = "Rapport hebdomadaire des stocks",
                    Frequency = "Weekly",
                    ScheduleDescription = "Chaque lundi à 08:00",
                    Format = "Excel",
                    EmailRecipients = "direction@hospital.local",
                    SaveToServer = true,
                    ServerPath = "/reports/stock/",
                    CreatedByName = "Admin Système",
                    CreatedAt = DateTime.Now.AddDays(-30),
                    IsActive = true,
                    LastExecutionDate = DateTime.Now.AddDays(-7),
                    NextExecutionDate = DateTime.Now.AddDays(7)
                },
                new RecurringReportViewModel
                {
                    Id = 2,
                    ReportType = "FinancialActivityReport",
                    ReportName = "Rapport financier mensuel",
                    Frequency = "Monthly",
                    ScheduleDescription = "Le 1er du mois à 06:00",
                    Format = "PDF",
                    EmailRecipients = "finance@hospital.local",
                    SaveToServer = true,
                    ServerPath = "/reports/finance/",
                    CreatedByName = "Admin Système",
                    CreatedAt = DateTime.Now.AddDays(-60),
                    IsActive = true,
                    LastExecutionDate = DateTime.Now.AddDays(-15),
                    NextExecutionDate = DateTime.Now.AddDays(15)
                }
            };

            // Filtrer par utilisateur si nécessaire
            if (userId.HasValue)
            {
                // Dans un vrai système, on filtrerait ici
            }

            return reports;
        }

        /// <summary>
        /// Supprime un rapport planifié
        /// </summary>
        public async Task<bool> DeleteScheduledReportAsync(int scheduleId)
        {
            // Dans un projet réel, on supprimerait ici le rapport planifié

            // Pour ce prototype, on simule juste le succès
            await _logger.LogInfoAsync("ReportService", "DeleteScheduledReport",
                $"Suppression du rapport planifié {scheduleId}");

            return true;
        }

        #endregion

        #region Gestion du Cache

        /// <summary>
        /// Vérifie si un rapport précalculé existe
        /// </summary>
        public Task<bool> CachedReportExistsAsync(string reportType, string reportKey)
        {
            string cacheKey = $"{reportType}_{reportKey}";
            return Task.FromResult(_cache.TryGetValue(cacheKey, out _));
        }

        /// <summary>
        /// Récupère un rapport précalculé depuis le cache
        /// </summary>
        public Task<object> GetCachedReportAsync(string reportType, string reportKey)
        {
            string cacheKey = $"{reportType}_{reportKey}";
            if (_cache.TryGetValue(cacheKey, out object cachedReport))
            {
                return Task.FromResult(cachedReport);
            }

            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Force le calcul d'un rapport (mise à jour des tables de rapport)
        /// </summary>
        public async Task<bool> RefreshReportDataAsync(string reportType, DateTime? asOfDate = null)
        {
            try
            {
                // Exécuter la procédure stockée appropriée selon le type de rapport
                switch (reportType)
                {
                    case "StockStatusReport":
                        await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateStockReports");
                        break;

                    case "UserCenterReport":
                         await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateUserCenterDetails");
                        break;

                    case "FinancialActivityReport":
                        await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateFinancialActivity @asOfDate",
                           new SqlParameter("@asOfDate", asOfDate ?? DateTime.Now));
                        break;

                    case "CaregiverPerformanceReport":
                        await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateCaregiverPerformance @asOfDate",
                           new SqlParameter("@asOfDate", asOfDate ?? DateTime.Now));
                        break;

                    default:
                        return false;
                }

                // Vider le cache pour ce type de rapport
                var cacheKeys = _cache.GetKeys().Where(k => k.ToString().StartsWith($"{reportType}_")).ToList();
                foreach (var key in cacheKeys)
                {
                    _cache.Remove(key);
                }

                await _logger.LogInfoAsync("ReportService", "RefreshReportData",
                    $"Rafraîchissement des données du rapport {reportType}");

                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportService", "RefreshReportDataError",
                    $"Erreur lors du rafraîchissement des données du rapport {reportType}",
                    details: new { Error = ex.Message });
                return false;
            }
        }

        #endregion

        #region Méthodes Utilitaires

        /// <summary>
        /// Récupère les données d'un rapport selon son type
        /// </summary>
        private async Task<object> GetReportDataAsync(string reportType, object filters)
        {
            switch (reportType)
            {
                case "UserCenterReport":
                    return await GenerateUserCenterReportAsync((UserCenterReportFilters)filters);

                case "ActiveSessionsReport":
                    return await GenerateActiveSessionsReportAsync((ActiveSessionsReportFilters)filters);

                case "StockStatusReport":
                    return await GenerateStockStatusReportAsync((StockStatusReportFilters)filters);

                case "StockMovementReport":
                    return await GenerateStockMovementReportAsync((StockMovementReportFilters)filters);

                case "StockValuationReport":
                    return await GenerateStockValuationReportAsync((StockValuationReportFilters)filters);

                case "FinancialActivityReport":
                    return await GenerateFinancialActivityReportAsync((FinancialActivityReportFilters)filters);

                case "PaymentReport":
                    return await GeneratePaymentReportAsync((PaymentReportFilters)filters);

                case "SalesReport":
                    return await GenerateSalesReportAsync((SalesReportFilters)filters);

                case "CaregiverPerformanceReport":
                    return await GenerateCaregiverPerformanceReportAsync((CaregiverPerformanceReportFilters)filters);

                case "MedicalActivityReport":
                    return await GenerateMedicalActivityReportAsync((MedicalActivityReportFilters)filters);

                default:
                    throw new ArgumentException($"Type de rapport inconnu : {reportType}");
            }
        }

        /// <summary>
        /// Récupère le titre d'un rapport selon son type
        /// </summary>
        private string GetReportTitle(string reportType)
        {
            return reportType switch
            {
                "UserCenterReport" => "Rapport des Utilisateurs et Centres",
                "ActiveSessionsReport" => "Rapport des Sessions Actives",
                "StockStatusReport" => "Rapport d'État des Stocks",
                "StockMovementReport" => "Rapport des Mouvements de Stock",
                "StockValuationReport" => "Rapport de Valorisation des Stocks",
                "FinancialActivityReport" => "Rapport d'Activité Financière",
                "PaymentReport" => "Rapport des Paiements",
                "SalesReport" => "Rapport des Ventes",
                "CaregiverPerformanceReport" => "Rapport de Performance des Soignants",
                "MedicalActivityReport" => "Rapport d'Activité Médicale",
                _ => "Rapport"
            };
        }

        /// <summary>
        /// Récupère la description des filtres appliqués à un rapport
        /// </summary>
        private Dictionary<string, string> GetFilterDescription(string reportType, object filters)
        {
            var result = new Dictionary<string, string>();
            switch (reportType)
            {
                case "UserCenterReport":
                    var userCenterFilters = (UserCenterReportFilters)filters;
                    if (userCenterFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(userCenterFilters.HospitalCenterId.Value));
                    }
                    if (!string.IsNullOrEmpty(userCenterFilters.RoleType))
                    {
                        result.Add("Rôle", userCenterFilters.RoleType);
                    }
                    if (userCenterFilters.IsUserActive.HasValue)
                    {
                        result.Add("Statut Utilisateur", userCenterFilters.IsUserActive.Value ? "Actif" : "Inactif");
                    }
                    if (userCenterFilters.IsAssignmentActive.HasValue)
                    {
                        result.Add("Statut Affectation", userCenterFilters.IsAssignmentActive.Value ? "Active" : "Inactive");
                    }
                    break;

                case "ActiveSessionsReport":
                    var activeSessionsFilters = (ActiveSessionsReportFilters)filters;
                    if (activeSessionsFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(activeSessionsFilters.HospitalCenterId.Value));
                    }
                    if (activeSessionsFilters.MinHoursConnected.HasValue)
                    {
                        result.Add("Durée minimale", $"{activeSessionsFilters.MinHoursConnected.Value} heures");
                    }
                    if (!string.IsNullOrEmpty(activeSessionsFilters.SearchTerm))
                    {
                        result.Add("Recherche", activeSessionsFilters.SearchTerm);
                    }
                    break;

                case "StockStatusReport":
                    var stockStatusFilters = (StockStatusReportFilters)filters;
                    if (stockStatusFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(stockStatusFilters.HospitalCenterId.Value));
                    }
                    if (stockStatusFilters.ProductCategoryId.HasValue)
                    {
                        result.Add("Catégorie", GetCategoryName(stockStatusFilters.ProductCategoryId.Value));
                    }
                    if (!string.IsNullOrEmpty(stockStatusFilters.StockStatus))
                    {
                        result.Add("Statut Stock", stockStatusFilters.StockStatus);
                    }
                    break;

                case "StockMovementReport":
                    var stockMovementFilters = (StockMovementReportFilters)filters;
                    if (stockMovementFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(stockMovementFilters.HospitalCenterId.Value));
                    }
                    //if (stockMovementFilters.ProductId.HasValue)
                    //{
                    //    result.Add("Produit", GetProductName(stockMovementFilters.ProductId.Value));
                    //}
                    if (!string.IsNullOrEmpty(stockMovementFilters.MovementType))
                    {
                        result.Add("Type de mouvement", stockMovementFilters.MovementType);
                    }
                    if (!string.IsNullOrEmpty(stockMovementFilters.ReferenceType))
                    {
                        result.Add("Type de référence", stockMovementFilters.ReferenceType);
                    }
                    if (stockMovementFilters.FromDate.HasValue)
                    {
                        result.Add("Du", stockMovementFilters.FromDate.Value.ToString("dd/MM/yyyy"));
                    }
                    if (stockMovementFilters.ToDate.HasValue)
                    {
                        result.Add("Au", stockMovementFilters.ToDate.Value.ToString("dd/MM/yyyy"));
                    }
                    break;

                case "StockValuationReport":
                    var stockValuationFilters = (StockValuationReportFilters)filters;
                    if (stockValuationFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(stockValuationFilters.HospitalCenterId.Value));
                    }
                    if (stockValuationFilters.ProductCategoryId.HasValue)
                    {
                        result.Add("Catégorie", GetCategoryName(stockValuationFilters.ProductCategoryId.Value));
                    }
                    result.Add("Valorisation au", stockValuationFilters.ValuationType switch
                    {
                        "SellingPrice" => "Prix de vente",
                        "LastPurchasePrice" => "Dernier prix d'achat",
                        "AveragePrice" => "Prix moyen",
                        _ => stockValuationFilters.ValuationType
                    });
                    break;

                case "FinancialActivityReport":
                    var financialActivityFilters = (FinancialActivityReportFilters)filters;
                    if (financialActivityFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(financialActivityFilters.HospitalCenterId.Value));
                    }
                    result.Add("Groupé par", financialActivityFilters.GroupBy switch
                    {
                        "Day" => "Jour",
                        "Week" => "Semaine",
                        "Month" => "Mois",
                        _ => financialActivityFilters.GroupBy
                    });
                    if (financialActivityFilters.FromDate.HasValue)
                    {
                        result.Add("Du", financialActivityFilters.FromDate.Value.ToString("dd/MM/yyyy"));
                    }
                    if (financialActivityFilters.ToDate.HasValue)
                    {
                        result.Add("Au", financialActivityFilters.ToDate.Value.ToString("dd/MM/yyyy"));
                    }
                    break;

                case "PaymentReport":
                    var paymentFilters = (PaymentReportFilters)filters;
                    if (paymentFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(paymentFilters.HospitalCenterId.Value));
                    }
                    //if (paymentFilters.PaymentMethodId.HasValue)
                    //{
                    //    result.Add("Méthode de paiement", GetPaymentMethodName(paymentFilters.PaymentMethodId.Value));
                    //}
                    if (!string.IsNullOrEmpty(paymentFilters.ReferenceType))
                    {
                        result.Add("Type de référence", paymentFilters.ReferenceType);
                    }
                    //if (paymentFilters.PatientId.HasValue)
                    //{
                    //    result.Add("Patient", GetPatientName(paymentFilters.PatientId.Value));
                    //}
                    //if (paymentFilters.ReceivedBy.HasValue)
                    //{
                    //    result.Add("Reçu par", GetUserName(paymentFilters.ReceivedBy.Value));
                    //}
                    if (paymentFilters.MinAmount.HasValue)
                    {
                        result.Add("Montant minimum", $"{paymentFilters.MinAmount.Value:N0} FCFA");
                    }
                    if (paymentFilters.MaxAmount.HasValue)
                    {
                        result.Add("Montant maximum", $"{paymentFilters.MaxAmount.Value:N0} FCFA");
                    }
                    if (paymentFilters.FromDate.HasValue)
                    {
                        result.Add("Du", paymentFilters.FromDate.Value.ToString("dd/MM/yyyy"));
                    }
                    if (paymentFilters.ToDate.HasValue)
                    {
                        result.Add("Au", paymentFilters.ToDate.Value.ToString("dd/MM/yyyy"));
                    }
                    break;

                case "SalesReport":
                    var salesFilters = (SalesReportFilters)filters;
                    if (salesFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(salesFilters.HospitalCenterId.Value));
                    }
                    if (!string.IsNullOrEmpty(salesFilters.PaymentStatus))
                    {
                        result.Add("Statut de paiement", salesFilters.PaymentStatus);
                    }
                    //if (salesFilters.PatientId.HasValue)
                    //{
                    //    result.Add("Patient", GetPatientName(salesFilters.PatientId.Value));
                    //}
                    //if (salesFilters.SoldBy.HasValue)
                    //{
                    //    result.Add("Vendu par", GetUserName(salesFilters.SoldBy.Value));
                    //}
                    //if (salesFilters.ProductId.HasValue)
                    //{
                    //    result.Add("Produit", GetProductName(salesFilters.ProductId.Value));
                    //}
                    if (salesFilters.FromDate.HasValue)
                    {
                        result.Add("Du", salesFilters.FromDate.Value.ToString("dd/MM/yyyy"));
                    }
                    if (salesFilters.ToDate.HasValue)
                    {
                        result.Add("Au", salesFilters.ToDate.Value.ToString("dd/MM/yyyy"));
                    }
                    break;

                case "CaregiverPerformanceReport":
                    var caregiverPerformanceFilters = (CaregiverPerformanceReportFilters)filters;
                    if (caregiverPerformanceFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(caregiverPerformanceFilters.HospitalCenterId.Value));
                    }
                    //if (caregiverPerformanceFilters.UserId.HasValue)
                    //{
                    //    result.Add("Soignant", GetUserName(caregiverPerformanceFilters.UserId.Value));
                    //}
                    if (caregiverPerformanceFilters.FromDate.HasValue)
                    {
                        result.Add("Du", caregiverPerformanceFilters.FromDate.Value.ToString("dd/MM/yyyy"));
                    }
                    if (caregiverPerformanceFilters.ToDate.HasValue)
                    {
                        result.Add("Au", caregiverPerformanceFilters.ToDate.Value.ToString("dd/MM/yyyy"));
                    }
                    break;

                case "MedicalActivityReport":
                    var medicalActivityFilters = (MedicalActivityReportFilters)filters;
                    if (medicalActivityFilters.HospitalCenterId.HasValue)
                    {
                        result.Add("Centre", GetCenterName(medicalActivityFilters.HospitalCenterId.Value));
                    }
                    //if (medicalActivityFilters.CareTypeId.HasValue)
                    //{
                    //    result.Add("Type de soin", GetCareTypeName(medicalActivityFilters.CareTypeId.Value));
                    //}
                    //if (medicalActivityFilters.ExaminationTypeId.HasValue)
                    //{
                    //    result.Add("Type d'examen", GetExaminationTypeName(medicalActivityFilters.ExaminationTypeId.Value));
                    //}
                    if (medicalActivityFilters.FromDate.HasValue)
                    {
                        result.Add("Du", medicalActivityFilters.FromDate.Value.ToString("dd/MM/yyyy"));
                    }
                    if (medicalActivityFilters.ToDate.HasValue)
                    {
                        result.Add("Au", medicalActivityFilters.ToDate.Value.ToString("dd/MM/yyyy"));
                    }
                    break;

                default:
                    // Pour les types non gérés, extraire les dates si disponibles
                    var type = filters.GetType();
                    var fromDateProp = type.GetProperty("FromDate");
                    var toDateProp = type.GetProperty("ToDate");
                    if (fromDateProp != null)
                    {
                        var fromDate = fromDateProp.GetValue(filters) as DateTime?;
                        if (fromDate.HasValue)
                        {
                            result.Add("Du", fromDate.Value.ToString("dd/MM/yyyy"));
                        }
                    }
                    if (toDateProp != null)
                    {
                        var toDate = toDateProp.GetValue(filters) as DateTime?;
                        if (toDate.HasValue)
                        {
                            result.Add("Au", toDate.Value.ToString("dd/MM/yyyy"));
                        }
                    }
                    break;
            }
            return result;
        }

        /// <summary>
        /// Récupère les en-têtes de colonnes pour un rapport
        /// </summary>
        private List<string> GetReportHeaders(string reportType)
        {
            return reportType switch
            {
                "UserCenterReport" => new List<string> { "Utilisateur", "Email", "Statut", "Centre", "Rôle", "Statut Affectation", "Date Début", "Date Fin" },
                "ActiveSessionsReport" => new List<string> { "Utilisateur", "Email", "Centre", "Connexion", "IP", "Durée" },
                "StockStatusReport" => new List<string> { "Produit", "Catégorie", "Centre", "Quantité", "Seuil Min", "Seuil Max", "Statut", "Dernier Mouvement", "Valeur" },
                "StockMovementReport" => new List<string> { "Date", "Produit", "Centre", "Type", "Quantité", "Référence", "Notes", "Utilisateur" },
                "StockValuationReport" => new List<string> { "Produit", "Catégorie", "Centre", "Quantité", "Prix unitaire", "Valeur totale" },
                "FinancialActivityReport" => new List<string> { "Date", "Centre", "Ventes", "Revenus Soins", "Revenus Examens", "Total Revenus", "Paiements Espèces", "Paiements Mobile", "Transactions", "Patients" },
                "PaymentReport" => new List<string> { "Date", "Patient", "Centre", "Méthode", "Référence", "Montant", "Reçu par", "Transaction" },
                "SalesReport" => new List<string> { "Numéro", "Date", "Patient", "Centre", "Vendeur", "Montant", "Remise", "Total", "Statut", "Articles" },
                "CaregiverPerformanceReport" => new List<string> { "Date", "Soignant", "Centre", "Patients", "Soins", "Examens", "Prescriptions", "Ventes", "Revenus" },
                "MedicalActivityReport" => new List<string> { "Date", "Centre", "Nouveaux Patients", "Épisodes", "Services", "Examens", "Prescriptions", "Revenus" },
                _ => new List<string>()
            };
        }

        /// <summary>
        /// Récupère les données d'un rapport au format Excel
        /// </summary>
        private List<List<object>> GetReportExcelData(string reportType, object reportData)
        {
            var result = new List<List<object>>();

            switch (reportType)
            {
                case "UserCenterReport":
                    var userCenterReport = (UserCenterReportViewModel)reportData;
                    foreach (var item in userCenterReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.FullName,
                            item.Email,
                            item.UserStatusText,
                            item.HospitalCenterName,
                            item.RoleType,
                            item.AssignmentStatusText,
                            item.FormattedStartDate,
                            item.FormattedEndDate
                        });
                    }
                    break;

                case "ActiveSessionsReport":
                    var activeSessionsReport = (ActiveSessionsReportViewModel)reportData;
                    foreach (var item in activeSessionsReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.UserName,
                            item.Email,
                            item.CurrentHospitalCenter,
                            item.FormattedLoginTime,
                            item.IpAddress,
                            item.ConnectionDuration
                        });
                    }
                    break;

                case "StockStatusReport":
                    var stockStatusReport = (StockStatusReportViewModel)reportData;
                    foreach (var item in stockStatusReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.ProductName,
                            item.ProductCategory,
                            item.HospitalCenterName,
                            item.CurrentQuantity,
                            item.MinimumThreshold,
                            item.MaximumThreshold,
                            item.StockStatus,
                            item.LastMovementDate,
                            item.TotalValue
                        });
                    }
                    break;

                case "StockMovementReport":
                    var stockMovementReport = (StockMovementReportViewModel)reportData;
                    foreach (var item in stockMovementReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.FormattedMovementDate,
                            item.ProductName,
                            item.HospitalCenterName,
                            item.MovementType,
                            item.FormattedQuantity,
                            item.ReferenceType + (item.ReferenceId.HasValue ? " #" + item.ReferenceId : ""),
                            item.Notes,
                            item.CreatedByName
                        });
                    }
                    break;

                case "StockValuationReport":
                    var stockValuationReport = (StockValuationReportViewModel)reportData;
                    foreach (var item in stockValuationReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.ProductName,
                            item.CategoryName,
                            item.HospitalCenterName,
                            item.FormattedQuantity,
                            item.FormattedUnitPrice,
                            item.FormattedTotalValue
                        });
                    }
                    break;

                case "FinancialActivityReport":
                    var financialActivityReport = (FinancialActivityReportViewModel)reportData;
                    foreach (var item in financialActivityReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.FormattedReportDate,
                            item.HospitalCenterName,
                            item.TotalSales,
                            item.TotalCareRevenue,
                            item.TotalExaminationRevenue,
                            item.TotalRevenue,
                            item.TotalCashPayments,
                            item.TotalMobilePayments,
                            item.TransactionCount,
                            item.PatientCount
                        });
                    }
                    break;

                case "PaymentReport":
                    var paymentReport = (PaymentReportViewModel)reportData;
                    foreach (var item in paymentReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.FormattedPaymentDate,
                            item.PatientName,
                            item.HospitalCenterName,
                            item.PaymentMethodName,
                            item.ReferenceType + " #" + item.ReferenceId,
                            item.FormattedAmount,
                            item.ReceivedByName,
                            item.TransactionReference
                        });
                    }
                    break;

                case "SalesReport":
                    var salesReport = (SalesReportViewModel)reportData;
                    foreach (var item in salesReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.SaleNumber,
                            item.FormattedSaleDate,
                            item.PatientName,
                            item.HospitalCenterName,
                            item.SoldByName,
                            item.FormattedTotalAmount,
                            item.FormattedDiscountAmount,
                            item.FormattedFinalAmount,
                            item.PaymentStatus,
                            item.ItemCount
                        });
                    }
                    break;

                case "CaregiverPerformanceReport":
                    var caregiverPerformanceReport = (CaregiverPerformanceReportViewModel)reportData;
                    foreach (var item in caregiverPerformanceReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.FormattedReportDate,
                            item.CaregiverName,
                            item.HospitalCenterName,
                            item.PatientsServed,
                            item.CareServicesProvided,
                            item.ExaminationsRequested,
                            item.PrescriptionsIssued,
                            item.SalesMade,
                            item.FormattedTotalRevenueGenerated
                        });
                    }
                    break;

                case "MedicalActivityReport":
                    var medicalActivityReport = (MedicalActivityReportViewModel)reportData;
                    foreach (var item in medicalActivityReport.Items)
                    {
                        result.Add(new List<object>
                        {
                            item.FormattedActivityDate,
                            item.HospitalCenterName,
                            item.NewPatients,
                            item.CareEpisodes,
                            item.CareServices,
                            item.Examinations,
                            item.Prescriptions,
                            item.FormattedTotalRevenue
                        });
                    }
                    break;

                default:
                    // Pour les types non gérés, retourner une liste vide
                    break;
            }

            return result;
        }

        /// <summary>
        /// Récupère les statistiques d'un rapport
        /// </summary>
        private Dictionary<string, object> GetReportStatistics(string reportType, object reportData)
        {
            var result = new Dictionary<string, object>();
            switch (reportType)
            {
                case "UserCenterReport":
                    var userCenterReport = (UserCenterReportViewModel)reportData;
                    result.Add("Total Utilisateurs", userCenterReport.TotalUsers);
                    result.Add("Utilisateurs Actifs", userCenterReport.ActiveUsers);
                    result.Add("Total Affectations", userCenterReport.TotalAssignments);
                    result.Add("Affectations Actives", userCenterReport.ActiveAssignments);
                    break;

                case "ActiveSessionsReport":
                    var activeSessionsReport = (ActiveSessionsReportViewModel)reportData;
                    result.Add("Sessions Actives", activeSessionsReport.TotalActiveSessions);
                    result.Add("Durée Moyenne (minutes)", activeSessionsReport.AverageSessionDuration);
                    break;

                case "StockStatusReport":
                    var stockStatusReport = (StockStatusReportViewModel)reportData;
                    result.Add("Total Produits", stockStatusReport.TotalProducts);
                    result.Add("Produits en Stock Critique", stockStatusReport.ProductsWithCriticalStock);
                    result.Add("Produits en Stock Bas", stockStatusReport.ProductsWithLowStock);
                    result.Add("Produits en Stock Normal", stockStatusReport.ProductsWithNormalStock);
                    result.Add("Produits en Surstock", stockStatusReport.ProductsWithOverstock);
                    result.Add("Valeur Totale du Stock", stockStatusReport.TotalStockValue);
                    break;

                case "StockMovementReport":
                    var stockMovementReport = (StockMovementReportViewModel)reportData;
                    result.Add("Total Mouvements", stockMovementReport.TotalMovements);
                    result.Add("Entrées Totales", stockMovementReport.TotalInQuantity);
                    result.Add("Sorties Totales", stockMovementReport.TotalOutQuantity);
                    result.Add("Changement Net", stockMovementReport.NetChange);
                    break;

                case "StockValuationReport":
                    var stockValuationReport = (StockValuationReportViewModel)reportData;
                    result.Add("Valeur Totale", stockValuationReport.TotalValue);
                    result.Add("Nombre de Produits", stockValuationReport.Items.Count);
                    break;

                case "FinancialActivityReport":
                    var financialActivityReport = (FinancialActivityReportViewModel)reportData;
                    result.Add("Revenus Totaux", financialActivityReport.TotalRevenue);
                    result.Add("Ventes", financialActivityReport.TotalSales);
                    result.Add("Revenus Soins", financialActivityReport.TotalCareRevenue);
                    result.Add("Revenus Examens", financialActivityReport.TotalExaminationRevenue);
                    result.Add("Transactions", financialActivityReport.TotalTransactionCount);
                    result.Add("Patients Uniques", financialActivityReport.TotalPatientCount);
                    break;

                case "PaymentReport":
                    var paymentReport = (PaymentReportViewModel)reportData;
                    result.Add("Total Paiements", paymentReport.TotalPayments);
                    foreach (var method in paymentReport.PaymentsByMethod)
                    {
                        result.Add($"Paiements {method.Key}", method.Value);
                    }
                    break;

                case "SalesReport":
                    var salesReport = (SalesReportViewModel)reportData;
                    result.Add("Montant Total Ventes", salesReport.TotalSalesAmount);
                    result.Add("Montant Total Remises", salesReport.TotalDiscountAmount);
                    result.Add("Montant Final", salesReport.TotalFinalAmount);
                    result.Add("Produits Vendus", salesReport.TotalProductsSold);
                    break;

                case "CaregiverPerformanceReport":
                    var caregiverPerformanceReport = (CaregiverPerformanceReportViewModel)reportData;
                    result.Add("Total Soignants", caregiverPerformanceReport.TotalCaregivers);
                    result.Add("Patients Servis", caregiverPerformanceReport.TotalPatientsServed);
                    result.Add("Services Fournis", caregiverPerformanceReport.TotalCareServicesProvided);
                    result.Add("Examens Demandés", caregiverPerformanceReport.TotalExaminationsRequested);
                    result.Add("Prescriptions Émises", caregiverPerformanceReport.TotalPrescriptionsIssued);
                    result.Add("Ventes Réalisées", caregiverPerformanceReport.TotalSalesMade);
                    result.Add("Revenus Générés", caregiverPerformanceReport.TotalRevenueGenerated);
                    break;

                case "MedicalActivityReport":
                    var medicalActivityReport = (MedicalActivityReportViewModel)reportData;
                    result.Add("Total Épisodes", medicalActivityReport.TotalEpisodes);
                    result.Add("Épisodes Actifs", medicalActivityReport.TotalActiveEpisodes);
                    result.Add("Épisodes Terminés", medicalActivityReport.TotalCompletedEpisodes);
                    result.Add("Total Examens", medicalActivityReport.TotalExaminations);
                    result.Add("Total Prescriptions", medicalActivityReport.TotalPrescriptions);
                    result.Add("Patients Uniques", medicalActivityReport.TotalPatients);
                    break;

                default:
                    // Pour les types non gérés, retourner un dictionnaire vide
                    break;
            }
            return result;
        }
        /// <summary>
        /// Crée l'en-tête d'un document PDF
        /// </summary>
        private Action<IContainer> CreateHeader(ExportParameters parameters)
        {
            return container =>
            {
                container.Row(row =>
                {
                    // Logo si disponible
                    if (!string.IsNullOrEmpty(parameters.CompanyLogo))
                    {
                        row.ConstantItem(100).Image(parameters.CompanyLogo);
                    }

                    // Titre et date
                    row.RelativeItem().Column(column =>
                    {
                        var title = parameters.CustomTitle ?? GetReportTitle(parameters.ReportType);
                        column.Item().Text(title).FontSize(16).Bold();
                        column.Item().Text($"Généré le {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);

                        // Filtres appliqués
                        var filters = GetFilterDescription(parameters.ReportType, parameters.Filters);
                        if (filters.Any())
                        {
                            column.Item().Text(" ");
                            foreach (var filter in filters)
                            {
                                column.Item().Text($"{filter.Key}: {filter.Value}").FontSize(10);
                            }
                        }
                    });
                });
            };
        }

        /// <summary>
        /// Crée le contenu d'un document PDF selon le type de rapport
        /// </summary>
        private Action<QuestPDF.Infrastructure.IContainer> CreateContent(string reportType, object reportData)
        {
            return container =>
            {
                container.PaddingVertical(10).Column(column =>
                {
                    // Tableau de données
                    column.Item().Table(table =>
                    {
                        // En-têtes
                        var headers = GetReportHeaders(reportType);
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < headers.Count; i++)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            for (int i = 0; i < headers.Count; i++)
                            {
                                header.Cell().Background("#D9D9D9").Padding(5).Text(headers[i]).Bold();
                            }
                        });

                        // Données
                        var data = GetReportExcelData(reportType, reportData);
                        foreach (var rowData in data)
                        {
                            for (int i = 0; i < rowData.Count; i++)
                            {
                                table.Cell().Padding(5).Text(rowData[i]?.ToString() ?? "");
                            }
                        }
                    });

                    // Statistiques
                    column.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Padding(5).Text("Statistiques").Bold();
                            header.Cell().Padding(5).Text("");
                        });

                        var stats = GetReportStatistics(reportType, reportData);
                        foreach (var stat in stats)
                        {
                            table.Cell().Padding(5).Text(stat.Key).Bold();
                            table.Cell().Padding(5).Text(stat.Value.ToString());
                        }
                    });
                });
            };
        }

        /// <summary>
        /// Crée le pied de page d'un document PDF
        /// </summary>
        private void CreateFooter(QuestPDF.Infrastructure.IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().AlignLeft().Text(text =>
                {
                    text.Span("HManagSys - Système de Gestion Hospitalière");
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(10);
                    text.CurrentPageNumber().FontSize(10);
                    text.Span(" sur ").FontSize(10);
                    text.TotalPages().FontSize(10);
                });
            });
        }

        /// <summary>
        /// Détermine si un mouvement de stock est positif
        /// </summary>
        private bool IsPositiveMovement(string type)
        {
            return type switch
            {
                "Initial" => true,
                "Entry" => true,
                "Transfer" when type.Contains("Incoming") => true,
                _ => false
            };
        }

        /// <summary>
        /// Vérifie si deux dates correspondent selon le regroupement choisi
        /// </summary>
        private bool DateMatches(DateTime date1, DateOnly date2, string groupBy)
        {
            return groupBy switch
            {
                "Day" => date1.Date == date2.ToDateTime(new TimeOnly(0, 0)),
                "Week" => GetIso8601WeekOfYear(date1) == GetIso8601WeekOfYear(date2.ToDateTime(new TimeOnly(0, 0))) && date1.Year == date2.Year,
                "Month" => date1.Month == date2.Month && date1.Year == date2.Year,
                _ => date1.Date == date2.ToDateTime(new TimeOnly(0, 0))
            };
        }

        /// <summary>
        /// Obtient la date représentative d'un groupe
        /// </summary>
        private DateTime GetGroupDate(DateOnly date, string groupBy)
        {
            var dateTime = date.ToDateTime(new TimeOnly(0, 0));

            return groupBy switch
            {
                "Day" => dateTime,
                "Week" => GetFirstDayOfWeek(dateTime),
                "Month" => new DateTime(dateTime.Year, dateTime.Month, 1),
                _ => dateTime
            };
        }

        /// <summary>
        /// Obtient le premier jour de la semaine pour une date donnée
        /// </summary>
        private DateTime GetFirstDayOfWeek(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var diff = date.DayOfWeek - culture.DateTimeFormat.FirstDayOfWeek;

            if (diff < 0)
                diff += 7;

            return date.AddDays(-diff).Date;
        }

        /// <summary>
        /// Obtient le numéro de semaine ISO-8601 pour une date
        /// </summary>
        private int GetIso8601WeekOfYear(DateTime date)
        {
            var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date,
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
        }

        /// <summary>
        /// Obtient le nom d'un texte de regroupement
        /// </summary>
        private string GetGroupByText(string groupBy)
        {
            return groupBy switch
            {
                "Day" => "jour",
                "Week" => "semaine",
                "Month" => "mois",
                _ => "période"
            };
        }

        /// <summary>
        /// Obtient le nom d'un centre hospitalier
        /// </summary>
        private string GetCenterName(int centerId)
        {
            return _context.HospitalCenters
                .Where(hc => hc.Id == centerId)
                .Select(hc => hc.Name)
                .FirstOrDefault() ?? "Centre inconnu";
        }

        /// <summary>
        /// Obtient le nom d'une catégorie de produits
        /// </summary>
        private string GetCategoryName(int categoryId)
        {
            return _context.ProductCategories
                .Where(pc => pc.Id == categoryId)
                .Select(pc => pc.Name)
                .FirstOrDefault() ?? "Catégorie inconnue";
        }


        #endregion
    }

    #region Extensions

    /// <summary>
    /// Extensions pour le cache
    /// </summary>
    public static class CacheExtensions
    {
        public static IEnumerable<object> GetKeys(this IMemoryCache cache)
        {
            var cacheEntryFields = typeof(MemoryCache)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var entriesField = cacheEntryFields.FirstOrDefault(f => f.Name == "_entries");

            if (entriesField == null)
                return Enumerable.Empty<object>();

            var entries = entriesField.GetValue(cache);
            var keys = new List<object>();

            // Utilisation de l'interface non générique IDictionary
            if (entries is System.Collections.IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    keys.Add(key);
                }
            }

            return keys;
        }
    }

    #endregion
}