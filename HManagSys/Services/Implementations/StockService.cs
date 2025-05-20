using DocumentFormat.OpenXml.InkML;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations
{
    /// <summary>
    /// Implémentation du service de gestion des stocks
    /// </summary>
    public class StockService : IStockService
    {
        private readonly IGenericRepository<StockMovement> _stockMovementRepository;
        private readonly IGenericRepository<StockInventory> _stockInventoryRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<Prescription> _prescriptionRepository;
        private readonly IGenericRepository<PrescriptionItem> _prescriptionItemRepository;
        private readonly IGenericRepository<CareService> _careServiceRepository;
        private readonly IGenericRepository<CareServiceProduct> _careServiceProductRepository;
        private readonly IGenericRepository<HospitalCenter> _hospitalCenterRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IApplicationLogger _logger;
        private readonly IAuditService _auditService;

        public StockService(
            IGenericRepository<StockMovement> stockMovementRepository,
            IGenericRepository<StockInventory> stockInventoryRepository,
            IGenericRepository<Product> productRepository,
            IGenericRepository<Prescription> prescriptionRepository,
            IGenericRepository<PrescriptionItem> prescriptionItemRepository,
            IGenericRepository<CareService> careServiceRepository,
            IGenericRepository<CareServiceProduct> careServiceProductRepository,
            IGenericRepository<HospitalCenter> hospitalCenterRepository,
            IGenericRepository<User> userRepository,
            IApplicationLogger logger,
            IAuditService auditService)
        {
            _stockMovementRepository = stockMovementRepository;
            _stockInventoryRepository = stockInventoryRepository;
            _productRepository = productRepository;
            _prescriptionRepository = prescriptionRepository;
            _prescriptionItemRepository = prescriptionItemRepository;
            _careServiceRepository = careServiceRepository;
            _careServiceProductRepository = careServiceProductRepository;
            _hospitalCenterRepository = hospitalCenterRepository;
            _userRepository = userRepository;
            _logger = logger;
            _auditService = auditService;
        }






        /// <summary>
        /// Enregistre une dispensation de prescription avec décrément de stock
        /// </summary>
        public async Task<OperationResult<StockMovementTrackingViewModel>> RecordPrescriptionDispensationAsync(int prescriptionId, int userId)
        {
            try
            {
                // Vérifier que la prescription existe
                var prescription = await _prescriptionRepository.QuerySingleAsync(q =>
                    q.Where(p => p.Id == prescriptionId)
                     .Include(p => p.HospitalCenter)
                     .Include(p => p.Patient)
                     .Include(p => p.PrescriptionItems)
                     .ThenInclude(i => i.Product));

                if (prescription == null)
                {
                    return OperationResult<StockMovementTrackingViewModel>.Error("Prescription introuvable");
                }

                // Vérifier que la prescription n'est pas déjà dispensée
                if (prescription.Status == "Dispensed")
                {
                    return OperationResult<StockMovementTrackingViewModel>.Error("Prescription déjà dispensée");
                }

                // Vérifier si le stock est suffisant
                var (isAvailable, shortageItems) = await CheckPrescriptionStockAvailabilityAsync(
                    prescriptionId, prescription.HospitalCenterId);

                if (!isAvailable)
                {
                    // Créer le résultat avec les informations sur les ruptures
                    var trackingResult = new StockMovementTrackingViewModel
                    {
                        ShortageItems = shortageItems,
                        SourceOperation = new SourceOperationInfo
                        {
                            OperationType = "Prescription",
                            OperationId = prescriptionId,
                            ReferenceNumber = $"PRESC-{prescriptionId}",
                            OperationDate = TimeZoneHelper.GetCameroonTime(),
                            UserId = userId,
                            HospitalCenterId = prescription.HospitalCenterId,
                            HospitalCenterName = prescription.HospitalCenter.Name
                        }
                    };

                    // Audit de l'échec
                    await _auditService.LogActionAsync(
                        userId,
                        "STOCK_CHECK_FAIL",
                        "Prescription",
                        prescriptionId,
                        null,
                        new { ShortageItems = shortageItems },
                        $"Échec de dispensation - Stock insuffisant pour {shortageItems.Count} produit(s)"
                    );

                    return OperationResult<StockMovementTrackingViewModel>.Error(
                        "Stock insuffisant pour certains produits");
                }

                // Créer le ViewModel de suivi
                var result = new StockMovementTrackingViewModel
                {
                    SourceOperation = new SourceOperationInfo
                    {
                        OperationType = "Prescription",
                        OperationId = prescriptionId,
                        ReferenceNumber = $"PRESC-{prescriptionId}",
                        OperationDate = TimeZoneHelper.GetCameroonTime(),
                        UserId = userId,
                        UserName = (await _userRepository.GetByIdAsync(userId))?.FirstName + " " +
                                  (await _userRepository.GetByIdAsync(userId))?.LastName,
                        HospitalCenterId = prescription.HospitalCenterId,
                        HospitalCenterName = prescription.HospitalCenter.Name
                    }
                };

                // Pour chaque item de la prescription, décrémenter le stock
                foreach (var item in prescription.PrescriptionItems)
                {
                    // Trouver l'inventaire correspondant
                    var inventory = await _stockInventoryRepository.GetSingleAsync(q =>
                        q.Where(i => i.ProductId == item.ProductId && i.HospitalCenterId == prescription.HospitalCenterId));

                    if (inventory == null)
                    {
                        continue; // Ignorer les produits sans inventaire
                    }

                    // Calculer la nouvelle quantité
                    decimal newQuantity = inventory.CurrentQuantity - item.Quantity;

                    // Mettre à jour l'inventaire
                    inventory.CurrentQuantity = newQuantity;
                    inventory.ModifiedBy = userId;
                    inventory.ModifiedAt = TimeZoneHelper.GetCameroonTime();
                    await _stockInventoryRepository.UpdateAsync(inventory);

                    // Enregistrer le mouvement de stock
                    var movement = new StockMovement
                    {
                        ProductId = item.ProductId,
                        HospitalCenterId = prescription.HospitalCenterId,
                        MovementType = "Prescription",
                        Quantity = -item.Quantity, // Négatif car c'est une sortie
                        ReferenceType = "Prescription",
                        ReferenceId = prescriptionId,
                        Notes = $"Dispensation de prescription pour {prescription.Patient.FirstName} {prescription.Patient.LastName}",
                        MovementDate = TimeZoneHelper.GetCameroonTime(),
                        CreatedBy = userId,
                        CreatedAt = TimeZoneHelper.GetCameroonTime()
                    };

                    await _stockMovementRepository.AddAsync(movement);

                    // Ajouter au résultat
                    result.Movements.Add(new StockMovementResultItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product.Name,
                        UnitOfMeasure = item.Product.UnitOfMeasure,
                        Quantity = -item.Quantity,
                        NewStockLevel = newQuantity,
                        MovementType = "Prescription",
                        MovementDate = TimeZoneHelper.GetCameroonTime()
                    });
                }

                // Mettre à jour le statut de la prescription
                prescription.Status = "Dispensed";
                prescription.ModifiedBy = userId;
                prescription.ModifiedAt = TimeZoneHelper.GetCameroonTime();
                await _prescriptionRepository.UpdateAsync(prescription);

                // Audit
                await _auditService.LogActionAsync(
                    userId,
                    "STOCK_MOVEMENT",
                    "Prescription",
                    prescriptionId,
                    null,
                    new { MovementsCount = result.Movements.Count },
                    $"Dispensation de prescription avec {result.Movements.Count} mouvements de stock"
                );

                // Log
                await _logger.LogInfoAsync("StockService", "PrescriptionDispensed",
                    $"Prescription {prescriptionId} dispensée avec {result.Movements.Count} mouvements de stock",
                    userId,
                    prescription.HospitalCenterId);

                return OperationResult<StockMovementTrackingViewModel>.Success(result);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("StockService", "RecordPrescriptionDispensationError",
                    "Erreur lors de l'enregistrement de la dispensation",
                    userId,
                    null,
                    null,
                    null,
                    new { PrescriptionId = prescriptionId, Error = ex.Message });

                return OperationResult<StockMovementTrackingViewModel>.Error(
                    "Une erreur est survenue lors de la dispensation de la prescription");
            }
        }

        /// <summary>
        /// Enregistre l'utilisation des produits pour un service de soin
        /// </summary>
        public async Task<OperationResult<StockMovementTrackingViewModel>> RecordCareServiceProductUsageAsync(int careServiceId, int userId)
        {
            try
            {
                // Vérifier que le service de soin existe
                var careService = await _careServiceRepository.QuerySingleAsync(q =>
                    q.Where(cs => cs.Id == careServiceId)
                     .Include(cs => cs.CareEpisode)
                     .ThenInclude(ce => ce.Patient)
                     .Include(cs => cs.CareEpisode)
                     .ThenInclude(ce => ce.HospitalCenter)
                     .Include(cs => cs.CareServiceProducts)
                     .ThenInclude(csp => csp.Product));

                if (careService == null)
                {
                    return OperationResult<StockMovementTrackingViewModel>.Error("Service de soin introuvable");
                }

                // Vérifier s'il y a des produits associés
                if (careService.CareServiceProducts == null || !careService.CareServiceProducts.Any())
                {
                    // Pas de produits à décrémenter
                    return OperationResult<StockMovementTrackingViewModel>.Success(new StockMovementTrackingViewModel
                    {
                        SourceOperation = new SourceOperationInfo
                        {
                            OperationType = "CareService",
                            OperationId = careServiceId,
                            ReferenceNumber = $"CARE-{careServiceId}",
                            OperationDate = TimeZoneHelper.GetCameroonTime(),
                            UserId = userId,
                            HospitalCenterId = careService.CareEpisode.HospitalCenterId,
                            HospitalCenterName = careService.CareEpisode.HospitalCenter.Name
                        }
                    });
                }

                int hospitalCenterId = careService.CareEpisode.HospitalCenterId;

                // Convertir les produits pour la vérification de stock
                var careProducts = careService.CareServiceProducts.Select(csp => new CareServiceProductItemViewModel
                {
                    ProductId = csp.ProductId,
                    QuantityUsed = csp.QuantityUsed
                }).ToList();

                // Vérifier si le stock est suffisant
                var (isAvailable, shortageItems) = await CheckCareServiceStockAvailabilityAsync(
                    careProducts, hospitalCenterId);

                if (!isAvailable)
                {
                    // Créer le résultat avec les informations sur les ruptures
                    var trackingResult = new StockMovementTrackingViewModel
                    {
                        ShortageItems = shortageItems,
                        SourceOperation = new SourceOperationInfo
                        {
                            OperationType = "CareService",
                            OperationId = careServiceId,
                            ReferenceNumber = $"CARE-{careServiceId}",
                            OperationDate = TimeZoneHelper.GetCameroonTime(),
                            UserId = userId,
                            HospitalCenterId = hospitalCenterId,
                            HospitalCenterName = careService.CareEpisode.HospitalCenter.Name
                        }
                    };

                    // Audit de l'échec
                    await _auditService.LogActionAsync(
                        userId,
                        "STOCK_CHECK_FAIL",
                        "CareService",
                        careServiceId,
                        null,
                        new { ShortageItems = shortageItems },
                        $"Échec d'utilisation - Stock insuffisant pour {shortageItems.Count} produit(s)"
                    );

                    return OperationResult<StockMovementTrackingViewModel>.Error(
                        "Stock insuffisant pour certains produits");
                }

                // Créer le ViewModel de suivi
                var result = new StockMovementTrackingViewModel
                {
                    SourceOperation = new SourceOperationInfo
                    {
                        OperationType = "CareService",
                        OperationId = careServiceId,
                        ReferenceNumber = $"CARE-{careServiceId}",
                        OperationDate = TimeZoneHelper.GetCameroonTime(),
                        UserId = userId,
                        UserName = (await _userRepository.GetByIdAsync(userId))?.FirstName + " " +
                                  (await _userRepository.GetByIdAsync(userId))?.LastName,
                        HospitalCenterId = hospitalCenterId,
                        HospitalCenterName = careService.CareEpisode.HospitalCenter.Name
                    }
                };

                // Pour chaque produit utilisé, décrémenter le stock
                foreach (var product in careService.CareServiceProducts)
                {
                    // Trouver l'inventaire correspondant
                    var inventory = await _stockInventoryRepository.GetSingleAsync(q =>
                        q.Where(i => i.ProductId == product.ProductId && i.HospitalCenterId == hospitalCenterId));

                    if (inventory == null)
                    {
                        continue; // Ignorer les produits sans inventaire
                    }

                    // Calculer la nouvelle quantité
                    decimal newQuantity = inventory.CurrentQuantity - product.QuantityUsed;

                    // Mettre à jour l'inventaire
                    inventory.CurrentQuantity = newQuantity;
                    inventory.ModifiedBy = userId;
                    inventory.ModifiedAt = TimeZoneHelper.GetCameroonTime();
                    await _stockInventoryRepository.UpdateAsync(inventory);

                    // Enregistrer le mouvement de stock
                    var movement = new StockMovement
                    {
                        ProductId = product.ProductId,
                        HospitalCenterId = hospitalCenterId,
                        MovementType = "Care",
                        Quantity = -product.QuantityUsed, // Négatif car c'est une sortie
                        ReferenceType = "CareService",
                        ReferenceId = careServiceId,
                        Notes = $"Utilisation pour soin de {careService.CareEpisode.Patient.FirstName} {careService.CareEpisode.Patient.LastName}",
                        MovementDate = TimeZoneHelper.GetCameroonTime(),
                        CreatedBy = userId,
                        CreatedAt = TimeZoneHelper.GetCameroonTime()
                    };

                    await _stockMovementRepository.AddAsync(movement);

                    // Ajouter au résultat
                    result.Movements.Add(new StockMovementResultItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Product.Name,
                        UnitOfMeasure = product.Product.UnitOfMeasure,
                        Quantity = -product.QuantityUsed,
                        NewStockLevel = newQuantity,
                        MovementType = "Care",
                        MovementDate = TimeZoneHelper.GetCameroonTime()
                    });
                }

                // Audit
                await _auditService.LogActionAsync(
                    userId,
                    "STOCK_MOVEMENT",
                    "CareService",
                    careServiceId,
                    null,
                    new { MovementsCount = result.Movements.Count },
                    $"Utilisation de produits pour soin avec {result.Movements.Count} mouvements de stock"
                );

                // Log
                await _logger.LogInfoAsync("StockService", "CareServiceProductsUsed",
                    $"Utilisation de {result.Movements.Count} produits pour le service de soin {careServiceId}",
                    userId,
                    hospitalCenterId);

                return OperationResult<StockMovementTrackingViewModel>.Success(result);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("StockService", "RecordCareServiceProductUsageError",
                    "Erreur lors de l'enregistrement de l'utilisation des produits",
                    userId,
                    null,
                    ex.Message );

                return OperationResult<StockMovementTrackingViewModel>.Error(
                    "Une erreur est survenue lors de l'enregistrement de l'utilisation des produits");
            }
        }

        /// <summary>
        /// Vérifie si le stock est suffisant pour dispenser une prescription
        /// </summary>
        public async Task<(bool IsAvailable, List<StockShortageItem> ShortageItems)> CheckPrescriptionStockAvailabilityAsync(
            int prescriptionId, int hospitalCenterId)
        {
            try
            {
                var shortageItems = new List<StockShortageItem>();

                // Récupérer la prescription et ses items
                var prescription = await _prescriptionRepository.QuerySingleAsync(q =>
                    q.Where(p => p.Id == prescriptionId)
                     .Include(p => p.PrescriptionItems)
                     .ThenInclude(i => i.Product));

                if (prescription == null)
                {
                    return (false, shortageItems);
                }

                // Vérifier chaque produit
                foreach (var item in prescription.PrescriptionItems)
                {
                    // Récupérer le stock actuel
                    var inventory = await _stockInventoryRepository.GetSingleAsync(q =>
                        q.Where(i => i.ProductId == item.ProductId && i.HospitalCenterId == hospitalCenterId));

                    if (inventory == null || inventory.CurrentQuantity < item.Quantity)
                    {
                        // Ajouter à la liste des produits en rupture
                        shortageItems.Add(new StockShortageItem
                        {
                            ProductId = item.ProductId,
                            ProductName = item.Product.Name,
                            UnitOfMeasure = item.Product.UnitOfMeasure,
                            RequestedQuantity = item.Quantity,
                            AvailableQuantity = inventory?.CurrentQuantity ?? 0
                        });
                    }
                }

                return (shortageItems.Count == 0, shortageItems);
            }
            catch (Exception ex)
            {
                // En cas d'erreur, considérer que le stock n'est pas disponible pour sécurité
                await _logger.LogErrorAsync("StockService", "CheckPrescriptionStockAvailabilityError",
                    "Erreur lors de la vérification du stock",
                    details: new { PrescriptionId = prescriptionId, HospitalCenterId = hospitalCenterId, Error = ex.Message });

                return (false, new List<StockShortageItem>());
            }
        }

        /// <summary>
        /// Vérifie si le stock est suffisant pour les produits d'un service de soin
        /// </summary>
        public async Task<(bool IsAvailable, List<StockShortageItem> ShortageItems)> CheckCareServiceStockAvailabilityAsync(
            List<CareServiceProductItemViewModel> products, int hospitalCenterId)
        {
            try
            {
                var shortageItems = new List<StockShortageItem>();

                if (products == null || !products.Any())
                {
                    return (true, shortageItems);
                }

                // Vérifier chaque produit
                foreach (var product in products)
                {
                    if (!product.ProductId.HasValue || product.QuantityUsed <= 0)
                    {
                        continue;
                    }

                    // Récupérer le produit pour avoir son nom et son unité de mesure
                    var productInfo = await _productRepository.GetByIdAsync(product.ProductId.Value);
                    if (productInfo == null)
                    {
                        continue;
                    }

                    // Récupérer le stock actuel
                    var inventory = await _stockInventoryRepository.GetSingleAsync(q =>
                        q.Where(i => i.ProductId == product.ProductId && i.HospitalCenterId == hospitalCenterId));

                    if (inventory == null || inventory.CurrentQuantity < product.QuantityUsed)
                    {
                        // Ajouter à la liste des produits en rupture
                        shortageItems.Add(new StockShortageItem
                        {
                            ProductId = product.ProductId.Value,
                            ProductName = productInfo.Name,
                            UnitOfMeasure = productInfo.UnitOfMeasure,
                            RequestedQuantity = product.QuantityUsed,
                            AvailableQuantity = inventory?.CurrentQuantity ?? 0
                        });
                    }
                }

                return (shortageItems.Count == 0, shortageItems);
            }
            catch (Exception ex)
            {
                // En cas d'erreur, considérer que le stock n'est pas disponible pour sécurité
                await _logger.LogErrorAsync("StockService", "CheckCareServiceStockAvailabilityError",
                    "Erreur lors de la vérification du stock",
                    details: new { ProductsCount = products?.Count ?? 0, HospitalCenterId = hospitalCenterId, Error = ex.Message });

                return (false, new List<StockShortageItem>());
            }
        }

        public Task<OperationResult> RecordMovementAsync(StockMovementRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult> RecordBulkMovementsAsync(List<StockMovementRequest> movements)
        {
            throw new NotImplementedException();
        }

        public Task<(List<StockMovementViewModel> Movements, int TotalCount)> GetMovementHistoryAsync(StockMovementFilters filters)
        {
            throw new NotImplementedException();
        }

        public Task<List<StockMovementViewModel>> GetProductMovementsAsync(int productId, int? centerId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult> CheckStockThresholdsAsync(int? centerId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Récupère les alertes de stock détaillées pour un centre
        /// </summary>
        /// <param name="centerId">ID du centre (null pour tous les centres)</param>
        /// <param name="severity">Filtre de sévérité (Low, Critical, OutOfStock, etc.)</param>
        /// <returns>Liste des alertes de stock</returns>
        public async Task<List<StockAlertDetailViewModel>> GetStockAlertsAsync(int? centerId = null, string? severity = null)
        {
            try
            {
                // Récupérer tous les stocks avec les produits associés
                var inventories = await _stockInventoryRepository.QueryListAsync(query => {
                    var filtered = query
                        .Include(s => s.Product)
                        .Include(s => s.Product.ProductCategory)
                        .Include(s => s.HospitalCenter)
                        .AsQueryable();

                    if (centerId.HasValue)
                    {
                        filtered = filtered.Where(s => s.HospitalCenterId == centerId.Value);
                    }

                    return filtered;
                });

                // Filtrer pour trouver les stocks critiques
                var alerts = new List<StockAlertDetailViewModel>();

                foreach (var inventory in inventories)
                {
                    // Déterminer le statut du stock
                    string stockStatus;

                    if (inventory.CurrentQuantity <= 0)
                    {
                        stockStatus = "OutOfStock";
                    }
                    else if (inventory.MinimumThreshold.HasValue && inventory.CurrentQuantity <= inventory.MinimumThreshold.Value)
                    {
                        stockStatus = "Low";
                    }
                    else if (inventory.MinimumThreshold.HasValue && inventory.CurrentQuantity <= inventory.MinimumThreshold.Value * 1.5m)
                    {
                        stockStatus = "Warning";
                    }
                    else
                    {
                        // Stock normal, ignorer
                        continue;
                    }

                    // Filtrer par sévérité si spécifiée
                    if (!string.IsNullOrEmpty(severity) && stockStatus != severity)
                    {
                        continue;
                    }

                    // Récupérer le dernier mouvement pour ce stock
                    var lastMovement = await _stockMovementRepository.QuerySingleAsync(q =>
                        q.Where(m => m.ProductId == inventory.ProductId && m.HospitalCenterId == inventory.HospitalCenterId)
                         .OrderByDescending(m => m.MovementDate)
                         .Select(m => new { Date = m.MovementDate }));

                    // Créer l'alerte
                    var alert = new StockAlertDetailViewModel
                    {
                        ProductId = inventory.ProductId,
                        ProductName = inventory.Product.Name,
                        CategoryName = inventory.Product.ProductCategory?.Name ?? "Non catégorisé",
                        HospitalCenterId = inventory.HospitalCenterId,
                        HospitalCenterName = inventory.HospitalCenter.Name,
                        CurrentQuantity = inventory.CurrentQuantity,
                        MinimumThreshold = inventory.MinimumThreshold,
                        MaximumThreshold = inventory.MaximumThreshold,
                        Severity = stockStatus,
                        UnitOfMeasure = inventory.Product.UnitOfMeasure,
                        LastMovementDate = lastMovement?.Date
                    };

                    alerts.Add(alert);
                }

                // Trier les alertes par statut puis par quantité
                return alerts
                    .OrderBy(a => a.Severity == "OutOfStock" ? 0 : a.Severity == "Low" ? 1 : 2)
                    .ThenBy(a => a.CurrentQuantity)
                    .ToList();
            }
            catch (Exception ex)
            {
                //_logger.LogError("Erreur lors de la récupération des alertes de stock", "Stock", "GetStockAlerts",
                //    ex.Message,  new { CenterId = centerId, Severity = severity });
                return new List<StockAlertDetailViewModel>();
            }
        }

        public Task<OperationResult> MarkAlertAsHandledAsync(int alertId, int handledBy, string? notes = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> CalculateStockStatusAsync(int productId, int centerId)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult<InventorySession>> StartInventoryAsync(int centerId, int startedBy, List<int>? productIds = null)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult> RecordInventoryCountAsync(int inventorySessionId, int productId, decimal countedQuantity, int countedBy, string? notes = null)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult<InventoryResult>> FinalizeInventoryAsync(int inventorySessionId, int finalizedBy)
        {
            throw new NotImplementedException();
        }

        public Task<List<InventorySessionViewModel>> GetInventorySessionsAsync(int? centerId = null, bool? isCompleted = null)
        {
            throw new NotImplementedException();
        }

        public Task<StockValuationReportViewModel> GenerateValuationReportAsync(int? centerId = null, DateTime? asOfDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<List<StockTurnoverViewModel>> CalculateTurnoverRatesAsync(int centerId, int days = 30)
        {
            throw new NotImplementedException();
        }

        public Task<List<ConsumptionAnalysisViewModel>> AnalyzeConsumptionAsync(int centerId, int days = 30)
        {
            throw new NotImplementedException();
        }

        public Task<List<DemandForecastViewModel>> ForecastDemandAsync(int centerId, int daysAhead = 30)
        {
            throw new NotImplementedException();
        }

        public Task<List<ConsolidationOpportunityViewModel>> FindConsolidationOpportunitiesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<OptimalTransferSuggestionViewModel>> SuggestOptimalTransfersAsync(int fromCenterId, int toCenterId)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult> UpdateBulkThresholdsAsync(List<BulkThresholdUpdateRequest> updates, int modifiedBy)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult<int>> CleanupOldMovementsAsync(DateTime beforeDate, bool preserveAuditTrail = true)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult> RecalculateStockLevelsAsync(int centerId, int requestedBy)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult<StockSyncReport>> SynchronizeStockDataAsync(List<int> centerIds, int requestedBy)
        {
            throw new NotImplementedException();
        }

        public async Task<StockInventory?> QuerySingleAsync(Func<IQueryable<StockInventory>, IQueryable<StockInventory>> queryBuilder)
        {
            return await _stockInventoryRepository.QuerySingleAsync(queryBuilder);
        }

        public async Task<List<StockInventory>> QueryListAsync(Func<IQueryable<StockInventory>, IQueryable<StockInventory>> queryBuilder)
        {
            return await _stockInventoryRepository.QueryListAsync(queryBuilder);
        }

    }
}