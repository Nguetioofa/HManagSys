using HManagSys.Attributes;
using HManagSys.Models;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des produits
    /// SuperAdmin pour la configuration, Personnel médical pour la consultation
    /// </summary>
    [RequireAuthentication]
    [RequireCurrentCenter]
    public class ProductController : BaseController
    {
        private readonly IProductService _productService;
        private readonly IProductCategoryService _categoryService;
        private readonly IApplicationLogger _appLogger;

        public ProductController(
            IProductService productService,
            IProductCategoryService categoryService,
            IApplicationLogger appLogger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _appLogger = appLogger;
        }

        /// <summary>
        /// Liste des produits avec filtres et pagination
        /// Accessible à tous les utilisateurs connectés
        /// </summary>
        public async Task<IActionResult> Index(ProductFilters? filters = null)
        {
            try
            {
                filters ??= new ProductFilters();

                var (products, totalCount) = await _productService.GetProductsAsync(filters);
                var statistics = await _productService.GetProductStatisticsAsync(CurrentCenterId);
                var categories = await _categoryService.GetActiveCategoriesForSelectAsync();

                var viewModel = new ProductListViewModel
                {
                    Products = products,
                    Filters = filters,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = filters.PageIndex,
                        PageSize = filters.PageSize,
                        TotalCount = totalCount,
                        //TotalPages = (int)Math.Ceiling((double)totalCount / filters.PageSize)
                    },
                    Statistics = statistics,
                    AvailableCategories = categories
                };

                // Log de l'accès
                await _appLogger.LogInfoAsync("Product", "IndexAccessed",
                    "Consultation de la liste des produits",
                    CurrentUserId, CurrentCenterId);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "IndexError",
                    "Erreur lors du chargement de la liste des produits",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des produits";
                return View(new ProductListViewModel());
            }
        }

        /// <summary>
        /// Détails d'un produit
        /// Accessible à tous les utilisateurs connectés
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var details = await _productService.GetProductDetailsAsync(id, CurrentCenterId);
                if (details == null)
                {
                    TempData["ErrorMessage"] = "Produit introuvable";
                    return RedirectToAction(nameof(Index));
                }

                return View(details);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "DetailsError",
                    $"Erreur lors du chargement des détails du produit {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des détails";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Affichage du formulaire de création
        /// SuperAdmin uniquement
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new CreateProductViewModel
                {
                    AvailableCategories = await _categoryService.GetActiveCategoriesForSelectAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "CreateGetError",
                    "Erreur lors du chargement du formulaire de création",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Traitement de la création d'un produit
        /// SuperAdmin uniquement
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.AvailableCategories = await _categoryService.GetActiveCategoriesForSelectAsync();
                    return View(model);
                }

                var result = await _productService.CreateProductAsync(model, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Produit créé avec succès";
                    return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
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
                        ModelState.AddModelError("", result.ErrorMessage ?? "Erreur lors de la création");
                    }

                    model.AvailableCategories = await _categoryService.GetActiveCategoriesForSelectAsync();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "CreateError",
                    "Erreur lors de la création d'un produit",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur inattendue s'est produite");
                model.AvailableCategories = await _categoryService.GetActiveCategoriesForSelectAsync();
                return View(model);
            }
        }

        /// <summary>
        /// Affichage du formulaire de modification
        /// SuperAdmin uniquement
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Produit introuvable";
                    return RedirectToAction(nameof(Index));
                }

                var model = new EditProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    ProductCategoryId = product.ProductCategoryId,
                    UnitOfMeasure = product.UnitOfMeasure,
                    SellingPrice = product.SellingPrice,
                    IsActive = product.IsActive,
                    AvailableCategories = await _categoryService.GetActiveCategoriesForSelectAsync(),
                    CentersWithStock = product.TotalCentersWithStock,
                    MovementCount = 0 // À calculer si nécessaire
                };

                return View(model);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "EditGetError",
                    $"Erreur lors du chargement du produit {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du produit";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Traitement de la modification d'un produit
        /// SuperAdmin uniquement
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Edit(int id, EditProductViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    TempData["ErrorMessage"] = "Données incohérentes";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    model.AvailableCategories = await _categoryService.GetActiveCategoriesForSelectAsync();
                    return View(model);
                }

                var result = await _productService.UpdateProductAsync(id, model, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Produit modifié avec succès";
                    return RedirectToAction(nameof(Details), new { id = id });
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
                        ModelState.AddModelError("", result.ErrorMessage ?? "Erreur lors de la modification");
                    }

                    model.AvailableCategories = await _categoryService.GetActiveCategoriesForSelectAsync();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "EditPostError",
                    $"Erreur lors de la modification du produit {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = id, Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur inattendue s'est produite");
                model.AvailableCategories = await _categoryService.GetActiveCategoriesForSelectAsync();
                return View(model);
            }
        }

        /// <summary>
        /// Activation/désactivation d'un produit via AJAX
        /// SuperAdmin uniquement
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            try
            {
                var result = await _productService.ToggleProductStatusAsync(id, isActive, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Produit {(isActive ? "activé" : "désactivé")} avec succès"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "Erreur lors du changement de statut"
                    });
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "ToggleStatusError",
                    $"Erreur lors du changement de statut du produit {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = id, IsActive = isActive, Error = ex.Message });

                return Json(new
                {
                    success = false,
                    message = "Une erreur inattendue s'est produite"
                });
            }
        }

        /// <summary>
        /// Suppression d'un produit via AJAX
        /// SuperAdmin uniquement
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Vérifier d'abord si le produit peut être supprimé
                var canDelete = await _productService.CanDeleteProductAsync(id);
                if (!canDelete)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Impossible de supprimer ce produit car il a des mouvements de stock ou est utilisé"
                    });
                }

                var result = await _productService.DeleteProductAsync(id, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Produit supprimé avec succès"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "Erreur lors de la suppression"
                    });
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "DeleteError",
                    $"Erreur lors de la suppression du produit {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = id, Error = ex.Message });

                return Json(new
                {
                    success = false,
                    message = "Une erreur inattendue s'est produite"
                });
            }
        }

        /// <summary>
        /// API pour la recherche de produits (autocomplete)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(string term, int? categoryId = null)
        {
            try
            {
                var products = await _productService.SearchProductsAsync(term ?? "", categoryId, CurrentCenterId);
                return Json(products.Select(p => new {
                    id = p.Id,
                    text = p.DisplayText,
                    category = p.CategoryName,
                    price = p.PriceText,
                    unit = p.UnitOfMeasure
                }).ToList());
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "SearchError",
                    "Erreur lors de la recherche de produits",
                    CurrentUserId, CurrentCenterId,
                    details: new { SearchTerm = term, CategoryId = categoryId, Error = ex.Message });

                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Export des produits en Excel
        /// </summary>
        [SuperAdmin]
        public async Task<IActionResult> Export(ProductFilters? filters = null)
        {
            try
            {
                filters ??= new ProductFilters { PageSize = int.MaxValue };
                var (products, _) = await _productService.GetProductsAsync(filters);

                // Log de l'export
                await _appLogger.LogInfoAsync("Product", "ExportRequested",
                    $"Export de {products.Count} produits",
                    CurrentUserId, CurrentCenterId);

                // Créer le CSV (à remplacer par Excel plus tard avec ClosedXML)
                var csv = GenerateProductsCsv(products);
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

                return File(bytes, "text/csv", $"produits_{DateTime.Now:yyyyMMdd_HHmm}.csv");
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "ExportError",
                    "Erreur lors de l'export des produits",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de l'export des produits";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Historique des mouvements d'un produit
        /// </summary>
        public async Task<IActionResult> MovementHistory(int id, int days = 30)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Produit introuvable" });
                }

                var movements = await _productService.GetProductMovementHistoryAsync(id, CurrentCenterId, days);

                return Json(new
                {
                    success = true,
                    product = product.Name,
                    movements = movements.Select(m => new {
                        date = m.MovementDate.ToString("dd/MM/yyyy HH:mm"),
                        type = m.MovementTypeText,
                        quantity = m.QuantityText,
                        center = m.CenterName,
                        reference = m.ReferenceId,
                        notes = m.Notes,
                        createdBy = m.CreatedByName,
                        icon = m.MovementIcon,
                        quantityClass = m.QuantityClass
                    })
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Product", "MovementHistoryError",
                    $"Erreur lors de la récupération de l'historique du produit {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { ProductId = id, Days = days, Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération de l'historique" });
            }
        }

        /// <summary>
        /// Génère un CSV des produits (temporaire)
        /// </summary>
        private string GenerateProductsCsv(List<ProductViewModel> products)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Nom,Description,Catégorie,Unité,Prix,Statut,Stock faible,Stock critique,Créé par,Date de création");

            foreach (var product in products)
            {
                csv.AppendLine($"\"{product.Name}\"," +
                              $"\"{product.Description ?? ""}\"," +
                              $"\"{product.CategoryName}\"," +
                              $"\"{product.UnitOfMeasure}\"," +
                              $"{product.SellingPrice}," +
                              $"{product.StatusText}," +
                              $"{(product.HasLowStock ? "Oui" : "Non")}," +
                              $"{(product.HasCriticalStock ? "Oui" : "Non")}," +
                              $"\"{product.CreatedByName}\"," +
                              $"{product.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            return csv.ToString();
        }
    }
}