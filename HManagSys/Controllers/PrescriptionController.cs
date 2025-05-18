using HManagSys.Attributes;
using HManagSys.Models;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers;

[RequireAuthentication]
[RequireCurrentCenter]
public class PrescriptionController : BaseController
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IPatientService _patientService;
    private readonly IProductService _productService;
    private readonly ICareEpisodeService _careEpisodeService;
    private readonly IApplicationLogger _logger;

    public PrescriptionController(
        IPrescriptionService prescriptionService,
        IPatientService patientService,
        IProductService productService,
        ICareEpisodeService careEpisodeService,
        IApplicationLogger logger)
    {
        _prescriptionService = prescriptionService;
        _patientService = patientService;
        _productService = productService;
        _careEpisodeService = careEpisodeService;
        _logger = logger;
    }

    [MedicalStaff]
    public async Task<IActionResult> Index(PrescriptionFilters? filters = null)
    {
        try
        {
            filters ??= new PrescriptionFilters();
            filters.HospitalCenterId = CurrentCenterId;

            var (prescriptions, total) = await _prescriptionService.GetPrescriptionsAsync(filters);

            var viewModel = new PagedViewModel<PrescriptionViewModel, PrescriptionFilters>
            {
                Items = prescriptions,
                Filters = filters,
                Pagination = new PaginationInfo
                {
                    CurrentPage = filters.PageIndex,
                    PageSize = filters.PageSize,
                    TotalCount = total
                }
            };

            await _logger.LogInfoAsync("Prescription", "IndexAccessed",
                "Liste des prescriptions consultée",
                CurrentUserId, CurrentCenterId);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Prescription", "IndexError",
                "Erreur lors du chargement des prescriptions",
                CurrentUserId, CurrentCenterId,
                details: new { Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des prescriptions";
            return View(new PagedViewModel<PrescriptionViewModel, PrescriptionFilters>());
        }
    }

    [MedicalStaff]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            PrescriptionViewModel? prescription = await _prescriptionService.GetByIdAsync(id);
            if (prescription == null)
            {
                TempData["ErrorMessage"] = "Prescription introuvable";
                return RedirectToAction(nameof(Index));
            }

            await _logger.LogInfoAsync("Prescription", "DetailsAccessed",
                $"Détails de la prescription {id} consultés",
                CurrentUserId, CurrentCenterId);

            return View(prescription);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Prescription", "DetailsError",
                $"Erreur lors du chargement des détails de la prescription {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PrescriptionId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des détails de la prescription";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> Create(int patientId, int? diagnosisId = null, int? careEpisodeId = null)
    {
        try
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient introuvable";
                return RedirectToAction("Index", "Patient");
            }

            var model = new CreatePrescriptionViewModel
            {
                PatientId = patientId,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                DiagnosisId = diagnosisId,
                CareEpisodeId = careEpisodeId,
                PrescriptionDate = DateTime.Now,
                HospitalCenterId = CurrentCenterId.Value,
                Status = "Pending",
                Items = new List<CreatePrescriptionItemViewModel>()
            };

            // Ajouter un premier item vide
            model.Items.Add(new CreatePrescriptionItemViewModel());

            // Charger les diagnostics
            var diagnoses = await _patientService.GetPatientDiagnosesAsync(patientId);
            model.DiagnosisOptions = diagnoses.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList();

            // Charger les épisodes de soins actifs
            var episodes = await _careEpisodeService.GetPatientCareEpisodesAsync(patientId);
            model.CareEpisodeOptions = episodes
                .Where(e => e.Status == "Active")
                .Select(e => new SelectOption(e.Id.ToString(), $"Épisode du {e.EpisodeStartDate:dd/MM/yyyy}"))
                .ToList();

            // Charger les produits disponibles
            model.AvailableProducts = await GetAvailableProductsForPrescriptionAsync();

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Prescription", "CreateGetError",
                "Erreur lors du chargement du formulaire de création",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = patientId, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
            return RedirectToAction("Details", "Patient", new { id = patientId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Create(CreatePrescriptionViewModel model)
    {
        try
        {
            // Filtrer les items vides
            model.Items = model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();

            if (!ModelState.IsValid || !model.Items.Any())
            {
                if (!model.Items.Any())
                {
                    ModelState.AddModelError("", "La prescription doit contenir au moins un produit");
                }

                // Recharger les listes pour le formulaire
                var diagnoses = await _patientService.GetPatientDiagnosesAsync(model.PatientId);
                model.DiagnosisOptions = diagnoses.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList();

                var episodes = await _careEpisodeService.GetPatientCareEpisodesAsync(model.PatientId);
                model.CareEpisodeOptions = episodes
                    .Where(e => e.Status == "Active")
                    .Select(e => new SelectOption(e.Id.ToString(), $"Épisode du {e.EpisodeStartDate:dd/MM/yyyy}"))
                    .ToList();

                model.AvailableProducts = await GetAvailableProductsForPrescriptionAsync();

                return View(model);
            }

            model.HospitalCenterId = CurrentCenterId.Value;
            var result = await _prescriptionService.CreatePrescriptionAsync(model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Prescription créée avec succès";
                return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
            }

            // En cas d'erreur
            foreach (var error in result.ValidationErrors)
            {
                ModelState.AddModelError("", error);
            }

            // Recharger les listes pour le formulaire
            var diagnosesRetry = await _patientService.GetPatientDiagnosesAsync(model.PatientId);
            model.DiagnosisOptions = diagnosesRetry.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList();

            var episodesRetry = await _careEpisodeService.GetPatientCareEpisodesAsync(model.PatientId);
            model.CareEpisodeOptions = episodesRetry
                .Where(e => e.Status == "Active")
                .Select(e => new SelectOption(e.Id.ToString(), $"Épisode du {e.EpisodeStartDate:dd/MM/yyyy}"))
                .ToList();

            model.AvailableProducts = await GetAvailableProductsForPrescriptionAsync();

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Prescription", "CreatePostError",
                "Erreur lors de la création de la prescription",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur est survenue lors de la création de la prescription");

            // Recharger les listes pour le formulaire
            var diagnoses = await _patientService.GetPatientDiagnosesAsync(model.PatientId);
            model.DiagnosisOptions = diagnoses.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList();

            var episodes = await _careEpisodeService.GetPatientCareEpisodesAsync(model.PatientId);
            model.CareEpisodeOptions = episodes
                .Where(e => e.Status == "Active")
                .Select(e => new SelectOption(e.Id.ToString(), $"Épisode du {e.EpisodeStartDate:dd/MM/yyyy}"))
                .ToList();

            model.AvailableProducts = await GetAvailableProductsForPrescriptionAsync();

            return View(model);
        }
    }

    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var prescription = await _prescriptionService.GetByIdAsync(id);
            if (prescription == null)
            {
                TempData["ErrorMessage"] = "Prescription introuvable";
                return RedirectToAction(nameof(Index));
            }

            if (prescription.Status == "Dispensed")
            {
                TempData["ErrorMessage"] = "Impossible de modifier une prescription déjà dispensée";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new EditPrescriptionViewModel
            {
                Id = prescription.Id,
                PatientId = prescription.PatientId,
                PatientName = prescription.PatientName,
                DiagnosisId = prescription.DiagnosisId,
                CareEpisodeId = prescription.CareEpisodeId,
                Instructions = prescription.Instructions,
                PrescriptionDate = prescription.PrescriptionDate,
                HospitalCenterId = prescription.HospitalCenterId,
                Status = prescription.Status,
                Items = prescription.Items.Select(i => new EditPrescriptionItemViewModel
                {
                    Id = i.Id,
                    PrescriptionId = i.PrescriptionId,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    Dosage = i.Dosage,
                    Frequency = i.Frequency,
                    Duration = i.Duration,
                    Instructions = i.Instructions
                }).ToList()
            };

            // Charger les diagnostics
            var diagnoses = await _patientService.GetPatientDiagnosesAsync(prescription.PatientId);
            model.DiagnosisOptions = diagnoses.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList();

            // Charger les épisodes de soins actifs
            var episodes = await _careEpisodeService.GetPatientCareEpisodesAsync(prescription.PatientId);
            model.CareEpisodeOptions = episodes
                .Where(e => e.Status == "Active")
                .Select(e => new SelectOption(e.Id.ToString(), $"Épisode du {e.EpisodeStartDate:dd/MM/yyyy}"))
                .ToList();

            // Charger les produits disponibles
            model.AvailableProducts = await GetAvailableProductsForPrescriptionAsync();

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Prescription", "EditGetError",
                "Erreur lors du chargement du formulaire de modification",
                CurrentUserId, CurrentCenterId,
                details: new { PrescriptionId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Edit(int id, EditPrescriptionViewModel model)
    {
        try
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Identifiant de prescription invalide";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                // Recharger les listes pour le formulaire
                var diagnoses = await _patientService.GetPatientDiagnosesAsync(model.PatientId);
                model.DiagnosisOptions = diagnoses.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList();

                var episodes = await _careEpisodeService.GetPatientCareEpisodesAsync(model.PatientId);
                model.CareEpisodeOptions = episodes
                    .Where(e => e.Status == "Active")
                    .Select(e => new SelectOption(e.Id.ToString(), $"Épisode du {e.EpisodeStartDate:dd/MM/yyyy}"))
                    .ToList();

                model.AvailableProducts = await GetAvailableProductsForPrescriptionAsync();

                return View(model);
            }

            var result = await _prescriptionService.UpdatePrescriptionAsync(id, model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Prescription modifiée avec succès";
                return RedirectToAction(nameof(Details), new { id });
            }

            // En cas d'erreur
            foreach (var error in result.ValidationErrors)
            {
                ModelState.AddModelError("", error);
            }

            // Recharger les listes pour le formulaire
            var diagnosesRetry = await _patientService.GetPatientDiagnosesAsync(model.PatientId);
            model.DiagnosisOptions = diagnosesRetry.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList();

            var episodesRetry = await _careEpisodeService.GetPatientCareEpisodesAsync(model.PatientId);
            model.CareEpisodeOptions = episodesRetry
                .Where(e => e.Status == "Active")
                .Select(e => new SelectOption(e.Id.ToString(), $"Épisode du {e.EpisodeStartDate:dd/MM/yyyy}"))
                .ToList();

            model.AvailableProducts = await GetAvailableProductsForPrescriptionAsync();

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Prescription", "EditPostError",
                $"Erreur lors de la modification de la prescription {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PrescriptionId = id, Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur est survenue lors de la modification de la prescription");

            // Recharger les listes pour le formulaire
            var diagnoses = await _patientService.GetPatientDiagnosesAsync(model.PatientId);
            model.DiagnosisOptions = diagnoses.Select(d => new SelectOption(d.Id.ToString(), d.DiagnosisName)).ToList();

            var episodes = await _careEpisodeService.GetPatientCareEpisodesAsync(model.PatientId);
            model.CareEpisodeOptions = episodes
                .Where(e => e.Status == "Active")
                .Select(e => new SelectOption(e.Id.ToString(), $"Épisode du {e.EpisodeStartDate:dd/MM/yyyy}"))
                .ToList();

            model.AvailableProducts = await GetAvailableProductsForPrescriptionAsync();

            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Dispense(int id)
    {
        try
        {
            var result = await _prescriptionService.DisposePrescriptionAsync(id, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Prescription dispensée avec succès";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["ErrorMessage"] = result.ErrorMessage ?? "Erreur lors de la dispensation de la prescription";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Prescription", "DispenseError",
                $"Erreur lors de la dispensation de la prescription {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PrescriptionId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Une erreur est survenue lors de la dispensation de la prescription";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> AddItem(CreatePrescriptionItemViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Données invalides" });
            }

            var result = await _prescriptionService.AddPrescriptionItemAsync(model.PrescriptionId, model, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                return Json(new
                {
                    success = true,
                    message = "Produit ajouté avec succès",
                    item = result.Data
                });
            }

            return Json(new
            {
                success = false,
                message = result.ErrorMessage ?? "Erreur lors de l'ajout du produit"
            });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Prescription", "AddItemError",
                $"Erreur lors de l'ajout d'un produit à la prescription {model.PrescriptionId}",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            return Json(new
            {
                success = false,
                message = "Une erreur est survenue lors de l'ajout du produit"
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> RemoveItem(int id)
    {
        try
        {
            var result = await _prescriptionService.RemovePrescriptionItemAsync(id, CurrentUserId!.Value);

            if (result.IsSuccess)
            {
                return Json(new { success = true, message = "Produit supprimé avec succès" });
            }

            return Json(new
            {
                success = false,
                message = result.ErrorMessage ?? "Erreur lors de la suppression du produit"
            });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Prescription", "RemoveItemError",
                $"Erreur lors de la suppression de l'item {id}",
                CurrentUserId, CurrentCenterId,
                details: new { ItemId = id, Error = ex.Message });

            return Json(new
            {
                success = false,
                message = "Une erreur est survenue lors de la suppression du produit"
            });
        }
    }

    // Méthode helper pour charger les produits disponibles
    private async Task<List<ProductViewModel>> GetAvailableProductsForPrescriptionAsync()
    {
        // Cette méthode serait implémentée avec le ProductService
        // Pour l'instant, retournons des données fictives
        return new List<ProductViewModel>
        {
            new() { Id = 1, Name = "Paracétamol 500mg", UnitOfMeasure = "boîte", SellingPrice = 1500 },
            new() { Id = 2, Name = "Amoxicilline 500mg", UnitOfMeasure = "boîte", SellingPrice = 3000 },
            new() { Id = 3, Name = "Ibuprofène 400mg", UnitOfMeasure = "boîte", SellingPrice = 2000 },
            new() { Id = 4, Name = "Métronidazole 500mg", UnitOfMeasure = "boîte", SellingPrice = 2500 },
            new() { Id = 5, Name = "Aspirine 500mg", UnitOfMeasure = "boîte", SellingPrice = 1000 },
            new() { Id = 6, Name = "Cotrimoxazole 960mg", UnitOfMeasure = "boîte", SellingPrice = 3500 }
        };
    }
}