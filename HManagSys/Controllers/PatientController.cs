using HManagSys.Attributes;
using HManagSys.Models;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers;

/// <summary>
/// Contrôleur pour la gestion des patients
/// </summary>
[RequireAuthentication]
[RequireCurrentCenter]
public class PatientController : BaseController
{
    private readonly IPatientService _patientService;
    private readonly IApplicationLogger _appLogger;

    public PatientController(
        IPatientService patientService,
        IApplicationLogger appLogger)
    {
        _patientService = patientService;
        _appLogger = appLogger;
    }

    /// <summary>
    /// Liste des patients avec filtres et pagination
    /// </summary>
    [MedicalStaff]
    public async Task<IActionResult> Index(PatientFilters? filters = null)
    {
        try
        {
            filters ??= new PatientFilters();
            filters.HospitalCenterId = CurrentCenterId;

            var (patients, totalCount) = await _patientService.SearchPatientsAsync(filters);
            var statistics = await _patientService.GetPatientStatisticsAsync(CurrentCenterId.Value);

            var viewModel = new PagedViewModel<PatientViewModel, PatientFilters>
            {
                Items = patients,
                Filters = filters,
                Pagination = new PaginationInfo
                {
                    CurrentPage = filters.PageIndex,
                    PageSize = filters.PageSize,
                    TotalCount = totalCount,
                    //TotalPages = (int)Math.Ceiling((double)totalCount / filters.PageSize)
                },
                ExtraData = statistics
            };

            // Log de l'accès
            await _appLogger.LogInfoAsync("Patient", "IndexAccessed",
                "Consultation de la liste des patients",
                CurrentUserId, CurrentCenterId);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "IndexError",
                "Erreur lors du chargement de la liste des patients",
                CurrentUserId, CurrentCenterId,
                details: new { Filters = filters, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des patients";
            return View(new PagedViewModel<PatientViewModel, PatientFilters>());
        }
    }

    /// <summary>
    /// Affichage du formulaire de création d'un patient
    /// </summary>
    [HttpGet]
    [MedicalStaff]
    public IActionResult Create()
    {
        var model = new CreatePatientViewModel
        {
            GenderOptions = GetGenderOptions(),
            BloodTypeOptions = GetBloodTypeOptions()
        };

        return View(model);
    }

    /// <summary>
    /// Traitement du formulaire de création
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Create(CreatePatientViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                model.GenderOptions = GetGenderOptions();
                model.BloodTypeOptions = GetBloodTypeOptions();
                return View(model);
            }

            // Vérifier les doublons potentiels
            var potentialDuplicates = await _patientService.CheckPotentialDuplicatesAsync(
                model.FirstName, model.LastName, model.PhoneNumber);

            if (potentialDuplicates.Any())
            {
                // Alerter sur les doublons, mais permettre de continuer
                TempData["WarningMessage"] = "Des patients similaires existent déjà. Veuillez vérifier avant de créer un doublon.";
                ViewBag.PotentialDuplicates = potentialDuplicates;

                model.GenderOptions = GetGenderOptions();
                model.BloodTypeOptions = GetBloodTypeOptions();
                return View(model);
            }

            var result = await _patientService.CreatePatientAsync(model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Patient créé avec succès";
                return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
            }
            else
            {
                foreach (var error in result.ValidationErrors)
                {
                    ModelState.AddModelError("", error);
                }

                model.GenderOptions = GetGenderOptions();
                model.BloodTypeOptions = GetBloodTypeOptions();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "CreateError",
                "Erreur lors de la création d'un patient",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur inattendue s'est produite");

            model.GenderOptions = GetGenderOptions();
            model.BloodTypeOptions = GetBloodTypeOptions();
            return View(model);
        }
    }

