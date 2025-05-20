using HManagSys.Attributes;
using HManagSys.Models;
using HManagSys.Models.ViewModels.Finance;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers
{
    [RequireAuthentication]
    [RequireCurrentCenter]
    public class FinancierController : BaseController
    {
        private readonly IFinancierService _financierService;
        private readonly IApplicationLogger _logger;

        public FinancierController(
            IFinancierService financierService,
            IApplicationLogger logger)
        {
            _financierService = financierService;
            _logger = logger;
        }

        /// <summary>
        /// Liste des financiers avec filtres et pagination
        /// </summary>
        [SuperAdmin]
        public async Task<IActionResult> Index(FinancierFilters? filters = null)
        {
            try
            {
                filters ??= new FinancierFilters();
                filters.HospitalCenterId = CurrentCenterId;

                var (financiers, totalCount) = await _financierService.GetFinanciersAsync(filters);

                var viewModel = new PagedViewModel<FinancierViewModel, FinancierFilters>
                {
                    Items = financiers,
                    Filters = filters,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = filters.PageIndex,
                        PageSize = filters.PageSize,
                        TotalCount = totalCount
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Financier", "IndexError",
                    "Erreur lors du chargement de la liste des financiers",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des financiers";
                return View(new PagedViewModel<FinancierViewModel, FinancierFilters>());
            }
        }

        /// <summary>
        /// Détails d'un financier
        /// </summary>
        [SuperAdmin]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var financier = await _financierService.GetByIdAsync(id);
                if (financier == null)
                {
                    TempData["ErrorMessage"] = "Financier introuvable";
                    return RedirectToAction(nameof(Index));
                }

                return View(financier);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Financier", "DetailsError",
                    $"Erreur lors du chargement des détails du financier {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { FinancierId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des détails du financier";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Formulaire de création d'un financier
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public IActionResult Create()
        {
            var model = new CreateFinancierViewModel
            {
                HospitalCenterId = CurrentCenterId.Value,
                IsActive = true
            };

            return View(model);
        }

        /// <summary>
        /// Traitement du formulaire de création d'un financier
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Create(CreateFinancierViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.HospitalCenterId = CurrentCenterId.Value;

                var result = await _financierService.CreateFinancierAsync(model, CurrentUserId.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Financier créé avec succès";
                    return RedirectToAction(nameof(Details), new { id = result.Data.Id });
                }

                ModelState.AddModelError("", result.ErrorMessage);
                return View(model);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Financier", "CreateError",
                    "Erreur lors de la création du financier",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur est survenue lors de la création du financier");
                return View(model);
            }
        }

        /// <summary>
        /// Formulaire de modification d'un financier
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var financier = await _financierService.GetByIdAsync(id);
                if (financier == null)
                {
                    TempData["ErrorMessage"] = "Financier introuvable";
                    return RedirectToAction(nameof(Index));
                }

                var model = new EditFinancierViewModel
                {
                    Id = financier.Id,
                    Name = financier.Name,
                    ContactInfo = financier.ContactInfo,
                    IsActive = financier.IsActive
                };

                return View(model);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Financier", "EditGetError",
                    $"Erreur lors du chargement du formulaire de modification du financier {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { FinancierId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire de modification";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Traitement du formulaire de modification d'un financier
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Edit(int id, EditFinancierViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    TempData["ErrorMessage"] = "ID du financier invalide";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _financierService.UpdateFinancierAsync(id, model, CurrentUserId.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Financier mis à jour avec succès";
                    return RedirectToAction(nameof(Details), new { id });
                }

                ModelState.AddModelError("", result.ErrorMessage);
                return View(model);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Financier", "EditPostError",
                    $"Erreur lors de la mise à jour du financier {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour du financier");
                return View(model);
            }
        }

        /// <summary>
        /// Activation/désactivation d'un financier
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            try
            {
                var result = await _financierService.ToggleFinancierStatusAsync(id, isActive, CurrentUserId.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = $"Financier {(isActive ? "activé" : "désactivé")} avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Financier", "ToggleStatusError",
                    $"Erreur lors du changement de statut du financier {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { FinancierId = id, IsActive = isActive, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur est survenue lors du changement de statut du financier";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// Liste des financiers actifs pour une liste déroulante (AJAX)
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> GetActiveFinanciers()
        {
            try
            {
                var financiers = await _financierService.GetActiveFinanciersSelectAsync(CurrentCenterId.Value);
                return Json(financiers);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Financier", "GetActiveFinanciersError",
                    "Erreur lors de la récupération des financiers actifs",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { error = "Une erreur est survenue lors de la récupération des financiers" });
            }
        }
    }
}