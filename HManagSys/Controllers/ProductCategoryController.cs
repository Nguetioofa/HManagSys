using HManagSys.Attributes;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des catégories de produits
    /// Accès SuperAdmin uniquement pour la configuration du système
    /// </summary>
    [RequireAuthentication]
    [RequireCurrentCenter]
    [SuperAdmin]
    public class ProductCategoryController : BaseController
    {
        private readonly IProductCategoryService _categoryService;
        private readonly IApplicationLogger _appLogger;

        public ProductCategoryController(
            IProductCategoryService categoryService,
            IApplicationLogger appLogger)
        {
            _categoryService = categoryService;
            _appLogger = appLogger;
        }

        /// <summary>
        /// Liste des catégories avec filtres et pagination
        /// </summary>
        public async Task<IActionResult> Index(ProductCategoryFilters? filters = null)
        {
            try
            {
                filters ??= new ProductCategoryFilters();

                var (categories, totalCount) = await _categoryService.GetCategoriesAsync(filters);
                var statistics = await _categoryService.GetCategoryStatisticsAsync();

                var viewModel = new ProductCategoryListViewModel
                {
                    Categories = categories,
                    Filters = filters,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = filters.PageIndex,
                        PageSize = filters.PageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / filters.PageSize)
                    },
                    Statistics = statistics
                };

                // Log de l'accès
                await _appLogger.LogInfoAsync("ProductCategory", "IndexAccessed",
                    "Consultation de la liste des catégories de produits",
                    CurrentUserId, CurrentCenterId);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("ProductCategory", "IndexError",
                    "Erreur lors du chargement de la liste des catégories",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des catégories de produits";
                return View(new ProductCategoryListViewModel());
            }
        }

        /// <summary>
        /// Affichage du formulaire de création
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateProductCategoryViewModel();
            return View(model);
        }

        /// <summary>
        /// Traitement de la création d'une catégorie
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductCategoryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _categoryService.CreateCategoryAsync(model, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Catégorie créée avec succès";
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
                        ModelState.AddModelError("", result.ErrorMessage ?? "Erreur lors de la création");
                    }

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("ProductCategory", "CreateError",
                    "Erreur lors de la création d'une catégorie",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur inattendue s'est produite");
                return View(model);
            }
        }

        /// <summary>
        /// Affichage du formulaire de modification
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Catégorie introuvable";
                    return RedirectToAction(nameof(Index));
                }

                var model = new EditProductCategoryViewModel
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    ProductCount = category.ProductCount
                };

                return View(model);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("ProductCategory", "EditGetError",
                    $"Erreur lors du chargement de la catégorie {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CategoryId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement de la catégorie";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Traitement de la modification d'une catégorie
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditProductCategoryViewModel model)
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
                    return View(model);
                }

                var result = await _categoryService.UpdateCategoryAsync(id, model, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Catégorie modifiée avec succès";
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
                        ModelState.AddModelError("", result.ErrorMessage ?? "Erreur lors de la modification");
                    }

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("ProductCategory", "EditPostError",
                    $"Erreur lors de la modification de la catégorie {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CategoryId = id, Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur inattendue s'est produite");
                return View(model);
            }
        }

        /// <summary>
        /// Activation/désactivation d'une catégorie via AJAX
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            try
            {
                var result = await _categoryService.ToggleCategoryStatusAsync(id, isActive, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Catégorie {(isActive ? "activée" : "désactivée")} avec succès"
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
                await _appLogger.LogErrorAsync("ProductCategory", "ToggleStatusError",
                    $"Erreur lors du changement de statut de la catégorie {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CategoryId = id, IsActive = isActive, Error = ex.Message });

                return Json(new
                {
                    success = false,
                    message = "Une erreur inattendue s'est produite"
                });
            }
        }

        /// <summary>
        /// Suppression d'une catégorie via AJAX
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Vérifier d'abord si la catégorie peut être supprimée
                var canDelete = await _categoryService.CanDeleteCategoryAsync(id);
                if (!canDelete)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Impossible de supprimer cette catégorie car elle contient des produits"
                    });
                }

                var result = await _categoryService.DeleteCategoryAsync(id, CurrentUserId!.Value);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Catégorie supprimée avec succès"
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
                await _appLogger.LogErrorAsync("ProductCategory", "DeleteError",
                    $"Erreur lors de la suppression de la catégorie {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CategoryId = id, Error = ex.Message });

                return Json(new
                {
                    success = false,
                    message = "Une erreur inattendue s'est produite"
                });
            }
        }

        /// <summary>
        /// Détails d'une catégorie
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Catégorie introuvable";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("ProductCategory", "DetailsError",
                    $"Erreur lors du chargement des détails de la catégorie {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CategoryId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des détails";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// API pour la recherche de catégories (autocomplete)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            try
            {
                var categories = await _categoryService.SearchCategoriesAsync(term ?? "");
                return Json(categories.Select(c => new {
                    id = c.Id,
                    text = c.Name
                }).ToList());
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("ProductCategory", "SearchError",
                    "Erreur lors de la recherche de catégories",
                    CurrentUserId, CurrentCenterId,
                    details: new { SearchTerm = term, Error = ex.Message });

                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Export des catégories en Excel
        /// </summary>
        public async Task<IActionResult> Export()
        {
            try
            {
                var filters = new ProductCategoryFilters { PageSize = int.MaxValue };
                var (categories, _) = await _categoryService.GetCategoriesAsync(filters);

                // Log de l'export
                await _appLogger.LogInfoAsync("ProductCategory", "ExportRequested",
                    $"Export de {categories.Count} catégories de produits",
                    CurrentUserId, CurrentCenterId);

                // Créer le CSV (à remplacer par Excel plus tard avec ClosedXML)
                var csv = GenerateCategoriesCsv(categories);
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

                return File(bytes, "text/csv", $"categories_produits_{DateTime.Now:yyyyMMdd_HHmm}.csv");
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("ProductCategory", "ExportError",
                    "Erreur lors de l'export des catégories",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de l'export des catégories";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Génère un CSV des catégories (temporaire)
        /// </summary>
        private string GenerateCategoriesCsv(List<ProductCategoryViewModel> categories)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Nom,Description,Statut,Nombre de produits,Créé par,Date de création");

            foreach (var category in categories)
            {
                csv.AppendLine($"\"{category.Name}\"," +
                              $"\"{category.Description ?? ""}\"," +
                              $"{category.StatusText}," +
                              $"{category.ProductCount}," +
                              $"\"{category.CreatedByName}\"," +
                              $"{category.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            return csv.ToString();
        }
    }
}