    /// <summary>
    /// Affichage des détails d'un patient
    /// </summary>
    [MedicalStaff]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var patient = await _patientService.GetPatientDetailsAsync(id);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient introuvable";
                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "DetailsError",
                $"Erreur lors du chargement des détails du patient {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des détails du patient";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Formulaire de modification d'un patient
    /// </summary>
    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient introuvable";
                return RedirectToAction(nameof(Index));
            }

            var model = new EditPatientViewModel
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                PhoneNumber = patient.PhoneNumber,
                Email = patient.Email,
                Address = patient.Address,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                BloodType = patient.BloodType,
                Allergies = patient.Allergies,
                IsActive = patient.IsActive,
                GenderOptions = GetGenderOptions(),
                BloodTypeOptions = GetBloodTypeOptions()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "EditError",
                $"Erreur lors du chargement du formulaire d'édition pour le patient {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire d'édition";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Traitement de la modification d'un patient
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Edit(int id, EditPatientViewModel model)
    {
        try
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Identifiant de patient invalide";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                model.GenderOptions = GetGenderOptions();
                model.BloodTypeOptions = GetBloodTypeOptions();
                return View(model);
            }

            var result = await _patientService.UpdatePatientAsync(id, model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Patient modifié avec succès";
                return RedirectToAction(nameof(Details), new { id });
            }
            else
            {
                foreach (var error in result.ValidationErrors)
                {
                    ModelState.AddModelError("", error);
                }

                model.GenderOptions = GetGenderOptions();
                model.BloodTypeOptions = GetBloodTypeOptions();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "EditPostError",
                $"Erreur lors de la modification du patient {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = id, Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur inattendue s'est produite");

            model.GenderOptions = GetGenderOptions();
            model.BloodTypeOptions = GetBloodTypeOptions();
            return View(model);
        }
    }

    /// <summary>
    /// Change le statut d'un patient (actif/inactif)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> ToggleStatus(int id, bool isActive)
    {
        try
        {
            var result = await _patientService.TogglePatientStatusAsync(id, isActive, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                return Json(new
                {
                    success = true,
                    message = $"Statut du patient modifié avec succès"
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = result.ErrorMessage ?? "Erreur lors de la modification du statut"
                });
            }
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "ToggleStatusError",
                $"Erreur lors de la modification du statut du patient {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = id, IsActive = isActive, Error = ex.Message });

            return Json(new
            {
                success = false,
                message = "Une erreur inattendue s'est produite"
            });
        }
    }

    /// <summary>
    /// Affiche l'historique médical du patient
    /// </summary>
    [MedicalStaff]
    public async Task<IActionResult> History(int id)
    {
        try
        {
            var history = await _patientService.GetPatientHistoryAsync(id);
            if (history == null)
            {
                TempData["ErrorMessage"] = "Historique du patient introuvable";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(history);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "HistoryError",
                $"Erreur lors du chargement de l'historique du patient {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement de l'historique";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// Recherche rapide de patients pour l'autocomplete
    /// </summary>
    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> Search(string term)
    {
        try
        {
            var patients = await _patientService.QuickSearchPatientsAsync(term, CurrentCenterId.Value);
            return Json(patients.Select(p => new
            {
                id = p.Id,
                text = p.Name,
                phone = p.PhoneNumber,
                info = p.AdditionalInfo,
                isActive = p.IsActive
            }));
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "SearchError",
                "Erreur lors de la recherche de patients",
                CurrentUserId, CurrentCenterId,
                details: new { SearchTerm = term, Error = ex.Message });

            return Json(new List<object>());
        }
    }

    /// <summary>
    /// Affiche le formulaire de création de diagnostic
    /// </summary>
    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> AddDiagnosis(int patientId)
    {
        try
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient introuvable";
                return RedirectToAction(nameof(Index));
            }

            var model = new CreateDiagnosisViewModel
            {
                PatientId = patientId,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                DiagnosisDate = DateTime.Now,
                HospitalCenterId = CurrentCenterId.Value,
                SeverityOptions = GetSeverityOptions()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "AddDiagnosisGetError",
                $"Erreur lors du chargement du formulaire de diagnostic pour le patient {patientId}",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = patientId, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire de diagnostic";
            return RedirectToAction(nameof(Details), new { id = patientId });
        }
    }

    /// <summary>
    /// Traite le formulaire de création de diagnostic
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> AddDiagnosis(CreateDiagnosisViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                model.SeverityOptions = GetSeverityOptions();
                return View(model);
            }

            model.HospitalCenterId = CurrentCenterId.Value;
            var result = await _patientService.AddDiagnosisAsync(model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Diagnostic ajouté avec succès";
                return RedirectToAction(nameof(Details), new { id = model.PatientId });
            }
            else
            {
                foreach (var error in result.ValidationErrors)
                {
                    ModelState.AddModelError("", error);
                }

                model.SeverityOptions = GetSeverityOptions();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("Patient", "AddDiagnosisPostError",
                $"Erreur lors de l'ajout d'un diagnostic pour le patient {model.PatientId}",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur inattendue s'est produite");

            model.SeverityOptions = GetSeverityOptions();
            return View(model);
        }
    }

    /// <summary>
    /// Listes d'options pour les formulaires
    /// </summary>
    private static List<SelectOption> GetGenderOptions()
    {
        return new List<SelectOption>
        {
            new("M", "Masculin"),
            new("F", "Féminin"),
        };
    }

    private static List<SelectOption> GetBloodTypeOptions()
    {
        return new List<SelectOption>
        {
            new("A+", "A+"),
            new("A-", "A-"),
            new("B+", "B+"),
            new("B-", "B-"),
            new("AB+", "AB+"),
            new("AB-", "AB-"),
            new("O+", "O+"),
            new("O-", "O-"),
            new("Unknown", "Inconnu")
        };
    }

    private static List<SelectOption> GetSeverityOptions()
    {
        return new List<SelectOption>
        {
            new("Mild", "Légère"),
            new("Moderate", "Modérée"),
            new("Severe", "Sévère"),
            new("Critical", "Critique")
        };
    }
}