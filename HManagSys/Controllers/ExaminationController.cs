using HManagSys.Attributes;
using HManagSys.Models;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers;

[RequireAuthentication]
[RequireCurrentCenter]
public class ExaminationController : BaseController
{
    private readonly IExaminationService _examinationService;
    private readonly IPatientService _patientService;
    private readonly ICareEpisodeService _careEpisodeService;
    private readonly IApplicationLogger _logger;

    public ExaminationController(
        IExaminationService examinationService,
        IPatientService patientService,
        ICareEpisodeService careEpisodeService,
        IApplicationLogger logger)
    {
        _examinationService = examinationService;
        _patientService = patientService;
        _careEpisodeService = careEpisodeService;
        _logger = logger;
    }

    [MedicalStaff]
    public async Task<IActionResult> Index(ExaminationFilters? filters = null)
    {
        try
        {
            filters ??= new ExaminationFilters();
            filters.HospitalCenterId = CurrentCenterId;

            var (examinations, total) = await _examinationService.GetExaminationsAsync(filters);

            var viewModel = new PagedViewModel<ExaminationViewModel, ExaminationFilters>
            {
                Items = examinations,
                Filters = filters,
                Pagination = new PaginationInfo
                {
                    CurrentPage = filters.PageIndex,
                    PageSize = filters.PageSize,
                    TotalCount = total
                }
            };

            await _logger.LogInfoAsync("Examination", "IndexAccessed",
                "Liste des examens consultée",
                CurrentUserId, CurrentCenterId);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Examination", "IndexError",
                "Erreur lors du chargement des examens",
                CurrentUserId, CurrentCenterId,
                details: new { Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des examens";
            return View(new PagedViewModel<ExaminationViewModel, ExaminationFilters>());
        }
    }

    [MedicalStaff]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var examination = await _examinationService.GetByIdAsync(id);
            if (examination == null)
            {
                TempData["ErrorMessage"] = "Examen introuvable";
                return RedirectToAction(nameof(Index));
            }

            await _logger.LogInfoAsync("Examination", "DetailsAccessed",
                $"Détails de l'examen {id} consultés",
                CurrentUserId, CurrentCenterId);

            return View(examination);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Examination", "DetailsError",
                $"Erreur lors du chargement des détails de l'examen {id}",
                CurrentUserId, CurrentCenterId,
                details: new { ExaminationId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des détails de l'examen";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> Create(int patientId, int? careEpisodeId = null)
    {
        try
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient introuvable";
                return RedirectToAction("Index", "Patient");
            }

            var model = new CreateExaminationViewModel
            {
                PatientId = patientId,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                CareEpisodeId = careEpisodeId,
                RequestDate = DateTime.Now,
                HospitalCenterId = CurrentCenterId.Value,
                ExaminationTypeOptions = await GetExaminationTypesAsync()
            };

            if (careEpisodeId.HasValue)
            {
                var episode = await _careEpisodeService.GetByIdAsync(careEpisodeId.Value);
                if (episode != null)
                {
                    model.CareEpisodeName = $"Épisode du {episode.EpisodeStartDate:dd/MM/yyyy}";
                }
            }

            model.CareEpisodeOptions = await GetCareEpisodesForPatientAsync(patientId);

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Examination", "CreateGetError",
                "Erreur lors du chargement du formulaire de création",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = patientId, CareEpisodeId = careEpisodeId, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
            return RedirectToAction("Details", "Patient", new { id = patientId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Create(CreateExaminationViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                model.ExaminationTypeOptions = await GetExaminationTypesAsync();
                model.CareEpisodeOptions = await GetCareEpisodesForPatientAsync(model.PatientId);
                return View(model);
            }

            model.HospitalCenterId = CurrentCenterId.Value;
            var result = await _examinationService.CreateExaminationAsync(model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Examen créé avec succès";
                return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
            }

            foreach (var error in result.ValidationErrors)
            {
                ModelState.AddModelError("", error);
            }

            model.ExaminationTypeOptions = await GetExaminationTypesAsync();
            model.CareEpisodeOptions = await GetCareEpisodesForPatientAsync(model.PatientId);
            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Examination", "CreatePostError",
                "Erreur lors de la création de l'examen",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur est survenue lors de la création de l'examen");
            model.ExaminationTypeOptions = await GetExaminationTypesAsync();
            model.CareEpisodeOptions = await GetCareEpisodesForPatientAsync(model.PatientId);
            return View(model);
        }
    }

    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> Schedule(int id)
    {
        try
        {
            var examination = await _examinationService.GetByIdAsync(id);
            if (examination == null)
            {
                TempData["ErrorMessage"] = "Examen introuvable";
                return RedirectToAction(nameof(Index));
            }

            if (examination.Status != "Requested")
            {
                TempData["ErrorMessage"] = "Impossible de planifier un examen qui n'est pas en statut 'Demandé'";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new ScheduleExaminationViewModel
            {
                ExaminationId = id,
                PatientName = examination.PatientName,
                ExaminationTypeName = examination.ExaminationTypeName,
                ScheduledDate = DateTime.Now.AddDays(1),
                PerformedBy = CurrentUserId!.Value,
                PerformedByOptions = await GetStaffOptionsAsync()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Examination", "ScheduleGetError",
                "Erreur lors du chargement du formulaire de planification",
                CurrentUserId, CurrentCenterId,
                details: new { ExaminationId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Schedule(ScheduleExaminationViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                model.PerformedByOptions = await GetStaffOptionsAsync();
                return View(model);
            }

            var result = await _examinationService.ScheduleExaminationAsync(model.ExaminationId, model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Examen planifié avec succès";
                return RedirectToAction(nameof(Details), new { id = model.ExaminationId });
            }

            ModelState.AddModelError("", result.ErrorMessage ?? "Erreur lors de la planification de l'examen");
            model.PerformedByOptions = await GetStaffOptionsAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Examination", "SchedulePostError",
                "Erreur lors de la planification de l'examen",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur est survenue lors de la planification de l'examen");
            model.PerformedByOptions = await GetStaffOptionsAsync();
            return View(model);
        }
    }

    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> AddResult(int id)
    {
        try
        {
            var examination = await _examinationService.GetByIdAsync(id);
            if (examination == null)
            {
                TempData["ErrorMessage"] = "Examen introuvable";
                return RedirectToAction(nameof(Index));
            }

            if (examination.Status != "Scheduled")
            {
                TempData["ErrorMessage"] = "Impossible d'ajouter un résultat à un examen non planifié";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (examination.Result != null)
            {
                TempData["ErrorMessage"] = "Cet examen a déjà un résultat";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new CreateExaminationResultViewModel
            {
                ExaminationId = id,
                PatientName = examination.PatientName,
                ExaminationTypeName = examination.ExaminationTypeName,
                ReportDate = DateTime.Now
            };

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Examination", "AddResultGetError",
                "Erreur lors du chargement du formulaire d'ajout de résultat",
                CurrentUserId, CurrentCenterId,
                details: new { ExaminationId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> AddResult(CreateExaminationResultViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _examinationService.AddExaminationResultAsync(model.ExaminationId, model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Résultat d'examen ajouté avec succès";
                return RedirectToAction(nameof(Details), new { id = model.ExaminationId });
            }

            ModelState.AddModelError("", result.ErrorMessage ?? "Erreur lors de l'ajout du résultat");
            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Examination", "AddResultPostError",
                "Erreur lors de l'ajout du résultat d'examen",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur est survenue lors de l'ajout du résultat");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Cancel(int id, string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Json(new { success = false, message = "La raison d'annulation est obligatoire" });
            }

            var result = await _examinationService.CancelExaminationAsync(id, reason, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                return Json(new { success = true, message = "Examen annulé avec succès" });
            }

            return Json(new { success = false, message = result.ErrorMessage ?? "Erreur lors de l'annulation de l'examen" });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Examination", "CancelError",
                "Erreur lors de l'annulation de l'examen",
                CurrentUserId, CurrentCenterId,
                details: new { ExaminationId = id, Reason = reason, Error = ex.Message });

            return Json(new { success = false, message = "Une erreur est survenue lors de l'annulation de l'examen" });
        }
    }

    // Helper methods for loading dropdown options
    private async Task<List<SelectOption>> GetExaminationTypesAsync()
    {
        // This would be implemented using an examination type service
        return new List<SelectOption>
        {
            new("1", "Radiographie"),
            new("2", "Analyse de sang"),
            new("3", "Échographie"),
            new("4", "Scanner"),
            new("5", "Électrocardiogramme")
        };
    }

    private async Task<List<SelectOption>> GetCareEpisodesForPatientAsync(int patientId)
    {
        var episodes = await _careEpisodeService.GetPatientCareEpisodesAsync(patientId);
        return episodes
            .Where(e => e.Status == "Active")
            .Select(e => new SelectOption(
                e.Id.ToString(),
                $"Épisode du {e.EpisodeStartDate:dd/MM/yyyy} - {e.DiagnosisName}")
            )
            .ToList();
    }

    private async Task<List<SelectOption>> GetStaffOptionsAsync()
    {
        // This would be implemented using a user/staff service
        return new List<SelectOption>
        {
            new("1", "Dr. Martin"),
            new("2", "Dr. Kamga"),
            new("3", "Infirmière Ngo"),
            new("4", "Technicien Foka")
        };
    }
}