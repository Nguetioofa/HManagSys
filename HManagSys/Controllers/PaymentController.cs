using HManagSys.Attributes;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Payments;
using HManagSys.Services.Implementations;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers;

[RequireAuthentication]
[RequireCurrentCenter]
public class PaymentController : BaseController
{
    private readonly IPaymentService _paymentService;
    private readonly IPatientService _patientService;
    private readonly ICareEpisodeService _careEpisodeService;
    private readonly IExaminationService _examinationService;
    private readonly IApplicationLogger _logger;
    private readonly IUserRepository _userRepository;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly IWorkflowService _workflowService;
    private readonly IPaymentMethodRepository _paymentMethodRepository;


    public PaymentController(
        IPaymentService paymentService,
        IPatientService patientService,
        ICareEpisodeService careEpisodeService,
        IExaminationService examinationService,
        IApplicationLogger logger,
        IDocumentGenerationService documentGenerationService,
        IPaymentMethodRepository paymentMethodRepository,
        IWorkflowService workflowService,
        IUserRepository userRepository)
    {
        _paymentService = paymentService;
        _patientService = patientService;
        _careEpisodeService = careEpisodeService;
        _examinationService = examinationService;
        _logger = logger;
        _documentGenerationService = documentGenerationService;
        _paymentMethodRepository = paymentMethodRepository;
        _userRepository = userRepository;
        _workflowService = workflowService;
    }


    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> GetPayableReferences(string type, int patientId)
    {
        try
        {
            var references = new List<object>();

            switch (type)
            {
                case "CareEpisode":
                    // Récupérer les épisodes de soins avec solde restant
                    var episodes = await _careEpisodeService.GetPatientCareEpisodesAsync(patientId);
                    references = episodes
                        .Where(e => e.RemainingBalance > 0)
                        .Select(e => new {
                            id = e.Id,
                            description = $"Épisode de soins du {e.EpisodeStartDate:dd/MM/yyyy} - {e.DiagnosisName}",
                            remainingAmount = e.RemainingBalance,
                            totalAmount = e.TotalCost
                        }).Cast<object>().ToList();
                    break;

                case "Examination":
                    // Récupérer les examens avec paiement dû
                    var examinations = await _examinationService.GetPatientExaminationsAsync(patientId);
                    // Calculer le montant restant à payer
                    var examinationsWithPayments = await _paymentService.CalculateRemainingPaymentsAsync(
                        examinations.Select(e => new PaymentReference
                        {
                            Type = "Examination",
                            Id = e.Id,
                            Amount = e.FinalPrice
                        }).ToList());

                    references = examinations
                        .Where(e => examinationsWithPayments.TryGetValue(e.Id, out var remaining) && remaining > 0)
                        .Select(e => new {
                            id = e.Id,
                            description = $"Examen {e.ExaminationTypeName} du {e.RequestDate:dd/MM/yyyy}",
                            remainingAmount = examinationsWithPayments[e.Id],
                            totalAmount = e.FinalPrice
                        }).Cast<object>().ToList();
                    break;

                default:
                    return Json(new { success = false, message = "Type de référence non pris en charge" });
            }

            return Json(new
            {
                success = true,
                references,
                message = references.Any()
                    ? $"{references.Count} référence(s) trouvée(s)"
                    : "Aucune référence impayée trouvée"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Une erreur est survenue lors de la récupération des références" });
        }
    }



    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> GetPaymentMethods()
    {
        try
        {
            var methods = await _paymentMethodRepository.QueryListAsync(x=>x.Where(x=>x.IsActive == true));

            var result = methods.Select(m => new
            {
                id = m.Id,
                name = m.Name
            }).ToList();

            return Json(result);

        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Une erreur est survenue" });
        }
    }



    [MedicalStaff]
    public async Task<IActionResult> DownloadReceipt(int id)
    {
        try
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Paiement introuvable";
                return RedirectToAction(nameof(Index));
            }

            var pdfBytes = await _documentGenerationService.GenerateReceiptPdfAsync(id);


            return File(pdfBytes, "application/pdf", $"Recu_Paiement_{id}.pdf");
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "ReceiptPdfError",
                $"Erreur lors de la génération du PDF du reçu de paiement {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PaymentId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors de la génération du PDF";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [MedicalStaff]
    public async Task<IActionResult> Index(PaymentFilters? filters = null)
    {
        try
        {
            filters ??= new PaymentFilters();
            filters.HospitalCenterId = CurrentCenterId;

            var (payments, total) = await _paymentService.GetPaymentsAsync(filters);

            var viewModel = new PagedViewModel<PaymentViewModel, PaymentFilters>
            {
                Items = payments,
                Filters = filters,
                Pagination = new PaginationInfo
                {
                    CurrentPage = filters.PageIndex,
                    PageSize = filters.PageSize,
                    TotalCount = total
                }
            };

            // Journalisation de l'accès
            await _logger.LogInfoAsync("Payment", "IndexAccessed",
                "Accès à la liste des paiements",
                CurrentUserId, CurrentCenterId);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "IndexError",
                "Erreur lors du chargement de la liste des paiements",
                CurrentUserId, CurrentCenterId,
                details: new { Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement de la liste des paiements";
            return View(new PagedViewModel<PaymentViewModel, PaymentFilters>());
        }
    }

    [MedicalStaff]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Paiement introuvable";
                return RedirectToAction(nameof(Index));
            }

            var actions = await _workflowService.GetNextActionsAsync(nameof(Payment), id);
            var relatedEntities = await _workflowService.GetRelatedEntitiesAsync(nameof(Payment), id);

            ViewBag.WorkflowActions = actions;
            ViewBag.RelatedEntities = relatedEntities;

            return View(payment);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "DetailsError",
                $"Erreur lors du chargement des détails du paiement {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PaymentId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des détails du paiement";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    [MedicalStaff]
    public async Task<IActionResult> Create(string referenceType, int referenceId)
    {
        try
        {
            // Vérifier si le type de référence est valide
            if (!IsValidReferenceType(referenceType))
            {
                TempData["ErrorMessage"] = "Type de référence invalide";
                return RedirectToAction(nameof(Index));
            }

            // Créer le modèle initial
            var model = new CreatePaymentViewModel
            {
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                HospitalCenterId = CurrentCenterId.Value,
                PaymentDate = DateTime.Now,
                ReceivedById = CurrentUserId.Value,
            };

            // Charger les informations spécifiques selon le type de référence
            switch (referenceType)
            {
                case "CareEpisode":
                    await LoadCareEpisodeInfoAsync(model, referenceId);
                    break;
                case "Examination":
                    await LoadExaminationInfoAsync(model, referenceId);
                    break;
                default:
                    TempData["ErrorMessage"] = $"Type de référence non pris en charge: {referenceType}";
                    return RedirectToAction(nameof(Index));
            }

            // Charger les méthodes de paiement
            model.PaymentMethods = await _paymentService.GetPaymentMethodsAsync();

            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "CreateGetError",
                "Erreur lors du chargement du formulaire de paiement",
                CurrentUserId, CurrentCenterId,
                details: new { ReferenceType = referenceType, ReferenceId = referenceId, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire de paiement";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> AddPayment(string referenceType, int referenceId, int patientId,
        decimal amount, int paymentMethodId, string transactionReference, string notes)
    {
        try
        {
            var model = new CreatePaymentViewModel
            {
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                PatientId = patientId,
                HospitalCenterId = CurrentCenterId.Value,
                PaymentDate = DateTime.Now,
                Amount = amount,
                PaymentMethodId = paymentMethodId,
                ReceivedById = CurrentUserId.Value,
                TransactionReference = transactionReference,
                Notes = notes
            };

            var result = await _paymentService.CreatePaymentAsync(model, CurrentUserId.Value);

            if (result.IsSuccess)
            {
                await _logger.LogInfoAsync("Payment", "AjaxPaymentCreated",
                    "Paiement créé par AJAX",
                    CurrentUserId, CurrentCenterId,
                    details: new { PaymentId = result.Data.Id });

                return Json(new
                {
                    success = true,
                    message = "Paiement enregistré avec succès",
                    paymentId = result.Data.Id
                });
            }

            return Json(new { success = false, message = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "AjaxPaymentError",
                "Erreur lors de la création du paiement par AJAX",
                CurrentUserId, CurrentCenterId,
                details: new { Error = ex.Message });

            return Json(new { success = false, message = "Une erreur est survenue lors de l'enregistrement du paiement" });
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> CreateModal(CreatePaymentViewModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Données invalides" });

        try
        {
            model.HospitalCenterId = CurrentCenterId.Value;
            model.ReceivedById = CurrentUserId.Value;
            var result = await _paymentService.CreatePaymentAsync(model, CurrentUserId.Value);

            if (result.IsSuccess)
                return Json(new { success = true, message = "Paiement enregistré" });


            string message = string.Join(",", result.ValidationErrors);

            return Json(new { success = false, message });

        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "CreateModal",
    "Erreur lors de la création du paiement",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });
            return Json(new { success = false, message = ex.Message });
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [MedicalStaff]
    public async Task<IActionResult> Create(CreatePaymentViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                // Recharger les listes pour le formulaire
                model.PaymentMethods = await _paymentService.GetPaymentMethodsAsync();
                return View(model);
            }
            model.HospitalCenterId = CurrentCenterId.Value;

            // Créer le paiement
            var result = await _paymentService.CreatePaymentAsync(model, CurrentUserId.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Paiement enregistré avec succès";
                return RedirectToAction(nameof(Receipt), new { id = result.Data.Id });
            }

            // En cas d'erreur
            foreach (var error in result.ValidationErrors)
            {
                ModelState.AddModelError("", error);
            }

            // Recharger les listes pour le formulaire
            model.PaymentMethods = await _paymentService.GetPaymentMethodsAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "CreatePostError",
                "Erreur lors de la création du paiement",
                CurrentUserId, CurrentCenterId,
                details: new { Model = model, Error = ex.Message });

            ModelState.AddModelError("", "Une erreur est survenue lors de la création du paiement");
            model.PaymentMethods = await _paymentService.GetPaymentMethodsAsync();
            return View(model);
        }
    }

    [MedicalStaff]
    public async Task<IActionResult> Receipt(int id)
    {
        try
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Paiement introuvable";
                return RedirectToAction(nameof(Index));
            }

            return View(payment);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "ReceiptError",
                $"Erreur lors du chargement du reçu du paiement {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PaymentId = id, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement du reçu";
            return RedirectToAction(nameof(Details), new { id });
        }
    }






    //[MedicalStaff]
    //public async Task<IActionResult> DownloadReceipt(int id)
    //{
    //    try
    //    {
    //        var payment = await _paymentService.GetByIdAsync(id);
    //        if (payment == null)
    //        {
    //            TempData["ErrorMessage"] = "Paiement introuvable";
    //            return RedirectToAction(nameof(Index));
    //        }

    //        var pdfBytes = await _paymentService.GenerateReceiptAsync(id);

    //        return File(pdfBytes, "application/pdf", $"Recu_{payment.ReferenceType}_{payment.ReferenceId}_{payment.Id}.pdf");
    //    }
    //    catch (Exception ex)
    //    {
    //        await _logger.LogErrorAsync("Payment", "DownloadReceiptError",
    //            $"Erreur lors du téléchargement du reçu du paiement {id}",
    //            CurrentUserId, CurrentCenterId,
    //            details: new { PaymentId = id, Error = ex.Message });

    //        TempData["ErrorMessage"] = "Erreur lors de la génération du reçu";
    //        return RedirectToAction(nameof(Details), new { id });
    //    }
    //}

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SuperAdmin]
    public async Task<IActionResult> Cancel(int id, string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Une raison est requise pour annuler le paiement";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _paymentService.CancelPaymentAsync(id, reason, CurrentUserId.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Paiement annulé avec succès";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["ErrorMessage"] = result.ErrorMessage ?? "Erreur lors de l'annulation du paiement";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "CancelError",
                $"Erreur lors de l'annulation du paiement {id}",
                CurrentUserId, CurrentCenterId,
                details: new { PaymentId = id, Reason = reason, Error = ex.Message });

            TempData["ErrorMessage"] = "Une erreur est survenue lors de l'annulation du paiement";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [MedicalStaff]
    public async Task<IActionResult> PatientPayments(int patientId)
    {
        try
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient introuvable";
                return RedirectToAction("Index", "Patient");
            }

            var payments = await _paymentService.GetPatientPaymentHistoryAsync(patientId);
            var summary = await _paymentService.GetPatientPaymentSummaryAsync(patientId);

            var viewModel = new PatientPaymentsViewModel
            {
                Patient = new Models.ViewModels.Patients.PatientViewModel
                {
                    Id = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName
                },
                Payments = payments,
                Summary = summary
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Payment", "PatientPaymentsError",
                $"Erreur lors du chargement des paiements du patient {patientId}",
                CurrentUserId, CurrentCenterId,
                details: new { PatientId = patientId, Error = ex.Message });

            TempData["ErrorMessage"] = "Erreur lors du chargement des paiements du patient";
            return RedirectToAction("Details", "Patient", new { id = patientId });
        }
    }

    // Méthodes privées d'aide

    private bool IsValidReferenceType(string referenceType)
    {
        return referenceType is "CareEpisode" or "Examination";
    }

    private async Task LoadCareEpisodeInfoAsync(CreatePaymentViewModel model, int careEpisodeId)
    {
        var careEpisode = await _careEpisodeService.GetByIdAsync(careEpisodeId);
        if (careEpisode == null)
        {
            throw new Exception($"Épisode de soins {careEpisodeId} introuvable");
        }

        model.PatientId = careEpisode.PatientId;
        model.PatientName = careEpisode.PatientName;
        model.ReferenceDescription = $"Épisode de soins ({careEpisode.DiagnosisName})";
        model.TotalAmount = careEpisode.TotalCost;
        model.RemainingAmount = careEpisode.RemainingBalance;
        model.Amount = careEpisode.RemainingBalance;
    }

    private async Task LoadExaminationInfoAsync(CreatePaymentViewModel model, int examinationId)
    {
        var examination = await _examinationService.GetByIdAsync(examinationId);
        if (examination == null)
        {
            throw new Exception($"Examen {examinationId} introuvable");
        }

        model.PatientId = examination.PatientId;
        model.PatientName = examination.PatientName;
        model.ReferenceDescription = $"Examen {examination.ExaminationTypeName}";
        model.TotalAmount = examination.FinalPrice;

        // Calculer le montant restant à payer (si des paiements existent déjà)
        var existingPayments = await _paymentService.GetPaymentsByReferenceAsync("Examination", examinationId);
        decimal totalPaid = existingPayments.Where(p => !p.IsCancelled).Sum(p => p.Amount);
        model.RemainingAmount = examination.FinalPrice - totalPaid;
        model.Amount = model.RemainingAmount ?? examination.FinalPrice;
    }
}

public class PaymentReference
{
    public string Type { get; set; }
    public int Id { get; set; }
    public decimal Amount { get; set; }
}