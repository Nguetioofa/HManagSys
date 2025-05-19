using HManagSys.Models.ViewModels.Workflow;
using HManagSys.Services.Interfaces;

namespace HManagSys.Services.Implementations;

/// <summary>
/// Implémentation du service de workflow
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly ICareEpisodeService _careEpisodeService;
    private readonly IPatientService _patientService;
    private readonly IExaminationService _examinationService;
    private readonly IPrescriptionService _prescriptionService;
    private readonly IPaymentService _paymentService;
    private readonly IApplicationLogger _logger;

    public WorkflowService(
        ICareEpisodeService careEpisodeService,
        IPatientService patientService,
        IExaminationService examinationService,
        IPrescriptionService prescriptionService,
        IPaymentService paymentService,
        IApplicationLogger logger)
    {
        _careEpisodeService = careEpisodeService;
        _patientService = patientService;
        _examinationService = examinationService;
        _prescriptionService = prescriptionService;
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Obtient les actions suivantes possibles pour une entité
    /// </summary>
    public async Task<List<WorkflowActionViewModel>> GetNextActionsAsync(string entityType, int entityId)
    {
        try
        {
            var actions = new List<WorkflowActionViewModel>();

            switch (entityType.ToLower())
            {
                case "patient":
                    await AddPatientActionsAsync(entityId, actions);
                    break;

                case "careepisode":
                    await AddCareEpisodeActionsAsync(entityId, actions);
                    break;

                case "examination":
                    await AddExaminationActionsAsync(entityId, actions);
                    break;

                case "prescription":
                    await AddPrescriptionActionsAsync(entityId, actions);
                    break;

                case "payment":
                    await AddPaymentActionsAsync(entityId, actions);
                    break;

                default:
                    break;
            }

            return actions.OrderByDescending(a => a.IsRecommended)
                          .ThenByDescending(a => a.Priority)
                          .ToList();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Workflow", "GetNextActionsError",
                $"Erreur lors de la récupération des actions pour {entityType} #{entityId}",
                details: new { EntityType = entityType, EntityId = entityId, Error = ex.Message });

            return new List<WorkflowActionViewModel>();
        }
    }

    /// <summary>
    /// Obtient les entités liées à une entité spécifique
    /// </summary>
    public async Task<List<RelatedEntityViewModel>> GetRelatedEntitiesAsync(string entityType, int entityId)
    {
        try
        {
            var entities = new List<RelatedEntityViewModel>();

            switch (entityType.ToLower())
            {
                case "patient":
                    await AddPatientRelatedEntitiesAsync(entityId, entities);
                    break;

                case "careepisode":
                    await AddCareEpisodeRelatedEntitiesAsync(entityId, entities);
                    break;

                case "examination":
                    await AddExaminationRelatedEntitiesAsync(entityId, entities);
                    break;

                case "prescription":
                    await AddPrescriptionRelatedEntitiesAsync(entityId, entities);
                    break;

                case "payment":
                    await AddPaymentRelatedEntitiesAsync(entityId, entities);
                    break;

                default:
                    break;
            }

            return entities.OrderByDescending(e => e.RelationshipDate).ToList();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Workflow", "GetRelatedEntitiesError",
                $"Erreur lors de la récupération des entités liées pour {entityType} #{entityId}",
                details: new { EntityType = entityType, EntityId = entityId, Error = ex.Message });

            return new List<RelatedEntityViewModel>();
        }
    }

    #region Patient Actions & Related Entities

    private async Task AddPatientActionsAsync(int patientId, List<WorkflowActionViewModel> actions)
    {
        var patient = await _patientService.GetPatientByIdAsync(patientId);
        if (patient == null) return;

        // Action: Ajouter diagnostic
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "add-diagnosis",
            DisplayName = "Ajouter diagnostic",
            Description = "Enregistrer un nouveau diagnostic pour ce patient",
            ControllerName = "Patient",
            ActionName = "AddDiagnosis",
            RouteValues = new Dictionary<string, string> { { "patientId", patientId.ToString() } },
            IconClass = "fas fa-stethoscope",
            ButtonClass = "btn-outline-info",
            Priority = 10,
            IsRecommended = true
        });

        // Action: Nouvel épisode de soins
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "new-care-episode",
            DisplayName = "Nouvel épisode de soins",
            Description = "Démarrer un nouvel épisode de soins pour ce patient",
            ControllerName = "CareEpisode",
            ActionName = "Create",
            RouteValues = new Dictionary<string, string> { { "patientId", patientId.ToString() } },
            IconClass = "fas fa-procedures",
            ButtonClass = "btn-outline-primary",
            Priority = 8
        });

        // Action: Nouvel examen
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "new-examination",
            DisplayName = "Nouvel examen",
            Description = "Prescrire un nouvel examen pour ce patient",
            ControllerName = "Examination",
            ActionName = "Create",
            RouteValues = new Dictionary<string, string> { { "patientId", patientId.ToString() } },
            IconClass = "fas fa-microscope",
            ButtonClass = "btn-outline-primary",
            Priority = 6
        });

        // Action: Nouvelle prescription
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "new-prescription",
            DisplayName = "Nouvelle prescription",
            Description = "Créer une nouvelle prescription pour ce patient",
            ControllerName = "Prescription",
            ActionName = "Create",
            RouteValues = new Dictionary<string, string> { { "patientId", patientId.ToString() } },
            IconClass = "fas fa-prescription",
            ButtonClass = "btn-outline-primary",
            Priority = 7
        });

        // Action: Voir l'historique
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "view-history",
            DisplayName = "Historique médical",
            Description = "Consulter l'historique médical complet du patient",
            ControllerName = "Patient",
            ActionName = "History",
            RouteValues = new Dictionary<string, string> { { "id", patientId.ToString() } },
            IconClass = "fas fa-history",
            ButtonClass = "btn-outline-secondary",
            Priority = 5
        });

        // Action: Voir les paiements
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "view-payments",
            DisplayName = "Historique paiements",
            Description = "Consulter l'historique des paiements du patient",
            ControllerName = "Payment",
            ActionName = "PatientPayments",
            RouteValues = new Dictionary<string, string> { { "patientId", patientId.ToString() } },
            IconClass = "fas fa-money-bill-wave",
            ButtonClass = "btn-outline-secondary",
            Priority = 4
        });
    }

    private async Task AddPatientRelatedEntitiesAsync(int patientId, List<RelatedEntityViewModel> entities)
    {
        // Récupérer les diagnostics
        var diagnoses = await _patientService.GetPatientDiagnosesAsync(patientId);
        foreach (var diagnosis in diagnoses)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "Diagnosis",
                EntityId = diagnosis.Id,
                DisplayName = diagnosis.DiagnosisName,
                Description = diagnosis.Description ?? "Aucune description",
                RelationshipType = "Diagnostic",
                RelationshipDate = diagnosis.DiagnosisDate,
                ControllerName = "Patient",
                ActionName = "DiagnosisDetails", // Ajouter si disponible
                IconClass = "fas fa-stethoscope",
                BadgeClass = $"badge-{GetSeverityClass(diagnosis.Severity)}"
            });
        }

        // Récupérer les épisodes de soins
        var careEpisodes = await _careEpisodeService.GetPatientCareEpisodesAsync(patientId);
        foreach (var episode in careEpisodes)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "CareEpisode",
                EntityId = episode.Id,
                DisplayName = $"Épisode de soins ({episode.DiagnosisName})",
                Description = $"Statut: {episode.Status}, Soignant: {episode.PrimaryCaregiverName}",
                RelationshipType = "Soins",
                RelationshipDate = episode.EpisodeStartDate,
                ControllerName = "CareEpisode",
                ActionName = "Details",
                IconClass = "fas fa-procedures",
                BadgeClass = GetStatusBadgeClass(episode.Status)
            });
        }

        // Récupérer les examens
        var examinations = await _examinationService.GetPatientExaminationsAsync(patientId);
        foreach (var exam in examinations)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "Examination",
                EntityId = exam.Id,
                DisplayName = exam.ExaminationTypeName,
                Description = $"Statut: {exam.Status}, Demandé par: {exam.RequestedByName}",
                RelationshipType = "Examen",
                RelationshipDate = exam.RequestDate,
                ControllerName = "Examination",
                ActionName = "Details",
                IconClass = "fas fa-microscope",
                BadgeClass = GetStatusBadgeClass(exam.Status)
            });
        }

        // Récupérer les prescriptions
        var prescriptions = await _prescriptionService.GetPatientPrescriptionsAsync(patientId);
        foreach (var prescription in prescriptions)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "Prescription",
                EntityId = prescription.Id,
                DisplayName = $"Prescription ({prescription.Items.Count} produits)",
                Description = $"Prescrit par: {prescription.PrescribedByName}, Statut: {prescription.StatusText}",
                RelationshipType = "Prescription",
                RelationshipDate = prescription.PrescriptionDate,
                ControllerName = "Prescription",
                ActionName = "Details",
                IconClass = "fas fa-prescription",
                BadgeClass = GetStatusBadgeClass(prescription.Status)
            });
        }

        // Récupérer les paiements
        var payments = await _paymentService.GetPatientPaymentHistoryAsync(patientId);
        foreach (var payment in payments)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "Payment",
                EntityId = payment.Id,
                DisplayName = $"Paiement {payment.FormattedAmount}",
                Description = $"Pour: {payment.ReferenceDescription}, Méthode: {payment.PaymentMethodName}",
                RelationshipType = "Paiement",
                RelationshipDate = payment.PaymentDate,
                ControllerName = "Payment",
                ActionName = "Details",
                IconClass = "fas fa-money-bill-wave",
                BadgeClass = payment.IsCancelled ? "badge-danger" : "badge-success"
            });
        }
    }

    #endregion

    #region CareEpisode Actions & Related Entities

    private async Task AddCareEpisodeActionsAsync(int episodeId, List<WorkflowActionViewModel> actions)
    {
        var episode = await _careEpisodeService.GetByIdAsync(episodeId);
        if (episode == null) return;

        // Actions selon le statut
        if (episode.Status == "Active")
        {
            // Action: Ajouter un soin
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "add-care-service",
                DisplayName = "Ajouter un soin",
                Description = "Enregistrer un nouveau service de soins",
                ControllerName = "CareEpisode",
                ActionName = "AddCareService",
                RouteValues = new Dictionary<string, string> { { "episodeId", episodeId.ToString() } },
                IconClass = "fas fa-plus-circle",
                ButtonClass = "btn-outline-success",
                Priority = 10,
                IsRecommended = true
            });

            // Action: Nouvel examen
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "new-examination",
                DisplayName = "Nouvel examen",
                Description = "Prescrire un nouvel examen lié à cet épisode",
                ControllerName = "Examination",
                ActionName = "Create",
                RouteValues = new Dictionary<string, string> {
                    { "patientId", episode.PatientId.ToString() },
                    { "careEpisodeId", episodeId.ToString() }
                },
                IconClass = "fas fa-microscope",
                ButtonClass = "btn-outline-primary",
                Priority = 8
            });

            // Action: Nouvelle prescription
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "new-prescription",
                DisplayName = "Nouvelle prescription",
                Description = "Créer une nouvelle prescription liée à cet épisode",
                ControllerName = "Prescription",
                ActionName = "Create",
                RouteValues = new Dictionary<string, string> {
                    { "patientId", episode.PatientId.ToString() },
                    { "careEpisodeId", episodeId.ToString() }
                },
                IconClass = "fas fa-prescription",
                ButtonClass = "btn-outline-primary",
                Priority = 7
            });

            // Action: Terminer l'épisode
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "complete-episode",
                DisplayName = "Terminer l'épisode",
                Description = "Marquer cet épisode comme terminé",
                ControllerName = "CareEpisode",
                ActionName = "Complete",
                RouteValues = new Dictionary<string, string> { { "id", episodeId.ToString() } },
                IconClass = "fas fa-check-circle",
                ButtonClass = "btn-outline-info",
                Priority = 5
            });

            // Action: Interrompre l'épisode
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "interrupt-episode",
                DisplayName = "Interrompre l'épisode",
                Description = "Interrompre cet épisode de soins",
                ControllerName = "CareEpisode",
                ActionName = "Interrupt",
                RouteValues = new Dictionary<string, string> { { "id", episodeId.ToString() } },
                IconClass = "fas fa-times-circle",
                ButtonClass = "btn-outline-danger",
                Priority = 4
            });
        }

        // Pour tous les statuts

        // Action: Enregistrer un paiement
        if (episode.RemainingBalance > 0)
        {
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "add-payment",
                DisplayName = "Enregistrer un paiement",
                Description = $"Solde restant: {episode.RemainingBalance:N0} FCFA",
                ControllerName = "Payment",
                ActionName = "Create",
                RouteValues = new Dictionary<string, string> {
                    { "referenceType", "CareEpisode" },
                    { "referenceId", episodeId.ToString() }
                },
                IconClass = "fas fa-money-bill-wave",
                ButtonClass = "btn-outline-success",
                Priority = episode.Status == "Active" ? 9 : 10,
                IsRecommended = episode.Status != "Active"
            });
        }

        // Action: Voir le patient
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "view-patient",
            DisplayName = "Voir le patient",
            Description = "Consulter le dossier complet du patient",
            ControllerName = "Patient",
            ActionName = "Details",
            RouteValues = new Dictionary<string, string> { { "id", episode.PatientId.ToString() } },
            IconClass = "fas fa-user",
            ButtonClass = "btn-outline-secondary",
            Priority = 2
        });
    }

    private async Task AddCareEpisodeRelatedEntitiesAsync(int episodeId, List<RelatedEntityViewModel> entities)
    {
        var episode = await _careEpisodeService.GetByIdAsync(episodeId);
        if (episode == null) return;

        // Patient
        entities.Add(new RelatedEntityViewModel
        {
            EntityType = "Patient",
            EntityId = episode.PatientId,
            DisplayName = episode.PatientName,
            Description = "Patient",
            RelationshipType = "Patient",
            RelationshipDate = episode.EpisodeStartDate,
            ControllerName = "Patient",
            ActionName = "Details",
            IconClass = "fas fa-user",
            BadgeClass = "badge-primary"
        });

        // Diagnostic associé
        entities.Add(new RelatedEntityViewModel
        {
            EntityType = "Diagnosis",
            EntityId = episode.DiagnosisId,
            DisplayName = episode.DiagnosisName,
            Description = "Diagnostic lié à cet épisode",
            RelationshipType = "Diagnostic",
            RelationshipDate = episode.EpisodeStartDate,
            ControllerName = "Patient",
            ActionName = "DiagnosisDetails", // À adapter selon la structure
            IconClass = "fas fa-stethoscope",
            BadgeClass = "badge-info"
        });

        // Services de soins
        if (episode.CareServices != null)
        {
            foreach (var service in episode.CareServices)
            {
                entities.Add(new RelatedEntityViewModel
                {
                    EntityType = "CareService",
                    EntityId = service.Id,
                    DisplayName = service.CareTypeName,
                    Description = $"Administré par: {service.AdministeredByName}, Coût: {service.Cost:N0} FCFA",
                    RelationshipType = "Service de soin",
                    RelationshipDate = service.ServiceDate,
                    ControllerName = "CareEpisode",
                    ActionName = "ServiceDetails", // À adapter selon la structure
                    IconClass = "fas fa-hand-holding-medical",
                    BadgeClass = "badge-success"
                });
            }
        }

        // Examens liés
        var examinations = await _examinationService.GetByEpisodeAsync(episodeId);
        foreach (var exam in examinations)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "Examination",
                EntityId = exam.Id,
                DisplayName = exam.ExaminationTypeName,
                Description = $"Statut: {exam.Status}, Demandé par: {exam.RequestedByName}",
                RelationshipType = "Examen",
                RelationshipDate = exam.RequestDate,
                ControllerName = "Examination",
                ActionName = "Details",
                IconClass = "fas fa-microscope",
                BadgeClass = GetStatusBadgeClass(exam.Status)
            });
        }

        // Prescriptions liées
        var prescriptions = await _prescriptionService.GetByEpisodeAsync(episodeId);
        foreach (var prescription in prescriptions)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "Prescription",
                EntityId = prescription.Id,
                DisplayName = $"Prescription ({prescription.Items.Count} produits)",
                Description = $"Prescrit par: {prescription.PrescribedByName}, Statut: {prescription.StatusText}",
                RelationshipType = "Prescription",
                RelationshipDate = prescription.PrescriptionDate,
                ControllerName = "Prescription",
                ActionName = "Details",
                IconClass = "fas fa-prescription",
                BadgeClass = GetStatusBadgeClass(prescription.Status)
            });
        }

        // Paiements liés
        var payments = await _paymentService.GetPaymentsByReferenceAsync("CareEpisode", episodeId);
        foreach (var payment in payments)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "Payment",
                EntityId = payment.Id,
                DisplayName = $"Paiement {payment.FormattedAmount}",
                Description = $"Méthode: {payment.PaymentMethodName}, Reçu par: {payment.ReceivedByName}",
                RelationshipType = "Paiement",
                RelationshipDate = payment.PaymentDate,
                ControllerName = "Payment",
                ActionName = "Details",
                IconClass = "fas fa-money-bill-wave",
                BadgeClass = payment.IsCancelled ? "badge-danger" : "badge-success"
            });
        }
    }

    #endregion

    #region Examination Actions & Related Entities

    private async Task AddExaminationActionsAsync(int examinationId, List<WorkflowActionViewModel> actions)
    {
        var examination = await _examinationService.GetByIdAsync(examinationId);
        if (examination == null) return;

        // Actions selon le statut
        switch (examination.Status)
        {
            case "Requested":
                // Action: Planifier
                actions.Add(new WorkflowActionViewModel
                {
                    ActionId = "schedule-examination",
                    DisplayName = "Planifier l'examen",
                    Description = "Programmer une date pour cet examen",
                    ControllerName = "Examination",
                    ActionName = "Schedule",
                    RouteValues = new Dictionary<string, string> { { "id", examinationId.ToString() } },
                    IconClass = "fas fa-calendar-alt",
                    ButtonClass = "btn-outline-primary",
                    Priority = 10,
                    IsRecommended = true
                });
                break;

            case "Scheduled":
                // Action: Réaliser
                actions.Add(new WorkflowActionViewModel
                {
                    ActionId = "complete-examination",
                    DisplayName = "Réaliser l'examen",
                    Description = "Marquer l'examen comme réalisé",
                    ControllerName = "Examination",
                    ActionName = "Complete",
                    RouteValues = new Dictionary<string, string> { { "id", examinationId.ToString() } },
                    IconClass = "fas fa-check-circle",
                    ButtonClass = "btn-outline-success",
                    Priority = 10,
                    IsRecommended = true
                });
                break;

            case "Completed":
                if (examination.Result == null)
                {
                    // Action: Ajouter résultat
                    actions.Add(new WorkflowActionViewModel
                    {
                        ActionId = "add-result",
                        DisplayName = "Ajouter un résultat",
                        Description = "Enregistrer les résultats de l'examen",
                        ControllerName = "Examination",
                        ActionName = "AddResult",
                        RouteValues = new Dictionary<string, string> { { "examinationId", examinationId.ToString() } },
                        IconClass = "fas fa-clipboard-check",
                        ButtonClass = "btn-outline-success",
                        Priority = 10,
                        IsRecommended = true
                    });
                }
                else
                {
                    // Action: Imprimer résultat
                    actions.Add(new WorkflowActionViewModel
                    {
                        ActionId = "print-result",
                        DisplayName = "Imprimer le résultat",
                        Description = "Imprimer le résultat de l'examen",
                        ControllerName = "Examination",
                        ActionName = "PrintResult",
                        RouteValues = new Dictionary<string, string> { { "id", examinationId.ToString() } },
                        IconClass = "fas fa-print",
                        ButtonClass = "btn-outline-secondary",
                        Priority = 7
                    });

                    // Action: Télécharger PDF
                    actions.Add(new WorkflowActionViewModel
                    {
                        ActionId = "download-result-pdf",
                        DisplayName = "Télécharger PDF",
                        Description = "Télécharger le résultat en PDF",
                        ControllerName = "Examination",
                        ActionName = "DownloadResultPdf",
                        RouteValues = new Dictionary<string, string> { { "id", examinationId.ToString() } },
                        IconClass = "fas fa-file-pdf",
                        ButtonClass = "btn-outline-danger",
                        Priority = 6
                    });
                }
                break;
        }

        // Pour tous les statuts sauf "Cancelled"
        if (examination.Status != "Cancelled")
        {
            // Action: Annuler
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "cancel-examination",
                DisplayName = "Annuler l'examen",
                Description = "Annuler cet examen",
                ControllerName = "Examination",
                ActionName = "Cancel",
                RouteValues = new Dictionary<string, string> { { "id", examinationId.ToString() } },
                IconClass = "fas fa-times-circle",
                ButtonClass = "btn-outline-danger",
                Priority = 3
            });
        }

        // Paiement si coût
        if (examination.FinalPrice > 0)
        {
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "add-payment",
                DisplayName = "Enregistrer un paiement",
                Description = $"Montant: {examination.FinalPrice:N0} FCFA",
                ControllerName = "Payment",
                ActionName = "Create",
                RouteValues = new Dictionary<string, string> {
                    { "referenceType", "Examination" },
                    { "referenceId", examinationId.ToString() }
                },
                IconClass = "fas fa-money-bill-wave",
                ButtonClass = "btn-outline-success",
                Priority = 8
            });
        }

        // Action: Voir le patient
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "view-patient",
            DisplayName = "Voir le patient",
            Description = "Consulter le dossier complet du patient",
            ControllerName = "Patient",
            ActionName = "Details",
            RouteValues = new Dictionary<string, string> { { "id", examination.PatientId.ToString() } },
            IconClass = "fas fa-user",
            ButtonClass = "btn-outline-secondary",
            Priority = 2
        });
    }

    private async Task AddExaminationRelatedEntitiesAsync(int examinationId, List<RelatedEntityViewModel> entities)
    {
        var examination = await _examinationService.GetByIdAsync(examinationId);
        if (examination == null) return;

        // Patient
        entities.Add(new RelatedEntityViewModel
        {
            EntityType = "Patient",
            EntityId = examination.PatientId,
            DisplayName = examination.PatientName,
            Description = "Patient",
            RelationshipType = "Patient",
            RelationshipDate = examination.RequestDate,
            ControllerName = "Patient",
            ActionName = "Details",
            IconClass = "fas fa-user",
            BadgeClass = "badge-primary"
        });

        // Épisode de soins lié
        if (examination.CareEpisodeId.HasValue)
        {
            var episode = await _careEpisodeService.GetByIdAsync(examination.CareEpisodeId.Value);
            if (episode != null)
            {
                entities.Add(new RelatedEntityViewModel
                {
                    EntityType = "CareEpisode",
                    EntityId = episode.Id,
                    DisplayName = $"Épisode de soins ({episode.DiagnosisName})",
                    Description = $"Statut: {episode.Status}, Soignant: {episode.PrimaryCaregiverName}",
                    RelationshipType = "Épisode de soins",
                    RelationshipDate = episode.EpisodeStartDate,
                    ControllerName = "CareEpisode",
                    ActionName = "Details",
                    IconClass = "fas fa-procedures",
                    BadgeClass = GetStatusBadgeClass(episode.Status)
                });
            }
        }

        // Résultat d'examen
        if (examination.Result != null)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "ExaminationResult",
                EntityId = examination.Result.Id,
                DisplayName = "Résultat d'examen",
                Description = $"Reporté par: {examination.Result.ReportedByName}, Date: {examination.Result.ReportDate.ToString("dd/MM/yyyy")}",
                RelationshipType = "Résultat",
                RelationshipDate = examination.Result.ReportDate,
                ControllerName = "Examination",
                ActionName = "ResultDetails", // À adapter selon la structure
                IconClass = "fas fa-clipboard-check",
                BadgeClass = "badge-success"
            });
        }

        // Paiements liés
        var payments = await _paymentService.GetPaymentsByReferenceAsync("Examination", examinationId);
        foreach (var payment in payments)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "Payment",
                EntityId = payment.Id,
                DisplayName = $"Paiement {payment.FormattedAmount}",
                Description = $"Méthode: {payment.PaymentMethodName}, Reçu par: {payment.ReceivedByName}",
                RelationshipType = "Paiement",
                RelationshipDate = payment.PaymentDate,
                ControllerName = "Payment",
                ActionName = "Details",
                IconClass = "fas fa-money-bill-wave",
                BadgeClass = payment.IsCancelled ? "badge-danger" : "badge-success"
            });
        }
    }

    #endregion

    #region Prescription Actions & Related Entities

    private async Task AddPrescriptionActionsAsync(int prescriptionId, List<WorkflowActionViewModel> actions)
    {
        var prescription = await _prescriptionService.GetByIdAsync(prescriptionId);
        if (prescription == null) return;

        // Actions selon le statut
        switch (prescription.Status)
        {
            case "Pending":
                // Action: Dispenser
                actions.Add(new WorkflowActionViewModel
                {
                    ActionId = "dispense-prescription",
                    DisplayName = "Dispenser la prescription",
                    Description = "Marquer la prescription comme dispensée",
                    ControllerName = "Prescription",
                    ActionName = "Dispense",
                    RouteValues = new Dictionary<string, string> { { "id", prescriptionId.ToString() } },
                    IconClass = "fas fa-pills",
                    ButtonClass = "btn-outline-success",
                    Priority = 10,
                    IsRecommended = true
                });

                // Action: Modifier
                actions.Add(new WorkflowActionViewModel
                {
                    ActionId = "edit-prescription",
                    DisplayName = "Modifier la prescription",
                    Description = "Modifier les produits et instructions",
                    ControllerName = "Prescription",
                    ActionName = "Edit",
                    RouteValues = new Dictionary<string, string> { { "id", prescriptionId.ToString() } },
                    IconClass = "fas fa-edit",
                    ButtonClass = "btn-outline-primary",
                    Priority = 9
                });

                // Action: Annuler
                actions.Add(new WorkflowActionViewModel
                {
                    ActionId = "cancel-prescription",
                    DisplayName = "Annuler la prescription",
                    Description = "Annuler cette prescription",
                    ControllerName = "Prescription",
                    ActionName = "Cancel",
                    RouteValues = new Dictionary<string, string> { { "id", prescriptionId.ToString() } },
                    IconClass = "fas fa-times-circle",
                    ButtonClass = "btn-outline-danger",
                    Priority = 3
                });
                break;
        }

        // Pour tous les statuts

        // Action: Imprimer
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "print-prescription",
            DisplayName = "Imprimer la prescription",
            Description = "Imprimer la prescription",
            ControllerName = "Prescription",
            ActionName = "Print",
            RouteValues = new Dictionary<string, string> { { "id", prescriptionId.ToString() } },
            IconClass = "fas fa-print",
            ButtonClass = "btn-outline-secondary",
            Priority = 7
        });

        // Action: Télécharger PDF
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "download-prescription-pdf",
            DisplayName = "Télécharger PDF",
            Description = "Télécharger la prescription en PDF",
            ControllerName = "Prescription",
            ActionName = "DownloadPdf",
            RouteValues = new Dictionary<string, string> { { "id", prescriptionId.ToString() } },
            IconClass = "fas fa-file-pdf",
            ButtonClass = "btn-outline-danger",
            Priority = 6
        });

        // Action: Voir le patient
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "view-patient",
            DisplayName = "Voir le patient",
            Description = "Consulter le dossier complet du patient",
            ControllerName = "Patient",
            ActionName = "Details",
            RouteValues = new Dictionary<string, string> { { "id", prescription.PatientId.ToString() } },
            IconClass = "fas fa-user",
            ButtonClass = "btn-outline-secondary",
            Priority = 2
        });
    }

    private async Task AddPrescriptionRelatedEntitiesAsync(int prescriptionId, List<RelatedEntityViewModel> entities)
    {
        var prescription = await _prescriptionService.GetByIdAsync(prescriptionId);
        if (prescription == null) return;

        // Patient
        entities.Add(new RelatedEntityViewModel
        {
            EntityType = "Patient",
            EntityId = prescription.PatientId,
            DisplayName = prescription.PatientName,
            Description = "Patient",
            RelationshipType = "Patient",
            RelationshipDate = prescription.PrescriptionDate,
            ControllerName = "Patient",
            ActionName = "Details",
            IconClass = "fas fa-user",
            BadgeClass = "badge-primary"
        });

        // Diagnostic lié
        if (prescription.DiagnosisId.HasValue)
        {
            var diagnosis = await _patientService.GetDiagnosisAsync(prescription.DiagnosisId.Value);
            if (diagnosis != null)
            {
                entities.Add(new RelatedEntityViewModel
                {
                    EntityType = "Diagnosis",
                    EntityId = diagnosis.Id,
                    DisplayName = diagnosis.DiagnosisName,
                    Description = diagnosis.Description ?? "Aucune description",
                    RelationshipType = "Diagnostic",
                    RelationshipDate = diagnosis.DiagnosisDate,
                    ControllerName = "Patient",
                    ActionName = "DiagnosisDetails", // À adapter selon la structure
                    IconClass = "fas fa-stethoscope",
                    BadgeClass = $"badge-{GetSeverityClass(diagnosis.Severity)}"
                });
            }
        }

        // Épisode de soins lié
        if (prescription.CareEpisodeId.HasValue)
        {
            var episode = await _careEpisodeService.GetByIdAsync(prescription.CareEpisodeId.Value);
            if (episode != null)
            {
                entities.Add(new RelatedEntityViewModel
                {
                    EntityType = "CareEpisode",
                    EntityId = episode.Id,
                    DisplayName = $"Épisode de soins ({episode.DiagnosisName})",
                    Description = $"Statut: {episode.Status}, Soignant: {episode.PrimaryCaregiverName}",
                    RelationshipType = "Épisode de soins",
                    RelationshipDate = episode.EpisodeStartDate,
                    ControllerName = "CareEpisode",
                    ActionName = "Details",
                    IconClass = "fas fa-procedures",
                    BadgeClass = GetStatusBadgeClass(episode.Status)
                });
            }
        }
    }

    #endregion

    #region Payment Actions & Related Entities

    private async Task AddPaymentActionsAsync(int paymentId, List<WorkflowActionViewModel> actions)
    {
        var payment = await _paymentService.GetByIdAsync(paymentId);
        if (payment == null) return;

        // Pour tous les statuts

        // Action: Imprimer reçu
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "print-receipt",
            DisplayName = "Imprimer le reçu",
            Description = "Imprimer le reçu de paiement",
            ControllerName = "Payment",
            ActionName = "Receipt",
            RouteValues = new Dictionary<string, string> { { "id", paymentId.ToString() } },
            IconClass = "fas fa-print",
            ButtonClass = "btn-outline-secondary",
            Priority = 7
        });

        // Action: Télécharger PDF
        actions.Add(new WorkflowActionViewModel
        {
            ActionId = "download-receipt-pdf",
            DisplayName = "Télécharger PDF",
            Description = "Télécharger le reçu en PDF",
            ControllerName = "Payment",
            ActionName = "DownloadReceipt",
            RouteValues = new Dictionary<string, string> { { "id", paymentId.ToString() } },
            IconClass = "fas fa-file-pdf",
            ButtonClass = "btn-outline-danger",
            Priority = 6
        });

        // Action: Voir la référence
        string controllerName = payment.ReferenceType switch
        {
            "CareEpisode" => "CareEpisode",
            "Examination" => "Examination",
            _ => ""
        };

        if (!string.IsNullOrEmpty(controllerName))
        {
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "view-reference",
                DisplayName = $"Voir {payment.ReferenceDescription}",
                Description = $"Consulter les détails de {payment.ReferenceText}",
                ControllerName = controllerName,
                ActionName = "Details",
                RouteValues = new Dictionary<string, string> { { "id", payment.ReferenceId.ToString() } },
                IconClass = payment.ReferenceType == "CareEpisode" ? "fas fa-procedures" : "fas fa-microscope",
                ButtonClass = "btn-outline-primary",
                Priority = 5
            });
        }

        // Action: Voir le patient
        if (payment.PatientId.HasValue)
        {
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "view-patient",
                DisplayName = "Voir le patient",
                Description = "Consulter le dossier complet du patient",
                ControllerName = "Patient",
                ActionName = "Details",
                RouteValues = new Dictionary<string, string> { { "id", payment.PatientId.Value.ToString() } },
                IconClass = "fas fa-user",
                ButtonClass = "btn-outline-secondary",
                Priority = 2
            });
        }

        // Action: Annuler le paiement (SuperAdmin uniquement)
        if (!payment.IsCancelled)
        {
            actions.Add(new WorkflowActionViewModel
            {
                ActionId = "cancel-payment",
                DisplayName = "Annuler le paiement",
                Description = "Annuler ce paiement (SuperAdmin uniquement)",
                ControllerName = "Payment",
                ActionName = "Cancel",
                RouteValues = new Dictionary<string, string> { { "id", paymentId.ToString() } },
                IconClass = "fas fa-ban",
                ButtonClass = "btn-outline-danger",
                Priority = 1
            });
        }
    }

    private async Task AddPaymentRelatedEntitiesAsync(int paymentId, List<RelatedEntityViewModel> entities)
    {
        var payment = await _paymentService.GetByIdAsync(paymentId);
        if (payment == null) return;

        // Patient
        if (payment.PatientId.HasValue)
        {
            entities.Add(new RelatedEntityViewModel
            {
                EntityType = "Patient",
                EntityId = payment.PatientId.Value,
                DisplayName = payment.PatientName,
                Description = "Patient",
                RelationshipType = "Patient",
                RelationshipDate = payment.PaymentDate,
                ControllerName = "Patient",
                ActionName = "Details",
                IconClass = "fas fa-user",
                BadgeClass = "badge-primary"
            });
        }

        // Référence liée
        switch (payment.ReferenceType)
        {
            case "CareEpisode":
                var episode = await _careEpisodeService.GetByIdAsync(payment.ReferenceId);
                if (episode != null)
                {
                    entities.Add(new RelatedEntityViewModel
                    {
                        EntityType = "CareEpisode",
                        EntityId = episode.Id,
                        DisplayName = $"Épisode de soins ({episode.DiagnosisName})",
                        Description = $"Statut: {episode.Status}, Soignant: {episode.PrimaryCaregiverName}",
                        RelationshipType = "Épisode de soins",
                        RelationshipDate = episode.EpisodeStartDate,
                        ControllerName = "CareEpisode",
                        ActionName = "Details",
                        IconClass = "fas fa-procedures",
                        BadgeClass = GetStatusBadgeClass(episode.Status)
                    });
                }
                break;

            case "Examination":
                var examination = await _examinationService.GetByIdAsync(payment.ReferenceId);
                if (examination != null)
                {
                    entities.Add(new RelatedEntityViewModel
                    {
                        EntityType = "Examination",
                        EntityId = examination.Id,
                        DisplayName = examination.ExaminationTypeName,
                        Description = $"Statut: {examination.Status}, Demandé par: {examination.RequestedByName}",
                        RelationshipType = "Examen",
                        RelationshipDate = examination.RequestDate,
                        ControllerName = "Examination",
                        ActionName = "Details",
                        IconClass = "fas fa-microscope",
                        BadgeClass = GetStatusBadgeClass(examination.Status)
                    });
                }
                break;
        }
    }

    #endregion

    #region Helper Methods

    private string GetStatusBadgeClass(string status)
    {
        return status?.ToLower() switch
        {
            "active" => "badge-success",
            "completed" => "badge-primary",
            "interrupted" => "badge-warning",
            "pending" => "badge-info",
            "scheduled" => "badge-info",
            "cancelled" => "badge-danger",
            "dispensed" => "badge-success",
            _ => "badge-secondary"
        };
    }

    private string GetSeverityClass(string? severity)
    {
        return severity?.ToLower() switch
        {
            "critical" => "danger",
            "severe" => "warning",
            "moderate" => "primary",
            "mild" => "success",
            _ => "secondary"
        };
    }

    #endregion
}