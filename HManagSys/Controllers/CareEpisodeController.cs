
using HManagSys.Attributes;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers;

[RequireAuthentication]
[RequireCurrentCenter]
public class CareEpisodeController : BaseController
{
    private readonly ICareEpisodeService _careEpisodeService;
    private readonly IPatientService _patientService;
    private readonly IApplicationLogger _logger;
    private readonly IUserRepository _userRepository;
    private readonly IProductService _productService;

    public CareEpisodeController(
        ICareEpisodeService careEpisodeService,
        IPatientService patientService,
        IUserRepository userRepository,
        IApplicationLogger logger,
        IProductService productService)
    {
        _careEpisodeService = careEpisodeService;
        _patientService = patientService;
        _logger = logger;
        _userRepository = userRepository;
        _productService = productService;
    }

    [MedicalStaff]
    public async Task<IActionResult> Index(CareEpisodeFilters? filters = null)
    {
        try
        {
            filters ??= new CareEpisodeFilters();
            filters.HospitalCenterId = CurrentCenterId;

            var (episodes, total) = await _careEpisodeService.GetCareEpisodesAsync(filters);

            var viewModel = new PagedViewModel<CareEpisodeViewModel, CareEpisodeFilters>
            {
                Items = episodes,
                Filters = filters,
                Pagination = new PaginationInfo
                {
                    CurrentPage = filters.PageIndex,
                    PageSize = filters.PageSize,
                    TotalCount = total
                }
            };

            await _logger.LogInfoAsync("CareEpisode", "IndexAccessed",
                "Liste des épisodes de soins consultée",
                CurrentUserId, CurrentCenterId);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisode", "IndexError",
                "Erreur lors du chargement des épisodes de soins",
                CurrentUserId, CurrentCenterId,
                details: new { Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des épisodes de soins";
            return View(new PagedViewModel<CareEpisodeViewModel, CareEpisodeFilters>());
        }
    }

    [MedicalStaff]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var episode = await _careEpisodeService.GetByIdAsync(id);
            if (episode == null)
            {
                TempData["ErrorMessage"] = "Épisode de soins introuvable";
                return RedirectToAction(nameof(Index));
            }

            await _logger.LogInfoAsync("CareEpisode", "DetailsAccessed",
                $"Détails de l'épisode de soins {id} consultés",
                CurrentUserId, CurrentCenterId);

            return View(episode);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisode", "DetailsError",
                $"Erreur lors du chargement des détails de l'épisode {id}",
                CurrentUserId, CurrentCenterId,
                details: new { EpisodeId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des détails de l'épisode";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> Create(int patientId)
    {
        try
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient introuvable";
                return RedirectToAction("Index", "Patient");
            }

            var diagnoses = await _patientService.GetPatientDiagnosesAsync(patientId);
            if (!diagnoses.Any())
            {
                TempData["WarningMessage"] = "Le patient n'a aucun diagnostic. Veuillez d'abord ajouter un diagnostic.";
                return RedirectToAction("AddDiagnosis", "Patient", new { patientId });
            }

            var model = new CreateCareEpisodeViewModel
            {
                PatientId = patientId,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                EpisodeStartDate = DateTime.Now,
                HospitalCenterId = CurrentCenterId.Value,
                DiagnosisOptions = diagnoses.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList(),
                CaregiverOptions = await GetCaregiversAsync()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisode", "CreateGetError",
                "Erreur lors du chargement du formulaire de création",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = patientId, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
            return RedirectToAction("Index", "Patient");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Create(CreateCareEpisodeViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                model.DiagnosisOptions = await GetDiagnosesForPatientAsync(model.PatientId);
                model.CaregiverOptions = await GetCaregiversAsync();
                return View(model);
            }

            model.HospitalCenterId = CurrentCenterId.Value;
            var result = await _careEpisodeService.CreateCareEpisodeAsync(model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Épisode de soins créé avec succès";
                return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
            }

            foreach (var error in result.ValidationErrors)
            {
                ModelState.AddModelError("", error);
            }

            model.DiagnosisOptions = await GetDiagnosesForPatientAsync(model.PatientId);
            model.CaregiverOptions = await GetCaregiversAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisode", "CreatePostError",
                "Erreur lors de la création de l'épisode de soins",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur est survenue lors de la création de l'épisode");
            model.DiagnosisOptions = await GetDiagnosesForPatientAsync(model.PatientId);
            model.CaregiverOptions = await GetCaregiversAsync();
            return View(model);
        }
    }

    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> AddCareService(int episodeId)
    {
        try
        {
            var episode = await _careEpisodeService.GetByIdAsync(episodeId);
            if (episode == null)
            {
                TempData["ErrorMessage"] = "Épisode de soins introuvable";
                return RedirectToAction(nameof(Index));
            }

            if (episode.Status != "Active")
            {
                TempData["ErrorMessage"] = "Impossible d'ajouter un service à un épisode non actif";
                return RedirectToAction(nameof(Details), new { id = episodeId });
            }

            var model = new CreateCareServiceViewModel
            {
                CareEpisodeId = episodeId,
                PatientName = episode.PatientName,
                ServiceDate = DateTime.Now,
                AdministeredBy = CurrentUserId!.Value,
                CareTypeOptions = await GetCareTypesAsync(),
                StaffOptions = await GetCaregiversAsync(),
                AvailableProducts = await GetAvailableProductsAsync()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisode", "AddCareServiceGetError",
                "Erreur lors du chargement du formulaire d'ajout de service",
                CurrentUserId, CurrentCenterId,
                details: new { EpisodeId = episodeId, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
            return RedirectToAction(nameof(Details), new { id = episodeId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> AddCareService(CreateCareServiceViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                model.CareTypeOptions = await GetCareTypesAsync();
                model.StaffOptions = await GetCaregiversAsync();
                model.AvailableProducts = await GetAvailableProductsAsync();
                return View(model);
            }

            var result = await _careEpisodeService.AddCareServiceAsync(model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Service de soins ajouté avec succès";
                return RedirectToAction(nameof(Details), new { id = model.CareEpisodeId });
            }

            foreach (var error in result.ValidationErrors)
            {
                ModelState.AddModelError("", error);
            }

            model.CareTypeOptions = await GetCareTypesAsync();
            model.StaffOptions = await GetCaregiversAsync();
            model.AvailableProducts = await GetAvailableProductsAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisode", "AddCareServicePostError",
                "Erreur lors de l'ajout du service de soins",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur est survenue lors de l'ajout du service");
            model.CareTypeOptions = await GetCareTypesAsync();
            model.StaffOptions = await GetCaregiversAsync();
            model.AvailableProducts = await GetAvailableProductsAsync();
            return View(model);
        }
    }

    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            var episode = await _careEpisodeService.GetByIdAsync(id);
            if (episode == null)
            {
                TempData["ErrorMessage"] = "Épisode de soins introuvable";
                return RedirectToAction(nameof(Index));
            }

            if (episode.Status != "Active")
            {
                TempData["ErrorMessage"] = "Impossible de terminer un épisode non actif";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new CompleteCareEpisodeViewModel
            {
                CareEpisodeId = id,
                PatientName = episode.PatientName,
                CompletionDate = DateTime.Now,
                Notes = "Traitement terminé avec succès."
            };

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisode", "CompleteGetError",
                "Erreur lors du chargement du formulaire de clôture",
                CurrentUserId, CurrentCenterId,
                details: new { EpisodeId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Complete(CompleteCareEpisodeViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _careEpisodeService.CompleteCareEpisodeAsync(model.CareEpisodeId, model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Épisode de soins clôturé avec succès";
                return RedirectToAction(nameof(Details), new { id = model.CareEpisodeId });
            }

            ModelState.AddModelError("", result.ErrorMessage ?? "Erreur lors de la clôture de l'épisode");
            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisode", "CompletePostError",
                "Erreur lors de la clôture de l'épisode",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur est survenue lors de la clôture de l'épisode");
            return View(model);
        }
    }

    // Helper methods for loading dropdown options
    private async Task<List<SelectOption>> GetDiagnosesForPatientAsync(int patientId)
    {
        var diagnoses = await _patientService.GetPatientDiagnosesAsync(patientId);
        return diagnoses.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList();
    }

    private async Task<List<SelectOption>> GetCaregiversAsync()
    {
        return await _userRepository.GetUsersByCenterAsync(CurrentCenterId.Value);

    }

    private async Task<List<SelectOption>> GetCareTypesAsync()
    {
        // This would be implemented using a care type service
        return new List<SelectOption>
        {
            new("1", "Consultation"),
            new("2", "Injection"),
            new("3", "Pansement"),
            new("4", "Perfusion"),
        };
    }

    private async Task<List<ProductViewModel>> GetAvailableProductsAsync()
    {
        // This would be implemented using a product service
        var products = await _productService.SearchProductsAsync(string.Empty, null, CurrentCenterId.Value);

        return products.Select(tt => new ProductViewModel
        {
            Id = tt.Id,
            Name = tt.Name,
            UnitOfMeasure = tt.UnitOfMeasure,
            SellingPrice = tt.SellingPrice
        }).ToList();

    }
}