using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations
{
    /// <summary>
    /// Service pour la gestion des transferts de stock entre centres
    /// </summary>
    public class TransferService : ITransferService
    {
        private readonly IGenericRepository<StockTransfer> _transferRepository;
        private readonly IGenericRepository<StockInventory> _stockInventoryRepository;
        private readonly IGenericRepository<StockMovement> _stockMovementRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<HospitalCenter> _hospitalCenterRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IApplicationLogger _appLogger;
        private readonly IAuditService _auditService;

        public TransferService(
            IGenericRepository<StockTransfer> transferRepository,
            IGenericRepository<StockInventory> stockInventoryRepository,
            IGenericRepository<StockMovement> stockMovementRepository,
            IGenericRepository<Product> productRepository,
            IGenericRepository<HospitalCenter> hospitalCenterRepository,
            IGenericRepository<User> userRepository,
            IApplicationLogger appLogger,
            IAuditService auditService)
        {
            _transferRepository = transferRepository;
            _stockInventoryRepository = stockInventoryRepository;
            _stockMovementRepository = stockMovementRepository;
            _productRepository = productRepository;
            _hospitalCenterRepository = hospitalCenterRepository;
            _userRepository = userRepository;
            _appLogger = appLogger;
            _auditService = auditService;
        }

        // ===== OPÉRATIONS DE DEMANDE =====

        public async Task<OperationResult<int>> RequestTransferAsync(TransferRequestViewModel model, int requestedBy)
        {
            try
            {
                // Validation
                var validation = await ValidateTransferRequestAsync(
                    model.ProductId,
                    model.FromHospitalCenterId,
                    model.ToHospitalCenterId,
                    model.Quantity);

                if (!validation.IsValid)
                {
                    return OperationResult<int>.ValidationError(validation.Errors);
                }

                // Créer l'entité
                var transfer = new StockTransfer
                {
                    ProductId = model.ProductId,
                    FromHospitalCenterId = model.FromHospitalCenterId,
                    ToHospitalCenterId = model.ToHospitalCenterId,
                    Quantity = model.Quantity,
                    TransferReason = model.TransferReason?.Trim(),
                    Status = "Requested",
                    RequestDate = TimeZoneHelper.GetCameroonTime(),
                    CreatedBy = requestedBy,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                // Créer la demande
                var createdTransfer = await _transferRepository.AddAsync(transfer);

                // Audit
                await _auditService.LogActionAsync(
                    requestedBy,
                    "TRANSFER_REQUEST",
                    "StockTransfer",
                    createdTransfer.Id,
                    null,
                    new
                    {
                        ProductId = model.ProductId,
                        FromCenterId = model.FromHospitalCenterId,
                        ToCenterId = model.ToHospitalCenterId,
                        Quantity = model.Quantity,
                        Reason = model.TransferReason
                    },
                    $"Demande de transfert créée pour le produit ID {model.ProductId} de centre {model.FromHospitalCenterId} vers {model.ToHospitalCenterId}"
                );

                // Log
                await _appLogger.LogInfoAsync("Stock", "TransferRequested",
                    $"Demande de transfert créée ID {createdTransfer.Id}",
                    requestedBy,
                    details: new
                    {
                        TransferId = createdTransfer.Id,
                        ProductId = model.ProductId,
                        FromCenterId = model.FromHospitalCenterId,
                        ToCenterId = model.ToHospitalCenterId,
                        Quantity = model.Quantity
                    });

                return OperationResult<int>.Success(createdTransfer.Id);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "RequestTransferError",
                    "Erreur lors de la création d'une demande de transfert",
                    requestedBy,
                    details: new { Model = model, Error = ex.Message });

                return OperationResult<int>.Error($"Erreur lors de la création de la demande : {ex.Message}");
            }
        }

        public async Task<List<TransferViewModel>> GetPendingTransfersAsync(int centerId, string? role = null)
        {
            try
            {
                var transfers = await _transferRepository.QueryListAsync(baseQuery =>
                {
                    var pendingStatuses = new[] { "Requested", "Pending", "Approved" };

                    // Base query with includes
                    baseQuery = 
                    baseQuery.Include(t => t.Product)
                            .ThenInclude(p => p.ProductCategory)
                            .Include(t => t.FromHospitalCenter)
                            .Include(t => t.ToHospitalCenter).AsNoTracking().AsSplitQuery()
                            .Where(t => pendingStatuses.Contains(t.Status));

                    // Filter by status
                    //baseQuery = baseQuery.Where(t => pendingStatuses.Contains(t.Status));

                    // Filter by center based on role
                    if (role == "SuperAdmin")
                    {
                        // SuperAdmin: voir les transferts impliquant ce centre
                        baseQuery = baseQuery.Where(t =>
                            t.FromHospitalCenterId == centerId || t.ToHospitalCenterId == centerId);
                    }
                    else
                    {
                        // Personnel médical: voir uniquement les transferts vers ce centre
                        baseQuery = baseQuery.Where(t => t.ToHospitalCenterId == centerId);
                    }

                    return baseQuery.OrderByDescending(t => t.RequestDate)
                        .Select(transfer =>
                                                                         new TransferViewModel
                                                                         {
                                                                             Id = transfer.Id,
                                                                             ProductId = transfer.ProductId,
                                                                             ProductName = transfer.Product.Name,
                                                                             ProductCategory = transfer.Product.ProductCategory.Name,
                                                                             FromHospitalCenterId = transfer.FromHospitalCenterId,
                                                                             FromHospitalCenterName = transfer.FromHospitalCenter.Name,
                                                                             ToHospitalCenterId = transfer.ToHospitalCenterId,
                                                                             ToHospitalCenterName = transfer.ToHospitalCenter.Name,
                                                                             Quantity = transfer.Quantity,
                                                                             UnitOfMeasure = transfer.Product.UnitOfMeasure,
                                                                             TransferReason = transfer.TransferReason,
                                                                             Status = transfer.Status,
                                                                             RequestDate = transfer.RequestDate,
                                                                             ApprovedDate = transfer.ApprovedDate,
                                                                             CompletedDate = transfer.CompletedDate,
                                                                             //RequestedByName = requestedByName,
                                                                             //ApprovedByName = approvedByName,
                                                                             //SourceStockQuantity = stockInventory
                                                                         });
                });

                return transfers.ToList();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetPendingTransfersError",
                    "Erreur lors de la récupération des transferts en attente",
                    details: new { CenterId = centerId, Role = role, Error = ex.Message });

                return new List<TransferViewModel>();
            }
        }

        public async Task<List<TransferViewModel>> GetTransfersForApprovalAsync(int centerId)
        {
            try
            {
                var transfers = await _transferRepository.QueryListAsync(query =>
                {
                    return query.Include(t => t.Product)
                        .ThenInclude(p => p.ProductCategory)
                        .Include(t => t.FromHospitalCenter)
                        .Include(t => t.ToHospitalCenter)
                        .Where(t => t.Status == "Requested" || t.Status == "Pending")
                        .Where(t => t.FromHospitalCenterId == centerId) // Seul le centre source peut approuver
                        .OrderByDescending(t => t.RequestDate)
                        .Select(transfer =>
                                                                         new TransferViewModel
                                                                         {
                                                                             Id = transfer.Id,
                                                                             ProductId = transfer.ProductId,
                                                                             ProductName = transfer.Product.Name,
                                                                             ProductCategory = transfer.Product.ProductCategory.Name,
                                                                             FromHospitalCenterId = transfer.FromHospitalCenterId,
                                                                             FromHospitalCenterName = transfer.FromHospitalCenter.Name,
                                                                             ToHospitalCenterId = transfer.ToHospitalCenterId,
                                                                             ToHospitalCenterName = transfer.ToHospitalCenter.Name,
                                                                             Quantity = transfer.Quantity,
                                                                             UnitOfMeasure = transfer.Product.UnitOfMeasure,
                                                                             TransferReason = transfer.TransferReason,
                                                                             Status = transfer.Status,
                                                                             RequestDate = transfer.RequestDate,
                                                                             ApprovedDate = transfer.ApprovedDate,
                                                                             CompletedDate = transfer.CompletedDate,
                                                                             //RequestedByName = requestedByName,
                                                                             //ApprovedByName = approvedByName,
                                                                             //SourceStockQuantity = stockInventory
                                                                         });
                });

                return transfers.ToList();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetTransfersForApprovalError",
                    "Erreur lors de la récupération des transferts pour approbation",
                    details: new { CenterId = centerId, Error = ex.Message });

                return new List<TransferViewModel>();
            }
        }

        public async Task<ValidationResult> ValidateTransferRequestAsync(int productId, int fromCenterId, int toCenterId, decimal quantity)
        {
            var errors = new List<string>();

            // Vérifier si le produit existe et est actif
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                errors.Add("Produit introuvable");
                return ValidationResult.Invalid(errors.ToArray());
            }

            if (!product.IsActive)
            {
                errors.Add("Ce produit n'est pas actif");
            }

            // Vérifier si les centres existent
            var fromCenter = await _hospitalCenterRepository.GetByIdAsync(fromCenterId);
            if (fromCenter == null)
            {
                errors.Add("Centre source introuvable");
            }

            var toCenter = await _hospitalCenterRepository.GetByIdAsync(toCenterId);
            if (toCenter == null)
            {
                errors.Add("Centre destination introuvable");
            }

            // Vérifier que les centres sont différents
            if (fromCenterId == toCenterId)
            {
                errors.Add("Les centres source et destination doivent être différents");
            }

            // Vérifier si la quantité est valide
            if (quantity <= 0)
            {
                errors.Add("La quantité doit être supérieure à 0");
            }

            // Vérifier si le stock est suffisant
            var availableQuantity = await GetAvailableQuantityAsync(productId, fromCenterId);
            if (quantity > availableQuantity)
            {
                errors.Add($"Stock insuffisant dans le centre source (disponible: {availableQuantity:N2})");
            }

            return errors.Any()
                ? ValidationResult.Invalid(errors.ToArray())
                : ValidationResult.Valid();
        }

        // ===== OPÉRATIONS D'APPROBATION =====

        public async Task<OperationResult> ApproveTransferAsync(int transferId, int approvedBy, string comments)
        {
            try
            {
                var transfer = await _transferRepository.GetByIdAsync(transferId);
                if (transfer == null)
                {
                    return OperationResult.Error("Transfert introuvable");
                }

                // Vérifier si le transfert peut être approuvé
                if (transfer.Status != "Requested" && transfer.Status != "Pending")
                {
                    return OperationResult.Error("Ce transfert ne peut pas être approuvé dans son statut actuel");
                }

                // Vérifier si l'utilisateur peut approuver
                var canApprove = await CanUserApproveTransferAsync(transferId, approvedBy);
                if (!canApprove)
                {
                    return OperationResult.Error("Vous n'avez pas les droits pour approuver ce transfert");
                }

                // Vérifier si le stock est toujours suffisant
                var availableQuantity = await GetAvailableQuantityAsync(transfer.ProductId, transfer.FromHospitalCenterId);
                if (transfer.Quantity > availableQuantity)
                {
                    return OperationResult.Error($"Stock insuffisant dans le centre source (disponible: {availableQuantity:N2})");
                }

                // Ancienne valeur pour l'audit
                var oldStatus = transfer.Status;

                // Mettre à jour le transfert
                transfer.Status = "Approved";
                transfer.ApprovedBy = approvedBy;
                transfer.ApprovedDate = TimeZoneHelper.GetCameroonTime();
                transfer.ModifiedBy = approvedBy;
                transfer.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _transferRepository.UpdateAsync(transfer);

                // Audit
                await _auditService.LogActionAsync(
                    approvedBy,
                    "TRANSFER_APPROVE",
                    "StockTransfer",
                    transferId,
                    new { Status = oldStatus },
                    new
                    {
                        Status = "Approved",
                        ApprovedBy = approvedBy,
                        ApprovedDate = transfer.ApprovedDate,
                        Comments = comments
                    },
                    $"Transfert ID {transferId} approuvé"
                );

                // Log
                await _appLogger.LogInfoAsync("Stock", "TransferApproved",
                    $"Transfert ID {transferId} approuvé",
                    approvedBy,
                    details: new { TransferId = transferId, Comments = comments });

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "ApproveTransferError",
                    $"Erreur lors de l'approbation du transfert {transferId}",
                    approvedBy,
                    details: new { TransferId = transferId, Comments = comments, Error = ex.Message });

                return OperationResult.Error($"Erreur lors de l'approbation : {ex.Message}");
            }
        }

        public async Task<OperationResult> RejectTransferAsync(int transferId, int rejectedBy, string reason)
        {
            try
            {
                var transfer = await _transferRepository.GetByIdAsync(transferId);
                if (transfer == null)
                {
                    return OperationResult.Error("Transfert introuvable");
                }

                // Vérifier si le transfert peut être rejeté
                if (transfer.Status == "Completed" || transfer.Status == "Rejected" || transfer.Status == "Cancelled")
                {
                    return OperationResult.Error("Ce transfert ne peut pas être rejeté dans son statut actuel");
                }

                // Vérifier si l'utilisateur peut rejeter
                var canApprove = await CanUserApproveTransferAsync(transferId, rejectedBy);
                if (!canApprove)
                {
                    return OperationResult.Error("Vous n'avez pas les droits pour rejeter ce transfert");
                }

                // Ancienne valeur pour l'audit
                var oldStatus = transfer.Status;

                // Mettre à jour le transfert
                transfer.Status = "Rejected";
                transfer.ModifiedBy = rejectedBy;
                transfer.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _transferRepository.UpdateAsync(transfer);

                // Audit
                await _auditService.LogActionAsync(
                    rejectedBy,
                    "TRANSFER_REJECT",
                    "StockTransfer",
                    transferId,
                    new { Status = oldStatus },
                    new { Status = "Rejected", Reason = reason },
                    $"Transfert ID {transferId} rejeté : {reason}"
                );

                // Log
                await _appLogger.LogInfoAsync("Stock", "TransferRejected",
                    $"Transfert ID {transferId} rejeté",
                    rejectedBy,
                    details: new { TransferId = transferId, Reason = reason });

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "RejectTransferError",
                    $"Erreur lors du rejet du transfert {transferId}",
                    rejectedBy,
                    details: new { TransferId = transferId, Reason = reason, Error = ex.Message });

                return OperationResult.Error($"Erreur lors du rejet : {ex.Message}");
            }
        }

        public async Task<OperationResult> CancelTransferAsync(int transferId, int cancelledBy, string reason)
        {
            try
            {
                var transfer = await _transferRepository.GetByIdAsync(transferId);
                if (transfer == null)
                {
                    return OperationResult.Error("Transfert introuvable");
                }

                // Vérifier si le transfert peut être annulé
                if (transfer.Status == "Completed" || transfer.Status == "Rejected" || transfer.Status == "Cancelled")
                {
                    return OperationResult.Error("Ce transfert ne peut pas être annulé dans son statut actuel");
                }

                // Ancienne valeur pour l'audit
                var oldStatus = transfer.Status;

                // Mettre à jour le transfert
                transfer.Status = "Cancelled";
                transfer.ModifiedBy = cancelledBy;
                transfer.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _transferRepository.UpdateAsync(transfer);

                // Audit
                await _auditService.LogActionAsync(
                    cancelledBy,
                    "TRANSFER_CANCEL",
                    "StockTransfer",
                    transferId,
                    new { Status = oldStatus },
                    new { Status = "Cancelled", Reason = reason },
                    $"Transfert ID {transferId} annulé : {reason}"
                );

                // Log
                await _appLogger.LogInfoAsync("Stock", "TransferCancelled",
                    $"Transfert ID {transferId} annulé",
                    cancelledBy,
                    details: new { TransferId = transferId, Reason = reason });

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "CancelTransferError",
                    $"Erreur lors de l'annulation du transfert {transferId}",
                    cancelledBy,
                    details: new { TransferId = transferId, Reason = reason, Error = ex.Message });

                return OperationResult.Error($"Erreur lors de l'annulation : {ex.Message}");
            }
        }

        // ===== OPÉRATIONS D'EXÉCUTION =====

        public async Task<OperationResult> CompleteTransferAsync(int transferId, int completedBy)
        {
            try
            {
                var transfer = await _transferRepository.GetSingleAsync(query => query
                    .Include(x=>x.FromHospitalCenter)
                    .Include(x => x.ToHospitalCenter)
                    .Where(x=>x.Id == transferId));
                if (transfer == null)
                {
                    return OperationResult.Error("Transfert introuvable");
                }

                // Vérifier si le transfert peut être complété
                if (transfer.Status != "Approved")
                {
                    return OperationResult.Error("Ce transfert doit être approuvé avant d'être complété");
                }

                // Utiliser une transaction pour assurer l'atomicité
                return await _transferRepository.TransactionAsync<OperationResult>(async () =>
                {
                    // 1. Vérifier si le stock est toujours suffisant
                    var availableQuantity = await GetAvailableQuantityAsync(transfer.ProductId, transfer.FromHospitalCenterId);
                    if (transfer.Quantity > availableQuantity)
                    {
                        return OperationResult.Error($"Stock insuffisant dans le centre source (disponible: {availableQuantity:N2})");
                    }

                    // 2. Diminuer le stock dans le centre source
                    var sourceStock = await _stockInventoryRepository.QuerySingleAsync<StockInventory>(q =>
                        q.Where(si => si.ProductId == transfer.ProductId && si.HospitalCenterId == transfer.FromHospitalCenterId));

                    if (sourceStock == null)
                    {
                        return OperationResult.Error("Stock source introuvable");
                    }

                    sourceStock.CurrentQuantity -= transfer.Quantity;
                    sourceStock.ModifiedBy = completedBy;
                    sourceStock.ModifiedAt = TimeZoneHelper.GetCameroonTime();
                    await _stockInventoryRepository.UpdateAsync(sourceStock);

                    // 3. Créer un mouvement de sortie dans le centre source
                    var sourceMovement = new StockMovement
                    {
                        ProductId = transfer.ProductId,
                        HospitalCenterId = transfer.FromHospitalCenterId,
                        MovementType = "Transfer",
                        Quantity = -transfer.Quantity, // Négatif car c'est une sortie
                        ReferenceType = "StockTransfer",
                        ReferenceId = transfer.Id,
                        Notes = $"Transfert vers {transfer.ToHospitalCenter.Name}",
                        MovementDate = TimeZoneHelper.GetCameroonTime(),
                        CreatedBy = completedBy,
                        CreatedAt = TimeZoneHelper.GetCameroonTime()
                    };

                    await _stockMovementRepository.AddAsync(sourceMovement);

                    // 4. Augmenter le stock dans le centre destination
                    var destinationStock = await _stockInventoryRepository.QuerySingleAsync<StockInventory>(q =>
                        q.Where(si => si.ProductId == transfer.ProductId && si.HospitalCenterId == transfer.ToHospitalCenterId));

                    if (destinationStock == null)
                    {
                        // Créer un stock initial si n'existe pas encore
                        destinationStock = new StockInventory
                        {
                            ProductId = transfer.ProductId,
                            HospitalCenterId = transfer.ToHospitalCenterId,
                            CurrentQuantity = transfer.Quantity,
                            CreatedBy = completedBy,
                            CreatedAt = TimeZoneHelper.GetCameroonTime()
                        };

                        await _stockInventoryRepository.AddAsync(destinationStock);
                    }
                    else
                    {
                        destinationStock.CurrentQuantity += transfer.Quantity;
                        destinationStock.ModifiedBy = completedBy;
                        destinationStock.ModifiedAt = TimeZoneHelper.GetCameroonTime();
                        await _stockInventoryRepository.UpdateAsync(destinationStock);
                    }

                    // 5. Créer un mouvement d'entrée dans le centre destination
                    var destinationMovement = new StockMovement
                    {
                        ProductId = transfer.ProductId,
                        HospitalCenterId = transfer.ToHospitalCenterId,
                        MovementType = "Transfer",
                        Quantity = transfer.Quantity, // Positif car c'est une entrée
                        ReferenceType = "StockTransfer",
                        ReferenceId = transfer.Id,
                        Notes = $"Transfert depuis {transfer.FromHospitalCenter.Name}",
                        MovementDate = TimeZoneHelper.GetCameroonTime(),
                        CreatedBy = completedBy,
                        CreatedAt = TimeZoneHelper.GetCameroonTime()
                    };

                    await _stockMovementRepository.AddAsync(destinationMovement);

                    // 6. Mettre à jour le statut du transfert
                    var oldStatus = transfer.Status;
                    transfer.Status = "Completed";
                    transfer.CompletedDate = TimeZoneHelper.GetCameroonTime();
                    transfer.ModifiedBy = completedBy;
                    transfer.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                    await _transferRepository.UpdateAsync(transfer);

                    // 7. Audit
                    await _auditService.LogActionAsync(
                        completedBy,
                        "TRANSFER_COMPLETE",
                        "StockTransfer",
                        transferId,
                        new { Status = oldStatus },
                        new
                        {
                            Status = "Completed",
                            CompletedDate = transfer.CompletedDate,
                            SourceMovementId = sourceMovement.Id,
                            DestinationMovementId = destinationMovement.Id
                        },
                        $"Transfert ID {transferId} complété"
                    );

                    // 8. Log
                    await _appLogger.LogInfoAsync("Stock", "TransferCompleted",
                        $"Transfert ID {transferId} complété",
                        completedBy,
                        details: new
                        {
                            TransferId = transferId,
                            ProductId = transfer.ProductId,
                            Quantity = transfer.Quantity,
                            FromCenterId = transfer.FromHospitalCenterId,
                            ToCenterId = transfer.ToHospitalCenterId,
                            SourceMovementId = sourceMovement.Id,
                            DestinationMovementId = destinationMovement.Id
                        });

                    return OperationResult.Success();
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "CompleteTransferError",
                    $"Erreur lors de la complétion du transfert {transferId}",
                    completedBy,
                    details: new { TransferId = transferId, Error = ex.Message });

                return OperationResult.Error($"Erreur lors de la complétion : {ex.Message}");
            }
        }

        // ===== OPÉRATIONS DE CONSULTATION =====

        public async Task<TransferViewModel?> GetTransferByIdAsync(int id)
        {
            try
            {
                return await _transferRepository.QuerySingleAsync(query =>
                {
                    return query.Include(t => t.Product)
                        .ThenInclude(p => p.ProductCategory)
                        .Include(t => t.FromHospitalCenter)
                        .Include(t => t.ToHospitalCenter)
                        .Include(t => t.ApprovedByNavigation)
                        .Where(t => t.Id == id)
                        .Select(transfer =>
                                                 new TransferViewModel
                                                 {
                                                     Id = transfer.Id,
                                                     ProductId = transfer.ProductId,
                                                     ProductName = transfer.Product.Name,
                                                     ProductCategory = transfer.Product.ProductCategory.Name,
                                                     FromHospitalCenterId = transfer.FromHospitalCenterId,
                                                     FromHospitalCenterName = transfer.FromHospitalCenter.Name,
                                                     ToHospitalCenterId = transfer.ToHospitalCenterId,
                                                     ToHospitalCenterName = transfer.ToHospitalCenter.Name,
                                                     Quantity = transfer.Quantity,
                                                     UnitOfMeasure = transfer.Product.UnitOfMeasure,
                                                     TransferReason = transfer.TransferReason,
                                                     Status = transfer.Status,
                                                     RequestDate = transfer.RequestDate,
                                                     ApprovedDate = transfer.ApprovedDate,
                                                     CompletedDate = transfer.CompletedDate,
                                                     //RequestedByName = requestedByName,
                                                     //ApprovedByName = approvedByName,
                                                     //SourceStockQuantity = stockInventory
                                                 }
                        );
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetTransferByIdError",
                    $"Erreur lors de la récupération du transfert {id}",
                    details: new { TransferId = id, Error = ex.Message });

                return null;
            }
        }

        internal class TransferResponse
        {
            public List<TransferViewModel> Transfers { get; set; }
            public int TotalCount { get; set; }
        }


        public async Task<(List<TransferViewModel> Transfers, int TotalCount)> GetTransfersAsync(
            TransferFilters filters,
            int? currentCenterId = null,
            int? currentUserId = null)
        {
            try
            {
                int TotalCount = 0;
                var transfers = await _transferRepository.QueryListAsync(baseQuery =>
                {
                     baseQuery = baseQuery.Include(t => t.Product)
                        .ThenInclude(p => p.ProductCategory)
                        .Include(t => t.FromHospitalCenter)
                        .Include(t => t.ToHospitalCenter)
                        .Include(t => t.ApprovedByNavigation)
                        .AsQueryable();

                    // Appliquer les filtres
                    if (!string.IsNullOrEmpty(filters.Status))
                    {
                        baseQuery = baseQuery.Where(t => t.Status == filters.Status);
                    }

                    if (filters.FromCenterId.HasValue)
                    {
                        baseQuery = baseQuery.Where(t => t.FromHospitalCenterId == filters.FromCenterId.Value);
                    }

                    if (filters.ToCenterId.HasValue)
                    {
                        baseQuery = baseQuery.Where(t => t.ToHospitalCenterId == filters.ToCenterId.Value);
                    }

                    if (filters.ProductId.HasValue)
                    {
                        baseQuery = baseQuery.Where(t => t.ProductId == filters.ProductId.Value);
                    }

                    if (filters.Days > 0)
                    {
                        var cutoffDate = DateTime.Now.AddDays(-filters.Days);
                        baseQuery = baseQuery.Where(t => t.RequestDate >= cutoffDate);
                    }

                    if (filters.OnlyMyRequests && currentUserId.HasValue)
                    {
                        baseQuery = baseQuery.Where(t => t.CreatedBy == currentUserId.Value);
                    }

                    // Limiter aux transferts impliquant le centre courant si spécifié
                    if (currentCenterId.HasValue)
                    {
                        baseQuery = baseQuery.Where(t =>
                            t.FromHospitalCenterId == currentCenterId.Value ||
                            t.ToHospitalCenterId == currentCenterId.Value);
                    }

                    // Calculer le nombre total
                    TotalCount = baseQuery.Count();

                    // Appliquer la pagination
                    var pagedQuery = baseQuery
                        .OrderByDescending(t => t.RequestDate)
                        .Skip((filters.PageIndex - 1) * filters.PageSize)
                        .Take(filters.PageSize);

                    // Mapper aux ViewModels
                   // var transfers = pagedQuery.Select(t => MapToTransferViewModel(t));

                    return pagedQuery.Select(transfer =>
                    
                         new TransferViewModel
                        {
                            Id = transfer.Id,
                            ProductId = transfer.ProductId,
                            ProductName = transfer.Product.Name,
                            ProductCategory = transfer.Product.ProductCategory.Name,
                            FromHospitalCenterId = transfer.FromHospitalCenterId,
                            FromHospitalCenterName = transfer.FromHospitalCenter.Name,
                            ToHospitalCenterId = transfer.ToHospitalCenterId,
                            ToHospitalCenterName = transfer.ToHospitalCenter.Name,
                            Quantity = transfer.Quantity,
                            UnitOfMeasure = transfer.Product.UnitOfMeasure,
                            TransferReason = transfer.TransferReason,
                            Status = transfer.Status,
                            RequestDate = transfer.RequestDate,
                            ApprovedDate = transfer.ApprovedDate,
                            CompletedDate = transfer.CompletedDate,
                            //RequestedByName = requestedByName,
                            //ApprovedByName = approvedByName,
                            //SourceStockQuantity = stockInventory
                        }
                    );



                    //return new TransferResponse { Transfers = transfers.ToList(), TotalCount = totalCount };
                });

                return (transfers, TotalCount);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetTransfersAsyncError",
                    "Erreur lors de la récupération des transferts",
                    details: new
                    {
                        Filters = filters,
                        CurrentCenterId = currentCenterId,
                        CurrentUserId = currentUserId,
                        Error = ex.Message
                    });

                return (new List<TransferViewModel>(), 0);
            }
        }

        public async Task<TransferStatisticsViewModel> GetTransferStatisticsAsync(int? centerId = null)
        {
            try
            {
                // Requête de base pour filtrer par centre si nécessaire
                var baseQuery = _transferRepository.QueryListAsync(query =>
                {
                    var q = query.AsQueryable();

                    if (centerId.HasValue)
                    {
                        q = q.Where(t =>
                            t.FromHospitalCenterId == centerId.Value ||
                            t.ToHospitalCenterId == centerId.Value);
                    }

                    return q;
                }).Result;

                // Créer les statistiques
                var stats = new TransferStatisticsViewModel
                {
                    TotalTransfers = baseQuery.Count,
                    PendingTransfers = baseQuery.Count(t => t.Status == "Pending" || t.Status == "Requested"),
                    ApprovedTransfers = baseQuery.Count(t => t.Status == "Approved"),
                    CompletedTransfers = baseQuery.Count(t => t.Status == "Completed"),
                    RejectedTransfers = baseQuery.Count(t => t.Status == "Rejected"),
                    CancelledTransfers = baseQuery.Count(t => t.Status == "Cancelled")
                };

                // Top produits transférés
                stats.TopTransferredProducts = await _transferRepository.QueryListAsync(query =>
                {
                    var q = query.Include(t => t.Product)
                        .Where(t => t.Status == "Completed");

                    if (centerId.HasValue)
                    {
                        q = q.Where(t =>
                            t.FromHospitalCenterId == centerId.Value ||
                            t.ToHospitalCenterId == centerId.Value);
                    }

                    return q.GroupBy(t => new { t.ProductId, t.Product.Name, t.Product.UnitOfMeasure })
                        .Select(g => new ProductTransferCount
                        {
                            ProductId = g.Key.ProductId,
                            ProductName = g.Key.Name,
                            UnitOfMeasure = g.Key.UnitOfMeasure,
                            TransferCount = g.Count(),
                            TotalQuantity = g.Sum(t => t.Quantity)
                        })
                        .OrderByDescending(g => g.TransferCount)
                        .Take(5);
                });

                // Top centres sources
                stats.TopSourceCenters = await _transferRepository.QueryListAsync(query =>
                {
                    var q = query.Include(t => t.FromHospitalCenter)
                        .Where(t => t.Status == "Completed");

                    if (centerId.HasValue)
                    {
                        q = q.Where(t =>
                            t.FromHospitalCenterId == centerId.Value ||
                            t.ToHospitalCenterId == centerId.Value);
                    }

                    return q.GroupBy(t => new { t.FromHospitalCenterId, t.FromHospitalCenter.Name })
                        .Select(g => new CenterTransferCount
                        {
                            CenterId = g.Key.FromHospitalCenterId,
                            CenterName = g.Key.Name,
                            TransferCount = g.Count()
                        })
                        .OrderByDescending(g => g.TransferCount)
                        .Take(5);
                });

                // Top centres destinations
                stats.TopDestinationCenters = await _transferRepository.QueryListAsync(query =>
                {
                    var q = query.Include(t => t.ToHospitalCenter)
                        .Where(t => t.Status == "Completed");

                    if (centerId.HasValue)
                    {
                        q = q.Where(t =>
                            t.FromHospitalCenterId == centerId.Value ||
                            t.ToHospitalCenterId == centerId.Value);
                    }

                    return q.GroupBy(t => new { t.ToHospitalCenterId, t.ToHospitalCenter.Name })
                        .Select(g => new CenterTransferCount
                        {
                            CenterId = g.Key.ToHospitalCenterId,
                            CenterName = g.Key.Name,
                            TransferCount = g.Count()
                        })
                        .OrderByDescending(g => g.TransferCount)
                        .Take(5);
                });

                return stats;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetTransferStatisticsError",
                    "Erreur lors de la récupération des statistiques de transfert",
                    details: new { CenterId = centerId, Error = ex.Message });

                return new TransferStatisticsViewModel();
            }
        }

        // ===== OPÉRATIONS DE SUPPORT =====

        public async Task<bool> CanUserApproveTransferAsync(int transferId, int userId)
        {
            try
            {
                return true;
                //var transfer = await _transferRepository.GetByIdAsync(transferId);
                //if (transfer == null)
                //    return false;

                //// Vérifier si l'utilisateur est assigné au centre source avec un rôle SuperAdmin
                //var userAssignments = await _transferRepository.QueryListAsync<UserCenterAssignment>(query =>
                //{
                //    return query.Where(uca =>
                //        uca.UserId == userId &&
                //        uca.HospitalCenterId == transfer.FromHospitalCenterId &&
                //        uca.RoleType == "SuperAdmin" &&
                //        uca.IsActive);
                //});

                //return userAssignments.Any();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "CanUserApproveTransferError",
                    "Erreur lors de la vérification des droits d'approbation",
                    details: new { TransferId = transferId, UserId = userId, Error = ex.Message });

                return false;
            }
        }

        public async Task<List<SelectOption>> GetAvailableCentersForTransferAsync(int? excludeCenterId = null)
        {
            try
            {
                return await _hospitalCenterRepository.QueryListAsync(query =>
                {
                    var q = query.Where(c => c.IsActive);

                    if (excludeCenterId.HasValue)
                    {
                        q = q.Where(c => c.Id != excludeCenterId.Value);
                    }

                    return q.OrderBy(c => c.Name)
                        .Select(c => new SelectOption(
                            c.Id.ToString(),
                            c.Name
                        ));
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetAvailableCentersForTransferError",
                    "Erreur lors de la récupération des centres disponibles",
                    details: new { ExcludeCenterId = excludeCenterId, Error = ex.Message });

                return new List<SelectOption>();
            }
        }

        public async Task<List<SelectOption>> GetAvailableProductsForTransferAsync(int fromCenterId)
        {
            try
            {
                return await _stockInventoryRepository.QueryListAsync(query =>
                {
                    return query.Include(si => si.Product)
                        .ThenInclude(p => p.ProductCategory)
                        .Where(si => si.HospitalCenterId == fromCenterId)
                        .Where(si => si.CurrentQuantity > 0)
                        .Where(si => si.Product.IsActive)
                        .OrderBy(si => si.Product.ProductCategory.Name)
                        .ThenBy(si => si.Product.Name)
                        .Select(si => new SelectOption(
                            si.ProductId.ToString(),
                            $"{si.Product.Name} ({si.Product.UnitOfMeasure}) - {si.CurrentQuantity:N2} disponibles"
                        ));
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetAvailableProductsForTransferError",
                    "Erreur lors de la récupération des produits disponibles",
                    details: new { FromCenterId = fromCenterId, Error = ex.Message });

                return new List<SelectOption>();
            }
        }

        public async Task<decimal> GetAvailableQuantityAsync(int productId, int centerId)
        {
            try
            {
                var stockInventory = await _stockInventoryRepository.QuerySingleAsync<StockInventory>(q =>
                    q.Where(si => si.ProductId == productId && si.HospitalCenterId == centerId));

                return stockInventory?.CurrentQuantity ?? 0;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetAvailableQuantityError",
                    "Erreur lors de la récupération de la quantité disponible",
                    details: new { ProductId = productId, CenterId = centerId, Error = ex.Message });

                return 0;
            }
        }

        // ===== MÉTHODES UTILITAIRES PRIVÉES =====

        /// <summary>
        /// Mappe un StockTransfer à un TransferViewModel
        /// </summary>
        //private IQueryable<TransferViewModel> MapToTransferViewModel(StockTransfer transfer)
        //{
        //    return _stockInventoryRepository.QueryListAsync(stockQuery =>
        //    {
        //        var stockInventory = stockQuery
        //            .Where(si => si.ProductId == transfer.ProductId && si.HospitalCenterId == transfer.FromHospitalCenterId)
        //            .Select(si => si.CurrentQuantity)
        //            .FirstOrDefault();

        //        return _userRepository.QueryListAsync(userQuery =>
        //        {
        //            var requestedByName = userQuery
        //                .Where(u => u.Id == transfer.CreatedBy)
        //                .Select(u => $"{u.FirstName} {u.LastName}")
        //                .FirstOrDefault() ?? "Inconnu";

        //            var approvedByName = userQuery
        //                .Where(u => u.Id == transfer.ApprovedBy)
        //                .Select(u => $"{u.FirstName} {u.LastName}")
        //                .FirstOrDefault();

        //            return new[] { new TransferViewModel
        //            {
        //                Id = transfer.Id,
        //                ProductId = transfer.ProductId,
        //                ProductName = transfer.Product.Name,
        //                ProductCategory = transfer.Product.ProductCategory.Name,
        //                FromHospitalCenterId = transfer.FromHospitalCenterId,
        //                FromHospitalCenterName = transfer.FromHospitalCenter.Name,
        //                ToHospitalCenterId = transfer.ToHospitalCenterId,
        //                ToHospitalCenterName = transfer.ToHospitalCenter.Name,
        //                Quantity = transfer.Quantity,
        //                UnitOfMeasure = transfer.Product.UnitOfMeasure,
        //                TransferReason = transfer.TransferReason,
        //                Status = transfer.Status,
        //                RequestDate = transfer.RequestDate,
        //                ApprovedDate = transfer.ApprovedDate,
        //                CompletedDate = transfer.CompletedDate,
        //                RequestedByName = requestedByName,
        //                ApprovedByName = approvedByName,
        //                SourceStockQuantity = stockInventory
        //            } });
        //        });
        //    });
        //}
    }
}