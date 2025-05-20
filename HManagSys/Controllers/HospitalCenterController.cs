using HManagSys.Attributes;
using HManagSys.Helpers;
using HManagSys.Models;
using HManagSys.Models.ViewModels.HospitalCenter;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers
{
    [RequireAuthentication]
    [SuperAdmin]
    public class HospitalCenterController : BaseController
    {
        private readonly IHospitalCenterService _hospitalCenterService;
        private readonly IApplicationLogger _logger;

        public HospitalCenterController(
            IHospitalCenterService hospitalCenterService,
            IApplicationLogger logger)
        {
            _hospitalCenterService = hospitalCenterService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(HospitalCenterFilters filters)
        {
            try
            {
                var (centers, totalCount) = await _hospitalCenterService.GetCentersAsync(filters);
                var statistics = await _hospitalCenterService.GetNetworkStatisticsAsync();

                var viewModel = new HospitalCenterListViewModel
                {
                    Centers = centers,
                    Filters = filters,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = filters.PageIndex,
                        PageSize = filters.PageSize,
                        TotalCount = totalCount
                    },
                    Statistics = statistics
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenter", "IndexError",
                    "Erreur lors du chargement de la liste des centres",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur est survenue lors du chargement des centres";
                return View(new HospitalCenterListViewModel());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var center = await _hospitalCenterService.GetCenterByIdAsync(id);
                if (center == null)
                {
                    TempData["ErrorMessage"] = "Centre introuvable";
                    return RedirectToAction(nameof(Index));
                }

                return View(center);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenter", "DetailsError",
                    $"Erreur lors du chargement des détails du centre {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CenterId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur est survenue lors du chargement des détails";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateHospitalCenterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateHospitalCenterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _hospitalCenterService.CreateCenterAsync(model, CurrentUserId.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Centre hospitalier créé avec succès";
                    return RedirectToAction(nameof(Details), new { id = result.Data.Id });
                }
                else
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenter", "CreateError",
                    "Erreur lors de la création du centre",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur est survenue lors de la création du centre");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var center = await _hospitalCenterService.GetCenterByIdAsync(id);
                if (center == null)
                {
                    TempData["ErrorMessage"] = "Centre introuvable";
                    return RedirectToAction(nameof(Index));
                }

                var model = new EditHospitalCenterViewModel
                {
                    Id = center.Id,
                    Name = center.Name,
                    Address = center.Address,
                    PhoneNumber = center.PhoneNumber,
                    Email = center.Email,
                    IsActive = center.IsActive
                };

                return View(model);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenter", "EditGetError",
                    $"Erreur lors du chargement du formulaire de modification du centre {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CenterId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur est survenue lors du chargement du formulaire";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditHospitalCenterViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    TempData["ErrorMessage"] = "ID du centre invalide";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _hospitalCenterService.UpdateCenterAsync(id, model, CurrentUserId.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Centre hospitalier mis à jour avec succès";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    ModelState.AddModelError("", result.ErrorMessage);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenter", "EditPostError",
                    $"Erreur lors de la mise à jour du centre {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CenterId = id, Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour du centre");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            try
            {
                var result = await _hospitalCenterService.ToggleCenterStatusAsync(id, isActive, CurrentUserId.Value);

                if (result.IsSuccess)
                {
                    var statusText = isActive ? "activé" : "désactivé";
                    TempData["SuccessMessage"] =  $"Centre {statusText} avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenter", "ToggleStatusError",
                    $"Erreur lors du changement de statut du centre {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CenterId = id, IsActive = isActive, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur est survenue lors du changement de statut";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ActivityReport(int id, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var center = await _hospitalCenterService.GetCenterByIdAsync(id);
                if (center == null)
                {
                    TempData["ErrorMessage"] = "Centre introuvable";
                    return RedirectToAction(nameof(Index));
                }

                // Default dates: last 30 days
                var today = TimeZoneHelper.GetCameroonTime().Date;
                var from = fromDate?.Date ?? today.AddDays(-30);
                var to = toDate?.Date ?? today;

                var report = await _hospitalCenterService.GenerateActivityReportAsync(id, from, to);

                ViewBag.CenterName = center.Name;
                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenter", "ActivityReportError",
                    $"Erreur lors de la génération du rapport d'activité pour le centre {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { CenterId = id, FromDate = fromDate, ToDate = toDate, Error = ex.Message });

                TempData["ErrorMessage"] = "Une erreur est survenue lors de la génération du rapport";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}