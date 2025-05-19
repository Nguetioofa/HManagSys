using HManagSys.Attributes;
using HManagSys.Helpers;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Payments;
using HManagSys.Models.ViewModels.Sales;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Implementations;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HManagSys.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des ventes
    /// </summary>
    [RequireAuthentication]
    [RequireCurrentCenter]
    public class SaleController : BaseController
    {
        private readonly ISaleService _saleService;
        private readonly IProductService _productService;
        private readonly IPatientService _patientService;
        private readonly IPaymentService _paymentService;
        private readonly IDocumentGenerationService _documentGenerationService;
        private readonly IApplicationLogger _logger;
        private readonly IStockService _stockInventoryRepository;

        public SaleController(
            ISaleService saleService,
            IProductService productService,
            IPatientService patientService,
            IPaymentService paymentService,
            IDocumentGenerationService documentGenerationService,
            IStockService stockInventoryRepository,
            IApplicationLogger logger)
        {
            _saleService = saleService;
            _productService = productService;
            _patientService = patientService;
            _paymentService = paymentService;
            _stockInventoryRepository = stockInventoryRepository;
            _documentGenerationService = documentGenerationService;
            _logger = logger;
        }

        /// <summary>
        /// Liste des ventes avec filtres et pagination
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Index(SaleFilters? filters = null)
        {
            try
            {
                filters ??= new SaleFilters();
                filters.HospitalCenterId = CurrentCenterId;

                var (sales, totalCount) = await _saleService.GetSalesAsync(filters);

                var viewModel = new PagedViewModel<SaleViewModel, SaleFilters>
                {
                    Items = sales,
                    Filters = filters,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = filters.PageIndex,
                        PageSize = filters.PageSize,
                        TotalCount = totalCount
                    }
                };

                // Charger un résumé des ventes récentes pour le tableau de bord
                ViewBag.Summary = await _saleService.GetSaleSummaryAsync(
                    CurrentCenterId.Value,
                    DateTime.Now.AddDays(-30),
                    DateTime.Now);

                // Log de l'accès
                await _logger.LogInfoAsync("Sale", "IndexAccessed",
                    "Consultation de la liste des ventes",
                    CurrentUserId, CurrentCenterId);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "IndexError",
                    "Erreur lors du chargement de la liste des ventes",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des ventes";
                return View(new PagedViewModel<SaleViewModel, SaleFilters>());
            }
        }

        /// <summary>
        /// Affichage du formulaire de création d'une vente
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Initialiser un panier vide ou récupérer celui en session
                var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("CurrentCart") ?? new CartViewModel();

                // Stocker en session
                HttpContext.Session.SetObjectAsJson("CurrentCart", cart);

                // Préparer le modèle pour la vue
                var model = new CreateSaleViewModel
                {
                    HospitalCenterId = CurrentCenterId.Value,
                    HospitalCenterName = CurrentCenterName,
                    Items = cart.Items,
                    PatientId = cart.PatientId,
                    PatientName = cart.PatientName,
                    DiscountAmount = cart.DiscountAmount,
                    DiscountReason = cart.DiscountReason,
                    Notes = cart.Notes
                };

                // Charger les méthodes de paiement
                model.PaymentMethods = (await _paymentService.GetPaymentMethodsAsync())
                    .Select(m => new SelectOption(m.Id.ToString(), m.Name))
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "CreateGetError",
                    "Erreur lors du chargement du formulaire de vente",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire de vente";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Traitement du formulaire de création de vente
        /// </summary>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        [MedicalStaff]
        public async Task<IActionResult> Create([FromBody] CreateSaleRequest request)
        {
            try
            {
                if (request == null || request.Items == null || !request.Items.Any())
                {
                    return Json(new { success = false, message = "Le panier est vide" });
                }

                // Créer le modèle de vente à partir de la requête
                var model = new CreateSaleViewModel
                {
                    PatientId = request.PatientId,
                    PatientName = request.PatientName,
                    HospitalCenterId = CurrentCenterId.Value,
                    Notes = request.Notes,
                    DiscountAmount = request.DiscountAmount,
                    DiscountReason = request.DiscountReason,
                    Items = request.Items
                };

                // Créer la vente avec paiement immédiat si demandé
                var result = await _saleService.CreateSaleAsync(
                    model,
                    CurrentUserId.Value,
                    request.ImmediatePayment,
                    request.PaymentMethodId,
                    request.TransactionReference);

                if (result.IsSuccess)
                {
                    // Réinitialiser le panier en session
                    HttpContext.Session.Remove("CurrentCart");

                    return Json(new
                    {
                        success = true,
                        message = "Vente créée avec succès",
                        saleId = result.Data.Id,
                        redirectUrl = Url.Action("Receipt", new { id = result.Data.Id })
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "CreatePostError",
                    "Erreur lors de la création de la vente",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Une erreur est survenue lors de la création de la vente" });
            }
        }

        /// <summary>
        /// Affichage des détails d'une vente
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var sale = await _saleService.GetByIdAsync(id);
                if (sale == null)
                {
                    TempData["ErrorMessage"] = "Vente introuvable";
                    return RedirectToAction(nameof(Index));
                }

                return View(sale);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "DetailsError",
                    $"Erreur lors du chargement des détails de la vente {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { SaleId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des détails de la vente";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Affichage du reçu de vente
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Receipt(int id)
        {
            try
            {
                var sale = await _saleService.GetByIdAsync(id);
                if (sale == null)
                {
                    TempData["ErrorMessage"] = "Vente introuvable";
                    return RedirectToAction(nameof(Index));
                }

                return View(sale);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "ReceiptError",
                    $"Erreur lors du chargement du reçu de la vente {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { SaleId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du reçu";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// Téléchargement du reçu en PDF
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            try
            {
                var sale = await _saleService.GetByIdAsync(id);
                if (sale == null)
                {
                    TempData["ErrorMessage"] = "Vente introuvable";
                    return RedirectToAction(nameof(Index));
                }

                var pdfBytes = await _saleService.GenerateReceiptAsync(id);

                return File(pdfBytes, "application/pdf", $"Recu_Vente_{sale.SaleNumber}.pdf");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "DownloadReceiptError",
                    $"Erreur lors du téléchargement du reçu de la vente {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { SaleId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du PDF";
                return RedirectToAction(nameof(Receipt), new { id });
            }
        }

        /// <summary>
        /// Annulation d'une vente (réservé aux SuperAdmin)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["ErrorMessage"] = "Une raison est requise pour annuler la vente";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _saleService.CancelSaleAsync(id, reason, CurrentUserId.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Vente annulée avec succès";
                    return RedirectToAction(nameof(Details), new { id });
                }

                TempData["ErrorMessage"] = result.ErrorMessage ?? "Erreur lors de l'annulation de la vente";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "CancelError",
                    $"Erreur lors de l'annulation de la vente {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { SaleId = id, Reason = reason, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur est survenue lors de l'annulation de la vente";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        #region AJAX Cart Operations

        /// <summary>
        /// Recherche de produits pour la vente
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public async Task<IActionResult> SearchProducts(string searchTerm, int? categoryId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm) && !categoryId.HasValue)
                {
                    return Json(new List<object>());
                }

                var products = await _productService.SearchProductsAsync(searchTerm, categoryId, CurrentCenterId);

                // Récupérer les stocks pour ces produits
                var productIds = products.Select(p => p.Id).ToList();
                var stocksQuery = await _stockInventoryRepository.QueryListAsync(q =>
                    q.Where(si => productIds.Contains(si.ProductId) && si.HospitalCenterId == CurrentCenterId.Value));

                var stocks = stocksQuery.ToDictionary(
                    si => si.ProductId,
                    si => new { CurrentStock = si.CurrentQuantity, MinThreshold = si.MinimumThreshold }
                );

                // Transformer en résultat avec infos de stock
                var result = products.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    categoryId = categoryId,
                    categoryName = p.CategoryName,
                    unitOfMeasure = p.UnitOfMeasure,
                    sellingPrice = p.SellingPrice,
                    currentStock = stocks.ContainsKey(p.Id) ? stocks[p.Id].CurrentStock : 0,
                    minThreshold = stocks.ContainsKey(p.Id) ? stocks[p.Id].MinThreshold : null,
                    isAvailable = stocks.ContainsKey(p.Id) && stocks[p.Id].CurrentStock > 0
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "SearchProductsError",
                    "Erreur lors de la recherche de produits",
                    CurrentUserId, CurrentCenterId,
                    details: new { SearchTerm = searchTerm, CategoryId = categoryId, Error = ex.Message });

                return Json(new { error = "Erreur lors de la recherche de produits" });
            }
        }

        /// <summary>
        /// Ajoute un produit au panier
        /// </summary>
        [HttpPost]
        [MedicalStaff]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0 || request.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Données invalides" });
                }

                // Récupérer le panier actuel
                var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("CurrentCart") ?? new CartViewModel();

                // Récupérer les informations du produit
                var product = await _productService.GetProductByIdAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Produit introuvable" });
                }

                // Vérifier la disponibilité
                var inventory = await _stockInventoryRepository.QuerySingleAsync(q =>
                    q.Where(si => si.ProductId == request.ProductId && si.HospitalCenterId == CurrentCenterId));

                if (inventory == null || inventory.CurrentQuantity < request.Quantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = inventory == null ?
                            "Produit non disponible dans ce centre" :
                            $"Stock insuffisant (disponible: {inventory.CurrentQuantity} {product.UnitOfMeasure})"
                    });
                }

                // Créer l'article pour le panier
                var cartItem = new CartItemViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CategoryName = product.CategoryName,
                    UnitOfMeasure = product.UnitOfMeasure,
                    Quantity = request.Quantity,
                    UnitPrice = product.SellingPrice,
                    AvailableStock = inventory.CurrentQuantity
                };

                // Ajouter au panier
                cart = await _saleService.AddToCartAsync(cartItem, cart);

                // Sauvegarder en session
                HttpContext.Session.SetObjectAsJson("CurrentCart", cart);

                return Json(new
                {
                    success = true,
                    message = "Produit ajouté au panier",
                    cartSummary = new
                    {
                        itemCount = cart.ItemCount,
                        subtotal = cart.SubTotal,
                        discountAmount = cart.DiscountAmount,
                        finalAmount = cart.FinalAmount,
                        items = cart.Items.Select(i => new
                        {
                            productId = i.ProductId,
                            productName = i.ProductName,
                            categoryName = i.CategoryName,
                            unitOfMeasure = i.UnitOfMeasure,
                            quantity = i.Quantity,
                            unitPrice = i.UnitPrice,
                            totalPrice = i.TotalPrice,
                            formattedUnitPrice = i.FormattedUnitPrice,
                            formattedTotalPrice = i.FormattedTotalPrice
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "AddToCartError",
                    "Erreur lors de l'ajout au panier",
                    CurrentUserId, CurrentCenterId,
                    details: new { Request = request, Error = ex.Message });

                return Json(new { success = false, message = "Une erreur est survenue lors de l'ajout au panier" });
            }
        }

        /// <summary>
        /// Supprime un produit du panier
        /// </summary>
        [HttpPost]
        [MedicalStaff]
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0)
                {
                    return Json(new { success = false, message = "Données invalides" });
                }

                // Récupérer le panier actuel
                var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("CurrentCart");
                if (cart == null)
                {
                    return Json(new { success = false, message = "Panier inexistant" });
                }

                // Supprimer du panier
                cart = await _saleService.RemoveFromCartAsync(request.ProductId, cart);

                // Sauvegarder en session
                HttpContext.Session.SetObjectAsJson("CurrentCart", cart);

                return Json(new
                {
                    success = true,
                    message = "Produit supprimé du panier",
                    cartSummary = new
                    {
                        itemCount = cart.ItemCount,
                        subtotal = cart.SubTotal,
                        discountAmount = cart.DiscountAmount,
                        finalAmount = cart.FinalAmount,
                        items = cart.Items.Select(i => new
                        {
                            productId = i.ProductId,
                            productName = i.ProductName,
                            categoryName = i.CategoryName,
                            unitOfMeasure = i.UnitOfMeasure,
                            quantity = i.Quantity,
                            unitPrice = i.UnitPrice,
                            totalPrice = i.TotalPrice,
                            formattedUnitPrice = i.FormattedUnitPrice,
                            formattedTotalPrice = i.FormattedTotalPrice
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "RemoveFromCartError",
                    "Erreur lors de la suppression du panier",
                    CurrentUserId, CurrentCenterId,
                    details: new { Request = request, Error = ex.Message });

                return Json(new { success = false, message = "Une erreur est survenue lors de la suppression du panier" });
            }
        }

        /// <summary>
        /// Met à jour la quantité d'un produit dans le panier
        /// </summary>
        [HttpPost]
        [MedicalStaff]
        public async Task<IActionResult> UpdateCartItemQuantity([FromBody] UpdateCartItemRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0 || request.Quantity < 0)
                {
                    return Json(new { success = false, message = "Données invalides" });
                }

                // Récupérer le panier actuel
                var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("CurrentCart");
                if (cart == null)
                {
                    return Json(new { success = false, message = "Panier inexistant" });
                }

                // Vérifier la disponibilité si augmentation
                var currentItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                if (currentItem != null && request.Quantity > currentItem.Quantity)
                {
                    var inventory = await  _stockInventoryRepository.QuerySingleAsync(q =>
                        q.Where(si => si.ProductId == request.ProductId && si.HospitalCenterId == CurrentCenterId));

                    if (inventory == null || inventory.CurrentQuantity < request.Quantity)
                    {
                        return Json(new
                        {
                            success = false,
                            message = inventory == null ?
                                "Produit non disponible dans ce centre" :
                                $"Stock insuffisant (disponible: {inventory.CurrentQuantity} {currentItem.UnitOfMeasure})"
                        });
                    }
                }

                // Mettre à jour la quantité
                cart = await _saleService.UpdateCartItemQuantityAsync(request.ProductId, request.Quantity, cart);

                // Sauvegarder en session
                HttpContext.Session.SetObjectAsJson("CurrentCart", cart);

                return Json(new
                {
                    success = true,
                    message = "Quantité mise à jour",
                    cartSummary = new
                    {
                        itemCount = cart.ItemCount,
                        subtotal = cart.SubTotal,
                        discountAmount = cart.DiscountAmount,
                        finalAmount = cart.FinalAmount,
                        items = cart.Items.Select(i => new
                        {
                            productId = i.ProductId,
                            productName = i.ProductName,
                            categoryName = i.CategoryName,
                            unitOfMeasure = i.UnitOfMeasure,
                            quantity = i.Quantity,
                            unitPrice = i.UnitPrice,
                            totalPrice = i.TotalPrice,
                            formattedUnitPrice = i.FormattedUnitPrice,
                            formattedTotalPrice = i.FormattedTotalPrice
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "UpdateCartItemQuantityError",
                    "Erreur lors de la mise à jour de la quantité",
                    CurrentUserId, CurrentCenterId,
                    details: new { Request = request, Error = ex.Message });

                return Json(new { success = false, message = "Une erreur est survenue lors de la mise à jour de la quantité" });
            }
        }

        /// <summary>
        /// Applique une remise au panier
        /// </summary>
        [HttpPost]
        [MedicalStaff]
        public async Task<IActionResult> ApplyDiscount([FromBody] DiscountRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, message = "Données invalides" });
                }

                // Récupérer le panier actuel
                var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("CurrentCart");
                if (cart == null || !cart.Items.Any())
                {
                    return Json(new { success = false, message = "Panier vide ou inexistant" });
                }

                // Valider le montant de la remise
                if (request.DiscountAmount < 0)
                {
                    return Json(new { success = false, message = "Le montant de la remise ne peut pas être négatif" });
                }

                if (request.DiscountAmount > cart.SubTotal)
                {
                    return Json(new { success = false, message = "La remise ne peut pas être supérieure au montant total" });
                }

                // Appliquer la remise
                cart = await _saleService.ApplyDiscountAsync(request.DiscountAmount, request.DiscountReason, cart);

                // Sauvegarder en session
                HttpContext.Session.SetObjectAsJson("CurrentCart", cart);

                return Json(new
                {
                    success = true,
                    message = "Remise appliquée",
                    cartSummary = new
                    {
                        itemCount = cart.ItemCount,
                        subtotal = cart.SubTotal,
                        discountAmount = cart.DiscountAmount,
                        finalAmount = cart.FinalAmount,
                        formattedSubTotal = cart.FormattedSubTotal,
                        formattedDiscountAmount = cart.FormattedDiscountAmount,
                        formattedFinalAmount = cart.FormattedFinalAmount
                    }
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "ApplyDiscountError",
                    "Erreur lors de l'application de la remise",
                    CurrentUserId, CurrentCenterId,
                    details: new { Request = request, Error = ex.Message });

                return Json(new { success = false, message = "Une erreur est survenue lors de l'application de la remise" });
            }
        }

        /// <summary>
        /// Associe un patient à la vente
        /// </summary>
        [HttpPost]
        [MedicalStaff]
        public async Task<IActionResult> SetPatient([FromBody] SetPatientRequest request)
        {
            try
            {
                // Récupérer le panier actuel
                var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("CurrentCart") ?? new CartViewModel();

                if (request.PatientId.HasValue)
                {
                    // Vérifier que le patient existe
                    var patient = await _patientService.GetPatientByIdAsync(request.PatientId.Value);
                    if (patient == null)
                    {
                        return Json(new { success = false, message = "Patient introuvable" });
                    }

                    cart.PatientId = request.PatientId;
                    cart.PatientName = $"{patient.FirstName} {patient.LastName}";
                }
                else
                {
                    // Supprimer l'association
                    cart.PatientId = null;
                    cart.PatientName = null;
                }

                // Sauvegarder en session
                HttpContext.Session.SetObjectAsJson("CurrentCart", cart);

                return Json(new
                {
                    success = true,
                    message = request.PatientId.HasValue ? "Patient associé à la vente" : "Patient dissocié de la vente",
                    patientInfo = new
                    {
                        patientId = cart.PatientId,
                        patientName = cart.PatientName
                    }
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "SetPatientError",
                    "Erreur lors de l'association du patient",
                    CurrentUserId, CurrentCenterId,
                    details: new { Request = request, Error = ex.Message });

                return Json(new { success = false, message = "Une erreur est survenue lors de l'association du patient" });
            }
        }

        /// <summary>
        /// Récupère le résumé du panier actuel
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public IActionResult GetCartSummary()
        {
            try
            {
                var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("CurrentCart") ?? new CartViewModel();

                return Json(new
                {
                    success = true,
                    cartSummary = new
                    {
                        itemCount = cart.ItemCount,
                        subtotal = cart.SubTotal,
                        discountAmount = cart.DiscountAmount,
                        finalAmount = cart.FinalAmount,
                        patientId = cart.PatientId,
                        patientName = cart.PatientName,
                        discountReason = cart.DiscountReason,
                        notes = cart.Notes,
                        formattedSubTotal = cart.FormattedSubTotal,
                        formattedDiscountAmount = cart.FormattedDiscountAmount,
                        formattedFinalAmount = cart.FormattedFinalAmount,
                        items = cart.Items.Select(i => new
                        {
                            productId = i.ProductId,
                            productName = i.ProductName,
                            categoryName = i.CategoryName,
                            unitOfMeasure = i.UnitOfMeasure,
                            quantity = i.Quantity,
                            unitPrice = i.UnitPrice,
                            totalPrice = i.TotalPrice,
                            formattedUnitPrice = i.FormattedUnitPrice,
                            formattedTotalPrice = i.FormattedTotalPrice
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = "Une erreur est survenue lors de la récupération du panier" });
            }
        }

        /// <summary>
        /// Réinitialise le panier
        /// </summary>
        [HttpPost]
        [MedicalStaff]
        public IActionResult ResetCart()
        {
            try
            {
                HttpContext.Session.Remove("CurrentCart");

                return Json(new { success = true, message = "Panier réinitialisé" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Une erreur est survenue lors de la réinitialisation du panier" });
            }
        }

        /// <summary>
        /// Met à jour les notes de la vente
        /// </summary>
        [HttpPost]
        [MedicalStaff]
        public IActionResult UpdateNotes([FromBody] UpdateNotesRequest request)
        {
            try
            {
                // Récupérer le panier actuel
                var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>("CurrentCart") ?? new CartViewModel();

                cart.Notes = request.Notes;

                // Sauvegarder en session
                HttpContext.Session.SetObjectAsJson("CurrentCart", cart);

                return Json(new { success = true, message = "Notes mises à jour" });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = "Une erreur est survenue lors de la mise à jour des notes" });
            }
        }

        #endregion

        #region Autres vues

        /// <summary>
        /// Historique des ventes d'un patient
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> PatientSales(int patientId)
        {
            try
            {
                var patient = await _patientService.GetPatientByIdAsync(patientId);
                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient introuvable";
                    return RedirectToAction("Index", "Patient");
                }

                var sales = await _saleService.GetPatientSalesHistoryAsync(patientId);

                var viewModel = new PatientSalesViewModel
                {
                    PatientId = patientId,
                    PatientName = $"{patient.FirstName} {patient.LastName}",
                    Sales = sales
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "PatientSalesError",
                    $"Erreur lors du chargement des ventes du patient {patientId}",
                    CurrentUserId, CurrentCenterId,
                    details: new { PatientId = patientId, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des ventes du patient";
                return RedirectToAction("Details", "Patient", new { id = patientId });
            }
        }

        /// <summary>
        /// Tableau de bord des ventes
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Résumé des 30 derniers jours
                var monthSummary = await _saleService.GetSaleSummaryAsync(
                    CurrentCenterId.Value,
                    DateTime.Now.AddDays(-30),
                    DateTime.Now);

                // Résumé des 7 derniers jours
                var weekSummary = await _saleService.GetSaleSummaryAsync(
                    CurrentCenterId.Value,
                    DateTime.Now.AddDays(-7),
                    DateTime.Now);

                // Résumé du jour
                var todaySummary = await _saleService.GetSaleSummaryAsync(
                    CurrentCenterId.Value,
                    DateTime.Now.Date,
                    DateTime.Now);

                var viewModel = new SaleDashboardViewModel
                {
                    MonthSummary = monthSummary,
                    WeekSummary = weekSummary,
                    TodaySummary = todaySummary
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Sale", "DashboardError",
                    "Erreur lors du chargement du tableau de bord des ventes",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du tableau de bord des ventes";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion
    }

    #region Classes de requête

    /// <summary>
    /// Modèle pour la requête de création de vente
    /// </summary>
    public class CreateSaleRequest
    {
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? DiscountReason { get; set; }
        public string? Notes { get; set; }
        public List<CartItemViewModel> Items { get; set; } = new();
        public bool ImmediatePayment { get; set; }
        public int? PaymentMethodId { get; set; }
        public string? TransactionReference { get; set; }
    }

    /// <summary>
    /// Modèle pour l'ajout d'un produit au panier
    /// </summary>
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }

    /// <summary>
    /// Modèle pour la suppression d'un produit du panier
    /// </summary>
    public class RemoveFromCartRequest
    {
        public int ProductId { get; set; }
    }

    /// <summary>
    /// Modèle pour la mise à jour de la quantité
    /// </summary>
    public class UpdateCartItemRequest
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }

    /// <summary>
    /// Modèle pour l'application d'une remise
    /// </summary>
    public class DiscountRequest
    {
        public decimal DiscountAmount { get; set; }
        public string? DiscountReason { get; set; }
    }

    /// <summary>
    /// Modèle pour l'association d'un patient
    /// </summary>
    public class SetPatientRequest
    {
        public int? PatientId { get; set; }
    }

    /// <summary>
    /// Modèle pour la mise à jour des notes
    /// </summary>
    public class UpdateNotesRequest
    {
        public string? Notes { get; set; }
    }

    #endregion
}

