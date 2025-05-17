using HManagSys.Attributes;
using HManagSys.Models;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers
{
    /// <summary>
    /// Contrôleur pour gérer les transferts de stock entre centres
    /// </summary>
    [RequireAuthentication]
    [RequireCurrentCenter]
    public class TransferController : BaseController
    {
        private readonly ITransferService _transferService;
        private readonly IProductService _productService;
        private readonly IApplicationLogger _appLogger;

        public TransferController(
            ITransferService transferService,
            IProductService productService,
            IApplicationLogger appLogger)
        {
            _transferService = transferService;
            _productService = productService;
            _appLogger = appLogger;
        }

        /// <summary>
        /// Affiche le tableau de bord des transferts
        /// </summary>
        public async Task<IActionResult> Index(TransferFilters? filters = null)
        {
            try
            {
                filters ??= new TransferFilters();

                // Récupérer les transferts selon les filtres
                var (transfers, totalCount) = await _transferService.GetTransfersAsync(
                    filters,
                    CurrentCenterId,
                    CurrentUserId);

                // Préparer les options pour les filtres
                var centers = await _transferService.GetAvailableCentersForTransferAsync();
                var availableProducts = new List<SelectOption>();

                if (CurrentCenterId.HasValue)
                {
                    availableProducts = await _transferService.GetAvailableProductsForTransferAsync(CurrentCenterId.Value);
                }

                // Créer le ViewModel
                var viewModel = new TransfersListViewModel
                {
                    Transfers = transfers,
                    Filters = filters,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = filters.PageIndex,
                        PageSize = filters.PageSize,
                        TotalCount = totalCount,
                       // TotalPages = (int)Math.Ceiling((double)totalCount / filters.PageSize)
                    },
                    AvailableCenters = centers,
                    AvailableProducts = availableProducts,
                    Statistics = await _transferService.GetTransferStatisticsAsync(CurrentCenterId)
                };

                // Liste des transferts en attente d'approbation si SuperAdmin
                if (IsSuperAdmin)
                {
                    viewModel.PendingApprovals = await _transferService.GetTransfersForApprovalAsync(CurrentCenterId!.Value);
                }

                // Log
                await _appLogger.LogInfoAsync("Transfer", "IndexAccessed",
                    "Consultation des transferts",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters });

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "IndexError",
                    "Erreur lors du chargement des transferts",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des transferts";
                return View(new TransfersListViewModel());
            }
        }

        /// <summary>
        /// Affiche le formulaire de demande de transfert (GET)
        /// </summary>
        [SuperAdmin]
        public async Task<IActionResult> Request(int? productId = null)
        {
            try
            {
                var model = new TransferRequestViewModel
                {
                    FromHospitalCenterId = CurrentCenterId!.Value,
                    AvailableCenters = await _transferService.GetAvailableCentersForTransferAsync(CurrentCenterId),
                    AvailableProducts = await _transferService.GetAvailableProductsForTransferAsync(CurrentCenterId!.Value)
                };

                // Présélectionner le produit si fourni
                if (productId.HasValue)
                {
                    model.ProductId = productId.Value;
                    var product = await _productService.GetProductByIdAsync(productId.Value);
                    if (product != null)
                    {
                        model.ProductName = product.Name;
                        model.UnitOfMeasure = product.UnitOfMeasure;
                        model.AvailableQuantity = await _transferService.GetAvailableQuantityAsync(
                            productId.Value, CurrentCenterId!.Value);
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "RequestGetError",
                    "Erreur lors du chargement du formulaire de demande de transfert",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = productId, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Traite la demande de transfert (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Request(TransferRequestViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.AvailableCenters = await _transferService.GetAvailableCentersForTransferAsync(CurrentCenterId);
                    model.AvailableProducts = await _transferService.GetAvailableProductsForTransferAsync(CurrentCenterId!.Value);
                    model.AvailableQuantity = await _transferService.GetAvailableQuantityAsync(
                        model.ProductId, CurrentCenterId!.Value);
                    var product = await _productService.GetProductByIdAsync(model.ProductId);
                    if (product != null)
                    {
                        model.UnitOfMeasure = product.UnitOfMeasure;
                    }
                    return View(model);
                }

                // Créer la demande de transfert
                var result = await _transferService.RequestTransferAsync(model, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Demande de transfert créée avec succès";
                    return RedirectToAction(nameof(Details), new { id = result.Data });
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
                        ModelState.AddModelError("", result.ErrorMessage ?? "Erreur lors de la création de la demande");
                    }

                    // Recharger les listes
                    model.AvailableCenters = await _transferService.GetAvailableCentersForTransferAsync(CurrentCenterId);
                    model.AvailableProducts = await _transferService.GetAvailableProductsForTransferAsync(CurrentCenterId!.Value);
                    model.AvailableQuantity = await _transferService.GetAvailableQuantityAsync(
                        model.ProductId, CurrentCenterId!.Value);
                    var product = await _productService.GetProductByIdAsync(model.ProductId);
                    if (product != null)
                    {
                        model.UnitOfMeasure = product.UnitOfMeasure;
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "RequestPostError",
                    "Erreur lors de la création d'une demande de transfert",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur inattendue s'est produite");

                // Recharger les listes
                model.AvailableCenters = await _transferService.GetAvailableCentersForTransferAsync(CurrentCenterId);
                model.AvailableProducts = await _transferService.GetAvailableProductsForTransferAsync(CurrentCenterId!.Value);

                return View(model);
            }
        }

        /// <summary>
        /// Affiche les détails d'un transfert
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var transfer = await _transferService.GetTransferByIdAsync(id);
                if (transfer == null)
                {
                    TempData["ErrorMessage"] = "Transfert introuvable";
                    return RedirectToAction(nameof(Index));
                }

                // Vérifier si l'utilisateur peut approuver ce transfert
                if (CurrentUserId.HasValue)
                {
                    ViewBag.CanApprove = await _transferService.CanUserApproveTransferAsync(id, CurrentUserId.Value);
                }
                else
                {
                    ViewBag.CanApprove = false;
                }

                // Log
                await _appLogger.LogInfoAsync("Transfer", "DetailsAccessed",
                    $"Consultation des détails du transfert {id}",
                    CurrentUserId, CurrentCenterId);

                return View(transfer);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "DetailsError",
                    $"Erreur lors du chargement des détails du transfert {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { TransferId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des détails";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Approuve un transfert
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Approve(int id, string comments)
        {
            try
            {
                var result = await _transferService.ApproveTransferAsync(id, CurrentUserId!.Value, comments);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Transfert approuvé avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Erreur lors de l'approbation";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "ApproveError",
                    $"Erreur lors de l'approbation du transfert {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { TransferId = id, Comments = comments, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur inattendue s'est produite";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// Rejette un transfert
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            try
            {
                var result = await _transferService.RejectTransferAsync(id, CurrentUserId!.Value, reason);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Transfert rejeté avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Erreur lors du rejet";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "RejectError",
                    $"Erreur lors du rejet du transfert {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { TransferId = id, Reason = reason, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur inattendue s'est produite";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// Complète un transfert (exécute le mouvement de stock)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                var result = await _transferService.CompleteTransferAsync(id, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Transfert complété avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Erreur lors de la complétion";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "CompleteError",
                    $"Erreur lors de la complétion du transfert {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { TransferId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur inattendue s'est produite";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// Annule un transfert
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            try
            {
                var result = await _transferService.CancelTransferAsync(id, CurrentUserId!.Value, reason);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Transfert annulé avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Erreur lors de l'annulation";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "CancelError",
                    $"Erreur lors de l'annulation du transfert {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { TransferId = id, Reason = reason, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur inattendue s'est produite";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// Affiche l'historique des transferts
        /// </summary>
        public async Task<IActionResult> History(TransferFilters? filters = null)
        {
            try
            {
                filters ??= new TransferFilters { Days = 30 };

                var (transfers, totalCount) = await _transferService.GetTransfersAsync(
                    filters,
                    CurrentCenterId,
                    CurrentUserId);

                var centers = await _transferService.GetAvailableCentersForTransferAsync();
                var products = await _productService.GetActiveProductsForSelectAsync();

                var viewModel = new TransferHistoryViewModel
                {
                    Transfers = transfers,
                    Filters = filters,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = filters.PageIndex,
                        PageSize = filters.PageSize,
                        TotalCount = totalCount,
                        //TotalPages = (int)Math.Ceiling((double)totalCount / filters.PageSize)
                    },
                    AvailableCenters = centers,
                    AvailableProducts = products.Select(p => new SelectOption(p.Id.ToString(), p.Name)).ToList(),
                    Statistics = await _transferService.GetTransferStatisticsAsync(CurrentCenterId)
                };

                // Log
                await _appLogger.LogInfoAsync("Transfer", "HistoryAccessed",
                    "Consultation de l'historique des transferts",
                    CurrentUserId, CurrentCenterId);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "HistoryError",
                    "Erreur lors du chargement de l'historique des transferts",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement de l'historique";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// API pour récupérer les statistiques des transferts
        /// </summary>
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var stats = await _transferService.GetTransferStatisticsAsync(CurrentCenterId);
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "StatisticsError",
                    "Erreur lors de la récupération des statistiques de transfert",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des statistiques" });
            }
        }

        /// <summary>
        /// API pour récupérer les infos d'un produit pour un transfert
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProductInfo(int productId)
        {
            try
            {
                if (!CurrentCenterId.HasValue)
                {
                    return Json(new { success = false, message = "Centre non sélectionné" });
                }

                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Produit introuvable" });
                }

                var availableQuantity = await _transferService.GetAvailableQuantityAsync(
                    productId, CurrentCenterId.Value);

                return Json(new
                {
                    success = true,
                    productName = product.Name,
                    unitOfMeasure = product.UnitOfMeasure,
                    availableQuantity = availableQuantity,
                    availableQuantityFormatted = $"{availableQuantity:N2} {product.UnitOfMeasure}"
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Transfer", "GetProductInfoError",
                    "Erreur lors de la récupération des infos du produit",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = productId, Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des infos du produit" });
            }
        }
    }
}