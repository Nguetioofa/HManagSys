using HManagSys.Attributes;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Implementations;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers
{
    /// <summary>
    /// Contrôleur principal pour la gestion des stocks
    /// Point d'entrée principal pour toutes les opérations de stock
    /// </summary>
    [RequireAuthentication]
    [RequireCurrentCenter]
    public class StockController : BaseController
    {
        private readonly IProductService _productService;
        private readonly IProductCategoryService _categoryService;
        private readonly IHospitalCenterRepository _centerRepository;
        private readonly IApplicationLogger _appLogger;

        public StockController(
            IProductService productService,
            IProductCategoryService categoryService,
            IHospitalCenterRepository centerRepository,
            IApplicationLogger appLogger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _centerRepository = centerRepository;
            _appLogger = appLogger;
        }

        /// <summary>
        /// Tableau de bord principal des stocks
        /// Vue d'ensemble des stocks du centre actuel
        /// </summary>
        public async Task<IActionResult> Index(StockOverviewFilters? filters = null)
        {
            try
            {
                filters ??= new StockOverviewFilters();

                var overview = await _productService.GetStockOverviewAsync(CurrentCenterId!.Value, filters);

                // Ajouter les informations de contexte
                overview.CurrentUserRole = CurrentRole ?? "";

                // Log de l'accès
                await _appLogger.LogInfoAsync("Stock", "OverviewAccessed",
                    "Accès au tableau de bord des stocks",
                    CurrentUserId, CurrentCenterId);

                return View(overview);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "OverviewError",
                    "Erreur lors du chargement du tableau de bord des stocks",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du tableau de bord des stocks";
                return View(new StockOverviewViewModel { CurrentCenterId = CurrentCenterId!.Value });
            }
        }

        /// <summary>
        /// Configuration du stock initial (SuperAdmin uniquement)
        /// Permet de définir les quantités de départ pour chaque produit
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> InitialSetup()
        {
            try
            {
                var center = await _centerRepository.GetByIdAsync(CurrentCenterId!.Value);
                var products = await _productService.GetActiveProductsForSelectAsync();

                var model = new InitializeStockViewModel
                {
                    HospitalCenterId = CurrentCenterId!.Value,
                    HospitalCenterName = center?.Name ?? "Centre inconnu",
                    Products = products.Select(p => StockMappingService.MapToProductStockInitViewModel(
                        new Models.EfModels.Product
                        {
                            Id = p.Id,
                            Name = p.Name,
                            UnitOfMeasure = p.UnitOfMeasure,
                            ProductCategory = new Models.EfModels.ProductCategory { Name = p.CategoryName }
                        })).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "InitialSetupGetError",
                    "Erreur lors du chargement de la configuration initiale",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement de la configuration";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Traitement de la configuration du stock initial
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> InitialSetup(InitializeStockViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _productService.InitializeBulkStockAsync(model, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Stock initial configuré avec succès";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Ajouter les erreurs au ModelState
                    if (result.ValidationErrors.Any())
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            ModelState.AddModelError("", error);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", result.ErrorMessage ?? "Erreur lors de la configuration");
                    }

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "InitialSetupPostError",
                    "Erreur lors de la configuration du stock initial",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur inattendue s'est produite");
                return View(model);
            }
        }

        /// <summary>
        /// Initialisation rapide d'un produit via modal
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> QuickInitialize(int productId, decimal quantity, decimal? minThreshold, decimal? maxThreshold)
        {
            try
            {
                var result = await _productService.InitializeStockAsync(
                    productId,
                    CurrentCenterId!.Value,
                    quantity,
                    minThreshold,
                    maxThreshold,
                    CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Stock initialisé avec succès"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "Erreur lors de l'initialisation"
                    });
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "QuickInitializeError",
                    "Erreur lors de l'initialisation rapide",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = productId, Quantity = quantity, Error = ex.Message });

                return Json(new
                {
                    success = false,
                    message = "Une erreur inattendue s'est produite"
                });
            }
        }

        /// <summary>
        /// Ajustement manuel du stock (SuperAdmin uniquement)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> AdjustStock(int productId, decimal quantity, string reason)
        {
            try
            {
                var result = await _productService.AdjustStockAsync(
                    productId,
                    CurrentCenterId!.Value,
                    quantity,
                    reason,
                    CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Stock ajusté avec succès"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "Erreur lors de l'ajustement"
                    });
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "AdjustStockError",
                    "Erreur lors de l'ajustement du stock",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = productId, Quantity = quantity, Reason = reason, Error = ex.Message });

                return Json(new
                {
                    success = false,
                    message = "Une erreur inattendue s'est produite"
                });
            }
        }

        /// <summary>
        /// Mise à jour des seuils de stock
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> UpdateThresholds(int productId, decimal? minThreshold, decimal? maxThreshold)
        {
            try
            {
                var result = await _productService.UpdateStockThresholdsAsync(
                    productId,
                    CurrentCenterId!.Value,
                    minThreshold,
                    maxThreshold,
                    CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Seuils mis à jour avec succès"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "Erreur lors de la mise à jour"
                    });
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "UpdateThresholdsError",
                    "Erreur lors de la mise à jour des seuils",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = productId, MinThreshold = minThreshold, MaxThreshold = maxThreshold, Error = ex.Message });

                return Json(new
                {
                    success = false,
                    message = "Une erreur inattendue s'est produite"
                });
            }
        }

        /// <summary>
        /// Filtrage des alertes de stock
        /// </summary>
        public async Task<IActionResult> Alerts(string? severity = null)
        {
            try
            {
                List<StockAlertViewModel> alerts;

                if (severity == "critical")
                {
                    alerts = await _productService.GetCriticalStockProductsAsync(CurrentCenterId);
                }
                else if (severity == "low")
                {
                    alerts = await _productService.GetLowStockProductsAsync(CurrentCenterId);
                }
                else
                {
                    // Toutes les alertes
                    var criticalAlerts = await _productService.GetCriticalStockProductsAsync(CurrentCenterId);
                    var lowAlerts = await _productService.GetLowStockProductsAsync(CurrentCenterId);
                    alerts = criticalAlerts.Concat(lowAlerts).ToList();
                }

                return Json(new
                {
                    success = true,
                    alerts = alerts.Select(a => new {
                        productId = a.ProductId,
                        productName = a.ProductName,
                        category = a.CategoryName,
                        currentQuantity = a.QuantityText,
                        minThreshold = a.MinimumThreshold,
                        severity = a.SeverityText,
                        severityBadge = a.SeverityBadge,
                        lastMovement = a.LastMovementDate?.ToString("dd/MM/yyyy") ?? "Jamais",
                        message = a.AlertMessage
                    })
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "AlertsError",
                    "Erreur lors de la récupération des alertes",
                    CurrentUserId, CurrentCenterId,
                    details: new { Severity = severity, Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors du chargement des alertes" });
            }
        }

        /// <summary>
        /// Mouvements récents du stock
        /// </summary>
        public async Task<IActionResult> RecentMovements(int limit = 20)
        {
            try
            {
                var movements = await _productService.GetRecentMovementsAsync(CurrentCenterId!.Value, limit);

                return Json(new
                {
                    success = true,
                    movements = movements.Select(m => new {
                        date = m.MovementDate.ToString("dd/MM/yyyy"),
                        time = m.TimeText,
                        product = m.ProductName,
                        type = m.MovementTypeText,
                        quantity = m.QuantityText,
                        quantityClass = m.QuantityClass,
                        reference = m.ReferenceText,
                        createdBy = m.CreatedByName,
                        icon = m.MovementIcon
                    })
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "RecentMovementsError",
                    "Erreur lors de la récupération des mouvements récents",
                    CurrentUserId, CurrentCenterId,
                    details: new { Limit = limit, Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors du chargement des mouvements" });
            }
        }

        /// <summary>
        /// Statistiques de stock pour les graphiques
        /// </summary>
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var statistics = await _productService.GetProductStatisticsAsync(CurrentCenterId);

                return Json(new
                {
                    success = true,
                    statistics = new
                    {
                        totalProducts = statistics.TotalProducts,
                        activeProducts = statistics.ActiveProducts,
                        inactiveProducts = statistics.InactiveProducts,
                        lowStock = statistics.ProductsWithLowStock,
                        criticalStock = statistics.ProductsWithCriticalStock,
                        averagePrice = statistics.AveragePrice,
                        categoriesUsed = statistics.CategoriesUsed,
                        activePercentage = statistics.ActivePercentage,
                        lowStockPercentage = statistics.LowStockPercentage
                    }
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "StatisticsError",
                    "Erreur lors de la récupération des statistiques",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors du chargement des statistiques" });
            }
        }

        /// <summary>
        /// Recherche globale de produits avec stock
        /// </summary>
        public async Task<IActionResult> SearchWithStock(string term)
        {
            try
            {
                var products = await _productService.SearchProductsAsync(term ?? "", null, CurrentCenterId);

                return Json(new
                {
                    success = true,
                    products = products.Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        category = p.CategoryName,
                        unit = p.UnitOfMeasure,
                        price = p.PriceText,
                        displayText = p.DisplayText
                    })
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "SearchWithStockError",
                    "Erreur lors de la recherche de produits",
                    CurrentUserId, CurrentCenterId,
                    details: new { SearchTerm = term, Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la recherche" });
            }
        }

        /// <summary>
        /// Export du rapport de stock
        /// </summary>
        [SuperAdmin]
        public async Task<IActionResult> ExportStockReport()
        {
            try
            {
                var report = await _productService.GenerateStockReportAsync(CurrentCenterId!.Value);

                // Log de l'export
                await _appLogger.LogInfoAsync("Stock", "StockReportExported",
                    "Export du rapport de stock",
                    CurrentUserId, CurrentCenterId);

                // Générer le CSV du rapport (temporaire)
                var csv = GenerateStockReportCsv(report);
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

                return File(bytes, "text/csv", $"rapport_stock_{CurrentCenterName}_{DateTime.Now:yyyyMMdd_HHmm}.csv");
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "ExportStockReportError",
                    "Erreur lors de l'export du rapport de stock",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de l'export du rapport";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Génère un CSV du rapport de stock (temporaire)
        /// </summary>
        private string GenerateStockReportCsv(StockReportViewModel report)
        {
            var csv = new System.Text.StringBuilder();

            // En-tête du rapport
            csv.AppendLine($"Rapport de Stock - {report.CenterName}");
            csv.AppendLine($"Période: du {report.FromDate:dd/MM/yyyy} au {report.ToDate:dd/MM/yyyy}");
            csv.AppendLine($"Généré le: {report.GeneratedAt:dd/MM/yyyy HH:mm}");
            csv.AppendLine();

            // Résumé
            csv.AppendLine("=== RÉSUMÉ ===");
            csv.AppendLine($"Produits totaux,{report.Summary.TotalProducts}");
            csv.AppendLine($"Produits en stock,{report.Summary.ProductsInStock}");
            csv.AppendLine($"Produits en rupture,{report.Summary.ProductsOutOfStock}");
            csv.AppendLine($"Stock bas,{report.Summary.ProductsLowStock}");
            csv.AppendLine($"Stock critique,{report.Summary.ProductsCriticalStock}");
            csv.AppendLine($"Valeur totale,{report.Summary.TotalValue:N0} FCFA");
            csv.AppendLine();

            // Détails par produit
            csv.AppendLine("=== DÉTAIL PAR PRODUIT ===");
            csv.AppendLine("Produit,Catégorie,Quantité,Unité,Prix unitaire,Valeur totale,Statut,Seuil min,Seuil max,Mouvements");

            foreach (var item in report.Items)
            {
                csv.AppendLine($"\"{item.ProductName}\"," +
                              $"\"{item.CategoryName}\"," +
                              $"{item.Quantity:N2}," +
                              $"\"{item.UnitOfMeasure}\"," +
                              $"{item.UnitPrice:N0}," +
                              $"{item.TotalValue:N0}," +
                              $"{item.Status}," +
                              $"{item.MinThreshold:N0}," +
                              $"{item.MaxThreshold:N0}," +
                              $"{item.MovementsCount}");
            }

            return csv.ToString();
        }

        /// <summary>
        /// Page d'aide et documentation
        /// </summary>
        public IActionResult Help()
        {
            return View();
        }
    }
}