using HManagSys.Controllers;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Payments;
using HManagSys.Models.ViewModels.Sales;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Documents;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace HManagSys.Services.Implementations
{
    /// <summary>
    /// Service pour la gestion des ventes
    /// Implémentation avec logique métier complète et gestion de stock intégrée
    /// </summary>
    public class SaleService : ISaleService
    {
        private readonly IGenericRepository<Sale> _saleRepository;
        private readonly IGenericRepository<SaleItem> _saleItemRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<StockInventory> _stockInventoryRepository;
        private readonly IGenericRepository<StockMovement> _stockMovementRepository;
        private readonly IGenericRepository<Patient> _patientRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<HospitalCenter> _hospitalCenterRepository;
        private readonly IPaymentService _paymentService;
        private readonly IApplicationLogger _logger;
        private readonly IAuditService _auditService;

        public SaleService(
            IGenericRepository<Sale> saleRepository,
            IGenericRepository<SaleItem> saleItemRepository,
            IGenericRepository<Product> productRepository,
            IGenericRepository<StockInventory> stockInventoryRepository,
            IGenericRepository<StockMovement> stockMovementRepository,
            IGenericRepository<Patient> patientRepository,
            IGenericRepository<User> userRepository,
            IGenericRepository<HospitalCenter> hospitalCenterRepository,
            IPaymentService paymentService,
            IApplicationLogger logger,
            IAuditService auditService)
        {
            _saleRepository = saleRepository;
            _saleItemRepository = saleItemRepository;
            _productRepository = productRepository;
            _stockInventoryRepository = stockInventoryRepository;
            _stockMovementRepository = stockMovementRepository;
            _patientRepository = patientRepository;
            _userRepository = userRepository;
            _hospitalCenterRepository = hospitalCenterRepository;
            _paymentService = paymentService;
            _logger = logger;
            _auditService = auditService;
        }

        // ===== OPÉRATIONS CRUD =====

        /// <summary>
        /// Récupère une vente par son ID avec toutes les informations associées
        /// </summary>
        public async Task<SaleViewModel?> GetByIdAsync(int id)
        {
            try
            {
                var sale = await _saleRepository.QuerySingleAsync<SaleViewModel>(q =>
                    q.Where(s => s.Id == id)
                     .Include(s => s.SaleItems).ThenInclude(si => si.Product).ThenInclude(p => p.ProductCategory)
                     .Include(s => s.Patient)
                     .Include(s => s.HospitalCenter)
                     .Include(s => s.SoldByNavigation)
                     .Select(s => new SaleViewModel
                     {
                         Id = s.Id,
                         SaleNumber = s.SaleNumber,
                         PatientId = s.PatientId,
                         PatientName = s.Patient != null ? $"{s.Patient.FirstName} {s.Patient.LastName}" : null,
                         HospitalCenterId = s.HospitalCenterId,
                         HospitalCenterName = s.HospitalCenter.Name,
                         SoldBy = s.SoldBy,
                         SoldByName = $"{s.SoldByNavigation.FirstName} {s.SoldByNavigation.LastName}",
                         SaleDate = s.SaleDate,
                         TotalAmount = s.TotalAmount,
                         DiscountAmount = s.DiscountAmount,
                         FinalAmount = s.FinalAmount,
                         PaymentStatus = s.PaymentStatus,
                         Notes = s.Notes,
                         IsCancelled = s.Notes != null && s.Notes.StartsWith("[CANCELLED]"),
                         CancellationReason = s.Notes != null && s.Notes.StartsWith("[CANCELLED]")
                            ? s.Notes.Replace("[CANCELLED]", "").Trim()
                            : null,
                         CreatedAt = s.CreatedAt,
                         Items = s.SaleItems.Select(si => new SaleItemViewModel
                         {
                             Id = si.Id,
                             SaleId = si.SaleId,
                             ProductId = si.ProductId,
                             ProductName = si.Product.Name,
                             CategoryName = si.Product.ProductCategory.Name,
                             UnitOfMeasure = si.Product.UnitOfMeasure,
                             Quantity = si.Quantity,
                             UnitPrice = si.UnitPrice,
                             TotalPrice = si.TotalPrice
                         }).ToList()
                     }));

                if (sale != null)
                {
                    // Charger les paiements associés
                    var payments = await _paymentService.GetPaymentsByReferenceAsync("Sale", id);
                    sale.Payments = payments;
                }

                return sale;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "GetByIdAsync",
                    $"Erreur lors de la récupération de la vente {id}",
                    details: new { SaleId = id, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère les ventes avec pagination et filtres
        /// </summary>
        public async Task<(List<SaleViewModel> Sales, int TotalCount)> GetSalesAsync(SaleFilters filters)
        {
            try
            {
                // Requête de base
                var query = await _saleRepository.QueryListAsync<SaleViewModel>(q =>
                {
                    var baseQuery = q.Include(s => s.Patient)
                                     .Include(s => s.HospitalCenter)
                                     .Include(s => s.SoldByNavigation)
                                     .Include(s => s.SaleItems)
                                     .AsQueryable();

                    // Appliquer les filtres
                    if (filters.HospitalCenterId.HasValue)
                        baseQuery = baseQuery.Where(s => s.HospitalCenterId == filters.HospitalCenterId.Value);


                    if (filters.PatientId.HasValue)
                        baseQuery = baseQuery.Where(s => s.PatientId == filters.PatientId.Value);


                    if (filters.SoldBy.HasValue)
                        baseQuery = baseQuery.Where(s => s.SoldBy == filters.SoldBy.Value);


                    if (!string.IsNullOrWhiteSpace(filters.PaymentStatus))
                        baseQuery = baseQuery.Where(s => s.PaymentStatus == filters.PaymentStatus);


                    if (filters.FromDate.HasValue)
                    {
                        var fromDate = filters.FromDate.Value.Date;
                        baseQuery = baseQuery.Where(s => s.SaleDate >= fromDate);
                    }

                    if (filters.ToDate.HasValue)
                    {
                        var toDate = filters.ToDate.Value.Date.AddDays(1).AddMilliseconds(-1);
                        baseQuery = baseQuery.Where(s => s.SaleDate <= toDate);
                    }

                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var searchTerm = filters.SearchTerm.ToLower();
                        baseQuery = baseQuery.Where(s =>
                            s.SaleNumber.ToLower().Contains(searchTerm) ||
                            (s.Patient != null && (s.Patient.FirstName.ToLower().Contains(searchTerm) ||
                                              s.Patient.LastName.ToLower().Contains(searchTerm))) ||
                            (s.Notes != null && s.Notes.ToLower().Contains(searchTerm))
                        );
                    }

                    // Comptage total
                    //int totalCount = baseQuery.Count();

                    // Tri et pagination
                    var salesQuery = baseQuery
                        .OrderByDescending(s => s.SaleDate)
                        .Skip((filters.PageIndex - 1) * filters.PageSize)
                        .Take(filters.PageSize)
                        .Select(s => new SaleViewModel
                        {
                            Id = s.Id,
                            SaleNumber = s.SaleNumber,
                            PatientId = s.PatientId,
                            PatientName = s.Patient != null ? $"{s.Patient.FirstName} {s.Patient.LastName}" : null,
                            HospitalCenterId = s.HospitalCenterId,
                            HospitalCenterName = s.HospitalCenter.Name,
                            SoldBy = s.SoldBy,
                            SoldByName = $"{s.SoldByNavigation.FirstName} {s.SoldByNavigation.LastName}",
                            SaleDate = s.SaleDate,
                            TotalAmount = s.TotalAmount,
                            DiscountAmount = s.DiscountAmount,
                            FinalAmount = s.FinalAmount,
                            PaymentStatus = s.PaymentStatus,
                            Notes = s.Notes,
                            IsCancelled = s.Notes != null && s.Notes.StartsWith("[CANCELLED]"),
                            CancellationReason = s.Notes != null && s.Notes.StartsWith("[CANCELLED]")
                                ? s.Notes.Replace("[CANCELLED]", "").Trim()
                                : null,
                            CreatedAt = s.CreatedAt,
                            Items = s.SaleItems.Select(si => new SaleItemViewModel
                            {
                                Id = si.Id,
                                SaleId = si.SaleId,
                                ProductId = si.ProductId,
                                Quantity = si.Quantity,
                                UnitPrice = si.UnitPrice,
                                TotalPrice = si.TotalPrice
                            }).ToList()
                        });

                    return salesQuery;
                });

                // Compter le total
                var totalCount = await _saleRepository.CountAsync(q =>
                {
                    var baseQuery = q;

                    // Appliquer les mêmes filtres que ci-dessus
                    if (filters.HospitalCenterId.HasValue)
                        baseQuery = baseQuery.Where(s => s.HospitalCenterId == filters.HospitalCenterId.Value);


                    if (filters.PatientId.HasValue)
                        baseQuery = baseQuery.Where(s => s.PatientId == filters.PatientId.Value);


                    if (filters.SoldBy.HasValue)
                        baseQuery = baseQuery.Where(s => s.SoldBy == filters.SoldBy.Value);


                    if (!string.IsNullOrWhiteSpace(filters.PaymentStatus))
                        baseQuery = baseQuery.Where(s => s.PaymentStatus == filters.PaymentStatus);


                    if (filters.FromDate.HasValue)
                    {
                        var fromDate = filters.FromDate.Value.Date;
                        baseQuery = baseQuery.Where(s => s.SaleDate >= fromDate);
                    }

                    if (filters.ToDate.HasValue)
                    {
                        var toDate = filters.ToDate.Value.Date.AddDays(1).AddMilliseconds(-1);
                        baseQuery = baseQuery.Where(s => s.SaleDate <= toDate);
                    }

                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var searchTerm = filters.SearchTerm.ToLower();
                        baseQuery = baseQuery.Where(s =>
                            s.SaleNumber.ToLower().Contains(searchTerm) ||
                            (s.Patient != null && (s.Patient.FirstName.ToLower().Contains(searchTerm) ||
                                              s.Patient.LastName.ToLower().Contains(searchTerm))) ||
                            (s.Notes != null && s.Notes.ToLower().Contains(searchTerm))
                        );
                    }

                    return baseQuery;
                });

                // Récupérer les sales
                var sales = query;

                // Charger les données complètes des produits pour chaque SaleItem
                foreach (var sale in sales)
                {
                    foreach (var item in sale.Items)
                    {
                        var product = await _productRepository.GetByIdAsync(item.ProductId);
                        if (product != null)
                        {
                            item.ProductName = product.Name;
                            item.UnitOfMeasure = product.UnitOfMeasure;

                            var category = await _productRepository.QuerySingleAsync(q =>
                                q.Where(p => p.Id == item.ProductId)
                                 .Include(p => p.ProductCategory)
                                 .Select(p => p.ProductCategory));

                            if (category != null)
                            {
                                item.CategoryName = category.Name;
                            }
                        }
                    }
                }

                // Charger les paiements pour chaque vente
                foreach (var sale in sales)
                {
                    var payments = await _paymentService.GetPaymentsByReferenceAsync("Sale", sale.Id);
                    sale.Payments = payments;
                }

                return (sales, totalCount);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "GetSalesAsync",
                    "Erreur lors de la récupération des ventes",
                    details: new { Filters = filters, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Crée une nouvelle vente avec gestion du stock et paiement optionnel
        /// </summary>
        public async Task<OperationResult<SaleViewModel>> CreateSaleAsync(CreateSaleViewModel model, int createdBy,
            bool immediatePayment = false, int? paymentMethodId = null, string? transactionReference = null)
        {
            try
            {
                // Vérification de la disponibilité des produits
                var cart = new CartViewModel
                {
                    Items = model.Items,
                    PatientId = model.PatientId,
                    PatientName = model.PatientName,
                    DiscountAmount = model.DiscountAmount,
                    DiscountReason = model.DiscountReason,
                    Notes = model.Notes
                };

                var validationResult = await ValidateCartAsync(cart, model.HospitalCenterId);
                if (!validationResult.IsSuccess)
                {
                    return OperationResult<SaleViewModel>.Error(validationResult.ErrorMessage);
                }

                // Récupération du nom du centre
                var center = await _hospitalCenterRepository.GetByIdAsync(model.HospitalCenterId);
                if (center == null)
                {
                    return OperationResult<SaleViewModel>.Error("Centre hospitalier invalide");
                }

                // Génération du numéro de vente
                string saleNumber = await GenerateSaleNumberAsync();

                // Création de la vente
                var sale = new Sale
                {
                    SaleNumber = saleNumber,
                    PatientId = model.PatientId,
                    HospitalCenterId = model.HospitalCenterId,
                    SoldBy = createdBy,
                    SaleDate = TimeZoneHelper.GetCameroonTime(),
                    TotalAmount = model.Items.Sum(i => i.Quantity * i.UnitPrice),
                    DiscountAmount = model.DiscountAmount,
                    FinalAmount = model.Items.Sum(i => i.Quantity * i.UnitPrice) - model.DiscountAmount,
                    PaymentStatus = "Pending", // Sera mis à jour après traitement du paiement si nécessaire
                    Notes = model.Notes,
                    CreatedBy = createdBy,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                // Enregistrement de la vente
                var createdSale = await _saleRepository.AddAsync(sale);

                // Création des articles de vente et mouvement de stock
                foreach (var item in model.Items)
                {
                    var saleItem = new SaleItem
                    {
                        SaleId = createdSale.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice,
                        CreatedBy = createdBy,
                        CreatedAt = TimeZoneHelper.GetCameroonTime()
                    };

                    await _saleItemRepository.AddAsync(saleItem);

                    // Mise à jour du stock
                    await DecrementStockAsync(
                        item.ProductId,
                        model.HospitalCenterId,
                        item.Quantity,
                        "Sale",
                        createdSale.Id,
                        createdBy);
                }

                // Traitement du paiement immédiat si demandé
                if (immediatePayment && paymentMethodId.HasValue)
                {
                    var paymentModel = new CreatePaymentViewModel
                    {
                        ReferenceType = "Sale",
                        ReferenceId = createdSale.Id,
                        PatientId = model.PatientId,
                        HospitalCenterId = model.HospitalCenterId,
                        PaymentMethodId = paymentMethodId.Value,
                        Amount = sale.FinalAmount, // Paiement total
                        PaymentDate = TimeZoneHelper.GetCameroonTime(),
                        ReceivedById = createdBy,
                        TransactionReference = transactionReference,
                        Notes = "Paiement à la création de la vente"
                    };

                    // Appel au service de paiement
                    var paymentResult = await _paymentService.CreatePaymentAsync(paymentModel, createdBy);

                    if (paymentResult.IsSuccess)
                    {
                        // Mise à jour du statut de paiement de la vente
                        await UpdateSalePaymentStatusAsync(createdSale.Id, "Paid", createdBy);
                    }
                    else
                    {
                        // La vente est créée mais le paiement a échoué
                        await _logger.LogWarningAsync("SaleService", "CreateSaleAsync",
                            $"Erreur lors du paiement immédiat pour la vente {createdSale.Id}",
                            createdBy,
                            model.HospitalCenterId,
                            details: new { SaleId = createdSale.Id, PaymentError = paymentResult.ErrorMessage });

                        return OperationResult<SaleViewModel>.Error($"La vente a été créée mais le paiement a échoué : {paymentResult.ErrorMessage}");
                    }
                }

                // Audit de la création
                await _auditService.LogActionAsync(
                    createdBy,
                    "SALE_CREATE",
                    "Sale",
                    createdSale.Id,
                    null,
                    new
                    {
                        SaleNumber = saleNumber,
                        PatientId = model.PatientId,
                        ItemCount = model.Items.Count,
                        TotalAmount = sale.TotalAmount,
                        FinalAmount = sale.FinalAmount
                    },
                    $"Vente {saleNumber} créée avec {model.Items.Count} article(s) pour un montant de {sale.FinalAmount} FCFA"
                );

                // Journalisation
                await _logger.LogInfoAsync("SaleService", "CreateSaleAsync",
                    $"Vente {saleNumber} créée",
                    createdBy,
                    model.HospitalCenterId,
                    "Sale",
                    createdSale.Id,
                    new { SaleId = createdSale.Id, Items = model.Items.Count });

                // Retourner la vente créée
                var viewModel = await GetByIdAsync(createdSale.Id);
                if (viewModel == null)
                {
                    return OperationResult<SaleViewModel>.Error("La vente a été créée mais n'a pas pu être récupérée");
                }

                return OperationResult<SaleViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "CreateSaleAsync",
                    "Erreur lors de la création de la vente",
                    createdBy,
                    model.HospitalCenterId,
                    details: new { Model = model, Error = ex.Message });
                return OperationResult<SaleViewModel>.Error("Une erreur est survenue lors de la création de la vente: " + ex.Message);
            }
        }

        /// <summary>
        /// Met à jour une vente (notes et remise uniquement)
        /// </summary>
        public async Task<OperationResult> UpdateSaleAsync(int id, UpdateSaleViewModel model, int modifiedBy)
        {
            try
            {
                var sale = await _saleRepository.GetByIdAsync(id);
                if (sale == null)
                {
                    return OperationResult.Error("Vente introuvable");
                }

                // Vérifier si la vente est annulée
                if (sale.Notes != null && sale.Notes.StartsWith("[CANCELLED]"))
                {
                    return OperationResult.Error("Impossible de modifier une vente annulée");
                }

                // Vérifier si la vente est déjà payée
                if (sale.PaymentStatus == "Paid")
                {
                    return OperationResult.Error("Impossible de modifier une vente déjà payée");
                }

                // Sauvegarde des anciennes valeurs pour l'audit
                var oldValues = new
                {
                    Notes = sale.Notes,
                    DiscountAmount = sale.DiscountAmount,
                    FinalAmount = sale.FinalAmount
                };

                // Mise à jour des champs modifiables
                sale.Notes = model.Notes;

                // Mettre à jour la remise et recalculer le montant final
                if (model.DiscountAmount != sale.DiscountAmount)
                {
                    if (model.DiscountAmount > sale.TotalAmount)
                    {
                        return OperationResult.Error("La remise ne peut pas être supérieure au montant total");
                    }

                    sale.DiscountAmount = model.DiscountAmount;
                    sale.FinalAmount = sale.TotalAmount - model.DiscountAmount;
                }

                // Mise à jour des informations d'audit
                sale.ModifiedBy = modifiedBy;
                sale.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                // Enregistrer les modifications
                await _saleRepository.UpdateAsync(sale);

                // Audit
                await _auditService.LogActionAsync(
                    modifiedBy,
                    "SALE_UPDATE",
                    "Sale",
                    id,
                    oldValues,
                    new
                    {
                        Notes = sale.Notes,
                        DiscountAmount = sale.DiscountAmount,
                        FinalAmount = sale.FinalAmount
                    },
                    $"Vente {sale.SaleNumber} mise à jour"
                );

                // Journalisation
                await _logger.LogInfoAsync("SaleService", "UpdateSaleAsync",
                    $"Vente {sale.SaleNumber} mise à jour",
                    modifiedBy,
                    sale.HospitalCenterId,
                    "Sale",
                    id,
                    new { SaleId = id });

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "UpdateSaleAsync",
                    $"Erreur lors de la mise à jour de la vente {id}",
                    modifiedBy,
                    null,
                    details: new { SaleId = id, Model = model, Error = ex.Message });
                return OperationResult.Error("Une erreur est survenue lors de la mise à jour de la vente");
            }
        }

        /// <summary>
        /// Annule une vente et restaure le stock
        /// </summary>
        public async Task<OperationResult> CancelSaleAsync(int id, string reason, int modifiedBy)
        {
            try
            {
                var sale = await _saleRepository.GetByIdAsync(id);
                if (sale == null)
                {
                    return OperationResult.Error("Vente introuvable");
                }

                // Vérifier si la vente est déjà annulée
                if (sale.Notes != null && sale.Notes.StartsWith("[CANCELLED]"))
                {
                    return OperationResult.Error("Cette vente est déjà annulée");
                }

                // Sauvegarde de l'ancien statut et des notes pour l'audit
                var oldValues = new
                {
                    PaymentStatus = sale.PaymentStatus,
                    Notes = sale.Notes
                };

                // Modifier les notes pour indiquer l'annulation
                sale.Notes = $"[CANCELLED] {reason}\nNotes originales: {sale.Notes ?? ""}";
                sale.PaymentStatus = "Cancelled";
                sale.ModifiedBy = modifiedBy;
                sale.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                // Mettre à jour la vente
                await _saleRepository.UpdateAsync(sale);

                // Restaurer le stock
                var saleItems = await _saleItemRepository.GetAllAsync(q =>
                    q.Where(si => si.SaleId == id));

                foreach (var item in saleItems)
                {
                    await RestoreStockAsync(
                        item.ProductId,
                        sale.HospitalCenterId,
                        item.Quantity,
                        "Sale",
                        id,
                        reason,
                        modifiedBy);
                }

                // Audit
                await _auditService.LogActionAsync(
                    modifiedBy,
                    "SALE_CANCEL",
                    "Sale",
                    id,
                    oldValues,
                    new
                    {
                        PaymentStatus = "Cancelled",
                        Notes = sale.Notes
                    },
                    $"Vente {sale.SaleNumber} annulée: {reason}"
                );

                // Journalisation
                await _logger.LogWarningAsync("SaleService", "CancelSaleAsync",
                    $"Vente {sale.SaleNumber} annulée",
                    modifiedBy,
                    sale.HospitalCenterId,
                    "Sale",
                    id,
                    new { SaleId = id, Reason = reason });

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "CancelSaleAsync",
                    $"Erreur lors de l'annulation de la vente {id}",
                    modifiedBy,
                    null,
                    details: new { SaleId = id, Reason = reason, Error = ex.Message });
                return OperationResult.Error("Une erreur est survenue lors de l'annulation de la vente");
            }
        }

        // ===== GESTION DU PANIER =====

        /// <summary>
        /// Ajoute un produit au panier
        /// </summary>
        public async Task<CartViewModel> AddToCartAsync(CartItemViewModel item, CartViewModel? existingCart = null)
        {
            try
            {
                // Initialiser un nouveau panier si nécessaire
                var cart = existingCart ?? new CartViewModel();

                // Vérifier si le produit existe déjà dans le panier
                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (existingItem != null)
                {
                    // Augmenter la quantité
                    existingItem.Quantity += item.Quantity;
                }
                else
                {
                    // Ajouter le nouvel article
                    cart.Items.Add(item);
                }

                return cart;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "AddToCartAsync",
                    "Erreur lors de l'ajout au panier",
                    details: new { ProductId = item.ProductId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Supprime un produit du panier
        /// </summary>
        public async Task<CartViewModel> RemoveFromCartAsync(int productId, CartViewModel existingCart)
        {
            try
            {
                existingCart.Items.RemoveAll(i => i.ProductId == productId);
                return existingCart;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "RemoveFromCartAsync",
                    "Erreur lors de la suppression du panier",
                    details: new { ProductId = productId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Met à jour la quantité d'un produit dans le panier
        /// </summary>
        public async Task<CartViewModel> UpdateCartItemQuantityAsync(int productId, decimal quantity, CartViewModel existingCart)
        {
            try
            {
                var item = existingCart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        // Supprimer l'article si la quantité est 0 ou négative
                        return await RemoveFromCartAsync(productId, existingCart);
                    }

                    // Mettre à jour la quantité
                    item.Quantity = quantity;
                }

                return existingCart;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "UpdateCartItemQuantityAsync",
                    "Erreur lors de la mise à jour de la quantité",
                    details: new { ProductId = productId, Quantity = quantity, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Applique une remise au panier
        /// </summary>
        public async Task<CartViewModel> ApplyDiscountAsync(decimal discountAmount, string? discountReason, CartViewModel existingCart)
        {
            try
            {
                // Vérifier que la remise n'excède pas le montant total
                if (discountAmount > existingCart.SubTotal)
                {
                    throw new Exception("La remise ne peut pas être supérieure au montant total");
                }

                existingCart.DiscountAmount = discountAmount;
                existingCart.DiscountReason = discountReason;

                return existingCart;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "ApplyDiscountAsync",
                    "Erreur lors de l'application de la remise",
                    details: new { DiscountAmount = discountAmount, Error = ex.Message });
                throw;
            }
        }

        // ===== OPÉRATIONS SPÉCIFIQUES =====

        /// <summary>
        /// Récupère l'historique des ventes d'un patient
        /// </summary>
        public async Task<List<SaleViewModel>> GetPatientSalesHistoryAsync(int patientId)
        {
            try
            {
                var patient = await _patientRepository.GetByIdAsync(patientId);
                if (patient == null)
                {
                    throw new Exception($"Patient {patientId} introuvable");
                }

                // Utiliser le filtre par patient
                var sales = await GetSalesAsync(new SaleFilters
                {
                    PatientId = patientId,
                    PageSize = 100 // Limiter à 100 dernières ventes
                });

                return sales.Sales;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "GetPatientSalesHistoryAsync",
                    $"Erreur lors de la récupération de l'historique des ventes du patient {patientId}",
                    details: new { PatientId = patientId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère un résumé des ventes pour un centre sur une période
        /// </summary>
        public async Task<SaleSummaryViewModel> GetSaleSummaryAsync(int hospitalCenterId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                // Dates par défaut si non spécifiées
                var effectiveFromDate = fromDate ?? DateTime.Now.AddDays(-30).Date;
                var effectiveToDate = toDate ?? DateTime.Now.Date.AddDays(1).AddMilliseconds(-1);

                var center = await _hospitalCenterRepository.GetByIdAsync(hospitalCenterId);
                if (center == null)
                {
                    throw new Exception($"Centre hospitalier {hospitalCenterId} introuvable");
                }

                // Récupérer les ventes sur la période
                var salesFilter = new SaleFilters
                {
                    HospitalCenterId = hospitalCenterId,
                    FromDate = effectiveFromDate,
                    ToDate = effectiveToDate,
                    PageSize = 10000 // Valeur élevée pour récupérer toutes les ventes
                };

                var (sales, _) = await GetSalesAsync(salesFilter);

                // Filtrer pour n'inclure que les ventes non annulées
                var validSales = sales.Where(s => s.PaymentStatus != "Cancelled").ToList();

                // Calculer les totaux
                decimal totalAmount = validSales.Sum(s => s.TotalAmount);
                decimal totalDiscounts = validSales.Sum(s => s.DiscountAmount);
                decimal netAmount = validSales.Sum(s => s.FinalAmount);

                // Compter les articles vendus
                int totalItemsSold = validSales.Sum(s => s.Items.Count);

                // Compter les patients uniques
                var uniquePatientIds = validSales
                    .Where(s => s.PatientId.HasValue)
                    .Select(s => s.PatientId.Value)
                    .Distinct()
                    .Count();

                // Agréger par statut de paiement
                var salesByStatus = validSales
                    .GroupBy(s => s.PaymentStatus)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.FinalAmount));

                // Agréger par méthode de paiement (basé sur les paiements associés)
                var salesByPaymentMethod = new Dictionary<string, decimal>();
                foreach (var sale in validSales)
                {
                    foreach (var payment in sale.Payments.Where(p => !p.IsCancelled))
                    {
                        if (!salesByPaymentMethod.ContainsKey(payment.PaymentMethodName))
                        {
                            salesByPaymentMethod[payment.PaymentMethodName] = 0;
                        }
                        salesByPaymentMethod[payment.PaymentMethodName] += payment.Amount;
                    }
                }

                // Calculer les produits les plus vendus (top 10)
                var topProducts = validSales
                    .SelectMany(s => s.Items)
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, ProductName = g.First().ProductName, Count = g.Count() })
                    .OrderByDescending(p => p.Count)
                    .Take(10)
                    .ToDictionary(p => p.ProductName, p => p.Count);

                return new SaleSummaryViewModel
                {
                    HospitalCenterId = hospitalCenterId,
                    HospitalCenterName = center.Name,
                    FromDate = effectiveFromDate,
                    ToDate = effectiveToDate,
                    TotalSales = validSales.Count,
                    TotalAmount = totalAmount,
                    TotalDiscounts = totalDiscounts,
                    NetAmount = netAmount,
                    TotalItemsSold = totalItemsSold,
                    TotalPatients = uniquePatientIds,
                    SalesByStatus = salesByStatus,
                    SalesByPaymentMethod = salesByPaymentMethod,
                    TopSellingProducts = topProducts
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "GetSaleSummaryAsync",
                    $"Erreur lors de la récupération du résumé des ventes pour le centre {hospitalCenterId}",
                    details: new { HospitalCenterId = hospitalCenterId, FromDate = fromDate, ToDate = toDate, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Génère un reçu de vente en PDF
        /// </summary>
        public async Task<byte[]> GenerateReceiptAsync(int saleId)
        {
            try
            {
                var sale = await GetByIdAsync(saleId);
                if (sale == null)
                {
                    throw new Exception($"Vente {saleId} introuvable");
                }

                // Récupérer les informations du centre
                var center = await _hospitalCenterRepository.GetByIdAsync(sale.HospitalCenterId);
                if (center == null)
                {
                    throw new Exception($"Centre hospitalier {sale.HospitalCenterId} introuvable");
                }

                // Récupérer les informations du vendeur
                var seller = await _userRepository.GetByIdAsync(sale.SoldBy);
                if (seller == null)
                {
                    throw new Exception($"Vendeur {sale.SoldBy} introuvable");
                }

                // Créer le modèle pour le PDF
                var receiptModel = new SaleReceiptModel
                {
                    SaleNumber = sale.SaleNumber,
                    SaleDate = sale.SaleDate,
                    HospitalName = center.Name,
                    HospitalAddress = center.Address ?? "",
                    HospitalContact = center.PhoneNumber ?? "",
                    SellerName = $"{seller.FirstName} {seller.LastName}",
                    PatientName = sale.PatientName,
                    Items = sale.Items.Select(i => new SaleReceiptItemModel
                    {
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitOfMeasure = i.UnitOfMeasure,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.TotalPrice
                    }).ToList(),
                    SubTotal = sale.TotalAmount,
                    DiscountAmount = sale.DiscountAmount,
                    TotalAmount = sale.FinalAmount,
                    PaymentStatus = sale.PaymentStatus,
                    Payments = sale.Payments.Where(p => !p.IsCancelled).Select(p => new PaymentReceiptModel
                    {
                        PaymentDate = p.PaymentDate,
                        Amount = p.Amount,
                        Method = p.PaymentMethodName,
                        Reference = p.TransactionReference
                    }).ToList()
                };

                // Générer le PDF avec QuestPDF
                var document = new SaleReceiptDocument(receiptModel);
                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "GenerateReceiptAsync",
                    $"Erreur lors de la génération du reçu pour la vente {saleId}",
                    details: new { SaleId = saleId, Error = ex.Message });
                throw;
            }
        }

        // ===== VALIDATION =====

        /// <summary>
        /// Valide le contenu d'un panier avant création de la vente
        /// </summary>
        public async Task<OperationResult> ValidateCartAsync(CartViewModel cart, int hospitalCenterId)
        {
            try
            {
                if (cart.Items.Count == 0)
                {
                    return OperationResult.Error("Le panier est vide");
                }

                // Vérifier la disponibilité des produits
                foreach (var item in cart.Items)
                {
                    var inventory = await _stockInventoryRepository.GetSingleAsync(q =>
                        q.Where(si => si.ProductId == item.ProductId && si.HospitalCenterId == hospitalCenterId));

                    if (inventory == null)
                    {
                        return OperationResult.Error($"Le produit {item.ProductName} n'est pas disponible dans ce centre");
                    }

                    if (inventory.CurrentQuantity < item.Quantity)
                    {
                        return OperationResult.Error($"Stock insuffisant pour '{item.ProductName}' (disponible: {inventory.CurrentQuantity} {item.UnitOfMeasure})");
                    }

                    // Vérifier que le produit est actif
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product == null || !product.IsActive)
                    {
                        return OperationResult.Error($"Le produit '{item.ProductName}' n'est pas actif ou n'existe plus");
                    }
                }

                // Vérifier que la remise n'excède pas le montant total
                decimal totalAmount = cart.Items.Sum(i => i.Quantity * i.UnitPrice);
                if (cart.DiscountAmount > totalAmount)
                {
                    return OperationResult.Error("La remise ne peut pas être supérieure au montant total");
                }

                // Vérifier la validité du patient si spécifié
                if (cart.PatientId.HasValue)
                {
                    var patient = await _patientRepository.GetByIdAsync(cart.PatientId.Value);
                    if (patient == null)
                    {
                        return OperationResult.Error("Patient invalide");
                    }

                    if (!patient.IsActive)
                    {
                        return OperationResult.Error("Le patient sélectionné est inactif");
                    }
                }

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "ValidateCartAsync",
                    "Erreur lors de la validation du panier",
                    details: new { CartItems = cart.Items.Count, Error = ex.Message });
                return OperationResult.Error("Une erreur est survenue lors de la validation du panier");
            }
        }

        /// <summary>
        /// Vérifie la disponibilité des produits dans le stock
        /// </summary>
        public async Task<List<ProductAvailabilityViewModel>> CheckProductsAvailabilityAsync(List<CartItemViewModel> items, int hospitalCenterId)
        {
            try
            {
                var availability = new List<ProductAvailabilityViewModel>();

                foreach (var item in items)
                {
                    var inventory = await _stockInventoryRepository.GetSingleAsync(q =>
                        q.Where(si => si.ProductId == item.ProductId && si.HospitalCenterId == hospitalCenterId)
                         .Include(si => si.Product));

                    if (inventory != null)
                    {
                        availability.Add(new ProductAvailabilityViewModel
                        {
                            ProductId = item.ProductId,
                            ProductName = inventory.Product.Name,
                            CurrentStock = inventory.CurrentQuantity,
                            MinimumThreshold = inventory.MinimumThreshold,
                            UnitOfMeasure = inventory.Product.UnitOfMeasure
                        });
                    }
                    else
                    {
                        // Produit non trouvé dans ce centre
                        var product = await _productRepository.GetByIdAsync(item.ProductId);
                        availability.Add(new ProductAvailabilityViewModel
                        {
                            ProductId = item.ProductId,
                            ProductName = product?.Name ?? $"Produit {item.ProductId}",
                            CurrentStock = 0,
                            MinimumThreshold = 0,
                            UnitOfMeasure = product?.UnitOfMeasure ?? "unité"
                        });
                    }
                }

                return availability;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "CheckProductsAvailabilityAsync",
                    "Erreur lors de la vérification de la disponibilité des produits",
                    details: new { HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Met à jour le statut de paiement d'une vente
        /// </summary>
        public async Task<OperationResult> UpdateSalePaymentStatusAsync(int saleId, string status, int modifiedBy)
        {
            try
            {
                var sale = await _saleRepository.GetByIdAsync(saleId);
                if (sale == null)
                {
                    return OperationResult.Error("Vente introuvable");
                }

                // Vérifier si la vente est annulée
                if (sale.Notes != null && sale.Notes.StartsWith("[CANCELLED]"))
                {
                    return OperationResult.Error("Impossible de modifier le statut d'une vente annulée");
                }

                string oldStatus = sale.PaymentStatus;
                sale.PaymentStatus = status;
                sale.ModifiedBy = modifiedBy;
                sale.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _saleRepository.UpdateAsync(sale);

                // Audit
                await _auditService.LogActionAsync(
                    modifiedBy,
                    "SALE_STATUS_UPDATE",
                    "Sale",
                    saleId,
                    new { PaymentStatus = oldStatus },
                    new { PaymentStatus = status },
                    $"Statut de paiement de la vente {sale.SaleNumber} modifié: {oldStatus} → {status}"
                );

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "UpdateSalePaymentStatusAsync",
                    $"Erreur lors de la mise à jour du statut de paiement de la vente {saleId}",
                    modifiedBy,
                    null,
                    details: new { SaleId = saleId, Status = status, Error = ex.Message });
                return OperationResult.Error("Une erreur est survenue lors de la mise à jour du statut de paiement");
            }
        }

        // ===== MÉTHODES PRIVÉES =====

        /// <summary>
        /// Génère un numéro de vente unique
        /// </summary>
        private async Task<string> GenerateSaleNumberAsync()
        {
            try
            {
                // Format: SALE-YYYYMMDD-XXXXX
                string dateCode = TimeZoneHelper.GetCameroonTime().ToString("yyyyMMdd");
                string prefix = $"SALE-{dateCode}-";

                // Trouver le dernier numéro utilisé pour aujourd'hui
                var lastSaleToday = await _saleRepository.GetSingleAsync(q =>
                    q.Where(s => s.SaleNumber.StartsWith(prefix))
                     .OrderByDescending(s => s.SaleNumber));

                int sequenceNumber = 1;
                if (lastSaleToday != null)
                {
                    // Extraire le numéro de séquence du dernier numéro de vente
                    string lastSequenceStr = lastSaleToday.SaleNumber.Substring(prefix.Length);
                    if (int.TryParse(lastSequenceStr, out int lastSequence))
                    {
                        sequenceNumber = lastSequence + 1;
                    }
                }

                return $"{prefix}{sequenceNumber:D5}";
            }
            catch (Exception ex)
            {

                // Fallback en cas d'erreur
                var timestamp = TimeZoneHelper.GetCameroonTime().ToString("yyyyMMddHHmmss");
                var random = new Random();
                return $"SALE-{timestamp}-{random.Next(10000, 99999)}";
            }
        }

        /// <summary>
        /// Décrémente le stock d'un produit lors d'une vente
        /// </summary>
        private async Task DecrementStockAsync(
            int productId,
            int hospitalCenterId,
            decimal quantity,
            string referenceType,
            int referenceId,
            int userId)
        {
            try
            {
                // Récupérer le stock actuel
                var inventory = await _stockInventoryRepository.GetSingleAsync(q =>
                    q.Where(si => si.ProductId == productId && si.HospitalCenterId == hospitalCenterId));

                if (inventory == null)
                {
                    throw new Exception($"Stock non trouvé pour le produit {productId} dans le centre {hospitalCenterId}");
                }

                // Vérifier la disponibilité
                if (inventory.CurrentQuantity < quantity)
                {
                    throw new Exception($"Stock insuffisant pour le produit {productId}");
                }

                // Mettre à jour le stock
                decimal oldQuantity = inventory.CurrentQuantity;
                inventory.CurrentQuantity -= quantity;
                inventory.ModifiedBy = userId;
                inventory.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _stockInventoryRepository.UpdateAsync(inventory);

                // Créer un mouvement de stock
                var movement = new StockMovement
                {
                    ProductId = productId,
                    HospitalCenterId = hospitalCenterId,
                    MovementType = "Sale",
                    Quantity = -quantity, // Négatif pour une sortie
                    ReferenceType = referenceType,
                    ReferenceId = referenceId,
                    Notes = $"Vente #{referenceId}",
                    MovementDate = TimeZoneHelper.GetCameroonTime(),
                    CreatedBy = userId,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                await _stockMovementRepository.AddAsync(movement);

                // Audit
                await _auditService.LogActionAsync(
                    userId,
                    "STOCK_DECREMENT",
                    "StockInventory",
                    inventory.Id,
                    new { CurrentQuantity = oldQuantity },
                    new { CurrentQuantity = inventory.CurrentQuantity },
                    $"Stock décrémenté de {quantity} unités pour le produit {productId} (Vente #{referenceId})"
                );
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "DecrementStockAsync",
                    $"Erreur lors de la décrémentation du stock pour le produit {productId}",
                    userId,
                    hospitalCenterId,
                    details: new { ProductId = productId, Quantity = quantity, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Restaure le stock d'un produit lors de l'annulation d'une vente
        /// </summary>
        private async Task RestoreStockAsync(
            int productId,
            int hospitalCenterId,
            decimal quantity,
            string referenceType,
            int referenceId,
            string reason,
            int userId)
        {
            try
            {
                // Récupérer le stock actuel
                var inventory = await _stockInventoryRepository.GetSingleAsync(q =>
                    q.Where(si => si.ProductId == productId && si.HospitalCenterId == hospitalCenterId));

                if (inventory == null)
                {
                    throw new Exception($"Stock non trouvé pour le produit {productId} dans le centre {hospitalCenterId}");
                }

                // Mettre à jour le stock
                decimal oldQuantity = inventory.CurrentQuantity;
                inventory.CurrentQuantity += quantity; // Réincrémentation du stock
                inventory.ModifiedBy = userId;
                inventory.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _stockInventoryRepository.UpdateAsync(inventory);

                // Créer un mouvement de stock (restauration)
                var movement = new StockMovement
                {
                    ProductId = productId,
                    HospitalCenterId = hospitalCenterId,
                    MovementType = "Adjustment",
                    Quantity = quantity, // Positif pour une entrée
                    ReferenceType = referenceType,
                    ReferenceId = referenceId,
                    Notes = $"Restauration suite à annulation vente #{referenceId}: {reason}",
                    MovementDate = TimeZoneHelper.GetCameroonTime(),
                    CreatedBy = userId,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                await _stockMovementRepository.AddAsync(movement);

                // Audit
                await _auditService.LogActionAsync(
                    userId,
                    "STOCK_RESTORE",
                    "StockInventory",
                    inventory.Id,
                    new { CurrentQuantity = oldQuantity },
                    new { CurrentQuantity = inventory.CurrentQuantity },
                    $"Stock restauré de {quantity} unités pour le produit {productId} (Annulation vente #{referenceId})"
                );
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("SaleService", "RestoreStockAsync",
                    $"Erreur lors de la restauration du stock pour le produit {productId}",
                    userId,
                    hospitalCenterId,
                    details: new { ProductId = productId, Quantity = quantity, Error = ex.Message });
                throw;
            }
        }
    }

    /// <summary>
    /// Classes pour la génération du reçu de vente
    /// </summary>
    public class SaleReceiptModel
    {
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string HospitalAddress { get; set; } = string.Empty;
        public string HospitalContact { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string? PatientName { get; set; }
        public List<SaleReceiptItemModel> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<PaymentReceiptModel> Payments { get; set; } = new();
    }

    public class SaleReceiptItemModel
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class PaymentReceiptModel
    {
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? Reference { get; set; }
    }

    /// <summary>
    /// Document QuestPDF pour la génération des reçus de vente
    /// </summary>
    public class SaleReceiptDocument
    {
        private readonly SaleReceiptModel _model;

        public SaleReceiptDocument(SaleReceiptModel model)
        {
            _model = model;
        }

        public byte[] GeneratePdf()
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(QuestPDF.Infrastructure.IContainer container)
        {
            container.Row(row =>
            {
                // Logo et informations du centre
                row.RelativeItem(2).Column(column =>
                {
                    column.Item().Text(_model.HospitalName)
                        .FontSize(16).Bold();

                    column.Item().Text(_model.HospitalAddress)
                        .FontSize(10);

                    column.Item().Text(_model.HospitalContact)
                        .FontSize(10);
                });

                // Informations du reçu
                row.RelativeItem(1).Column(column =>
                {
                    column.Item().AlignRight().Text("REÇU DE VENTE")
                        .FontSize(16).Bold();

                    column.Item().AlignRight().Text($"N° {_model.SaleNumber}")
                        .FontSize(10);

                    column.Item().AlignRight().Text($"Date: {_model.SaleDate:dd/MM/yyyy HH:mm}")
                        .FontSize(10);
                });
            });
        }

        private void ComposeContent(QuestPDF.Infrastructure.IContainer container)
        {
            container.Column(column =>
            {
                // Informations client/vendeur
                column.Item().PaddingTop(20).Grid(grid =>
                {
                    grid.Columns(2);

                    grid.Item().Column(c =>
                    {
                        c.Item().Text("Vendeur:").Bold();
                        c.Item().Text(_model.SellerName);
                    });

                    grid.Item().Column(c =>
                    {
                        c.Item().Text("Client:").Bold();
                        c.Item().Text(_model.PatientName ?? "Client occasionnel");
                    });
                });

                // Table des articles
                column.Item().PaddingTop(20).Table(table =>
                {
                    // Définition des colonnes
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    // En-têtes
                    table.Header(header =>
                    {
                        header.Cell().Text("Produit").Bold();
                        header.Cell().AlignRight().Text("Quantité").Bold();
                        header.Cell().AlignRight().Text("Prix unitaire").Bold();
                        header.Cell().AlignRight().Text("Total").Bold();

                        header.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Black);
                    });

                    // Lignes d'articles
                    foreach (var item in _model.Items)
                    {
                        table.Cell().Text(item.ProductName);
                        table.Cell().AlignRight().Text($"{item.Quantity:N2} {item.UnitOfMeasure}");
                        table.Cell().AlignRight().Text($"{item.UnitPrice:N0} FCFA");
                        table.Cell().AlignRight().Text($"{item.TotalPrice:N0} FCFA");
                    }

                    // Total
                    table.Cell().ColumnSpan(3).AlignRight().Text("Sous-total:").Bold();
                    table.Cell().AlignRight().Text($"{_model.SubTotal:N0} FCFA").Bold();

                    if (_model.DiscountAmount > 0)
                    {
                        table.Cell().ColumnSpan(3).AlignRight().Text("Remise:").Bold();
                        table.Cell().AlignRight().Text($"{_model.DiscountAmount:N0} FCFA").Bold();
                    }

                    table.Cell().ColumnSpan(3).AlignRight().Text("Total:").Bold();
                    table.Cell().AlignRight().Border(1).BorderColor(Colors.Black)
                        .Text($"{_model.TotalAmount:N0} FCFA").Bold();
                });

                // Informations de paiement
                column.Item().PaddingTop(20).Column(c =>
                {
                    c.Item().Text("Informations de paiement").Bold();

                    if (_model.PaymentStatus == "Paid")
                    {
                        c.Item().Text("Statut: Payé").FontColor(Colors.Green.Medium);
                    }
                    else if (_model.PaymentStatus == "Partial")
                    {
                        c.Item().Text("Statut: Partiellement payé").FontColor(Colors.Orange.Medium);
                    }
                    else if (_model.PaymentStatus == "Pending")
                    {
                        c.Item().Text("Statut: En attente de paiement").FontColor(Colors.Blue.Medium);
                    }

                    if (_model.Payments.Any())
                    {
                        c.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Date").Bold();
                                header.Cell().Text("Méthode").Bold();
                                header.Cell().AlignRight().Text("Montant").Bold();
                            });

                            foreach (var payment in _model.Payments)
                            {
                                table.Cell().Text(payment.PaymentDate.ToString("dd/MM/yyyy HH:mm"));
                                table.Cell().Text(payment.Method + (payment.Reference != null ? $" (Réf: {payment.Reference})" : ""));
                                table.Cell().AlignRight().Text($"{payment.Amount:N0} FCFA");
                            }

                            decimal totalPaid = _model.Payments.Sum(p => p.Amount);
                            decimal remaining = Math.Max(0, _model.TotalAmount - totalPaid);

                            if (remaining > 0)
                            {
                                table.Cell().ColumnSpan(2).AlignRight().Text("Reste à payer:").Bold();
                                table.Cell().AlignRight().Text($"{remaining:N0} FCFA").Bold()
                                    .FontColor(Colors.Red.Medium);
                            }
                        });
                    }
                });
            });
        }

        private void ComposeFooter(QuestPDF.Infrastructure.IContainer container)
        {
            container.Column(column =>
            {
                column.Item().PaddingTop(10).BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                    .AlignCenter().Text("Merci pour votre achat!").FontSize(10);

                column.Item().AlignCenter().Text($"Imprimé le {TimeZoneHelper.GetCameroonTime():dd/MM/yyyy HH:mm}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }
    }
}