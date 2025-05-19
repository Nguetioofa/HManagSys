using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Payments;
using HManagSys.Services.Documents;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using System.Linq;

namespace HManagSys.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IGenericRepository<Payment> _paymentRepository;
        private readonly IGenericRepository<PaymentMethod> _paymentMethodRepository;
        private readonly IGenericRepository<Patient> _patientRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<HospitalCenter> _hospitalCenterRepository;
        private readonly IGenericRepository<CareEpisode> _careEpisodeRepository;
        private readonly IGenericRepository<Examination> _examinationRepository;
        private readonly IApplicationLogger _logger;
        private readonly IAuditService _auditService;

        public PaymentService(
            IGenericRepository<Payment> paymentRepository,
            IGenericRepository<PaymentMethod> paymentMethodRepository,
            IGenericRepository<Patient> patientRepository,
            IGenericRepository<User> userRepository,
            IGenericRepository<HospitalCenter> hospitalCenterRepository,
            IGenericRepository<CareEpisode> careEpisodeRepository,
            IGenericRepository<Examination> examinationRepository,
            IApplicationLogger logger,
            IAuditService auditService)
        {
            _paymentRepository = paymentRepository;
            _paymentMethodRepository = paymentMethodRepository;
            _patientRepository = patientRepository;
            _userRepository = userRepository;
            _hospitalCenterRepository = hospitalCenterRepository;
            _careEpisodeRepository = careEpisodeRepository;
            _examinationRepository = examinationRepository;
            _logger = logger;
            _auditService = auditService;
        }

        /// <summary>
        /// Récupère un paiement par son ID
        /// </summary>
        public async Task<PaymentViewModel?> GetByIdAsync(int id)
        {
            try
            {
                var payment = await _paymentRepository.QuerySingleAsync<PaymentViewModel>(q =>
                    q.Where(p => p.Id == id)
                     .Include(p => p.Patient)
                     .Include(p => p.PaymentMethod)
                     .Include(p => p.HospitalCenter)
                     .Include(p => p.ReceivedByNavigation)
                     .Select(p => new PaymentViewModel
                     {
                         Id = p.Id,
                         ReferenceType = p.ReferenceType,
                         ReferenceId = p.ReferenceId,
                         PatientId = p.PatientId,
                         PatientName = p.Patient != null ? $"{p.Patient.FirstName} {p.Patient.LastName}" : "",
                         HospitalCenterId = p.HospitalCenterId,
                         HospitalCenterName = p.HospitalCenter.Name,
                         PaymentMethodId = p.PaymentMethodId,
                         PaymentMethodName = p.PaymentMethod.Name,
                         Amount = p.Amount,
                         PaymentDate = p.PaymentDate,
                         ReceivedById = p.ReceivedBy,
                         ReceivedByName = $"{p.ReceivedByNavigation.FirstName} {p.ReceivedByNavigation.LastName}",
                         TransactionReference = p.TransactionReference,
                         Notes = p.Notes,
                         // Chercher si le paiement a été annulé dans les notes (à améliorer avec un champ dédié)
                         IsCancelled = p.Notes != null && p.Notes.StartsWith("[CANCELLED]"),
                         CancellationReason = p.Notes != null && p.Notes.StartsWith("[CANCELLED]")
                            ? p.Notes.Replace("[CANCELLED]", "").Trim()
                            : null,
                         CreatedAt = p.CreatedAt,
                         CreatedBy = p.CreatedBy,
                         CreatedByName = p.CreatedBy.ToString() // À remplacer par le nom réel dans une implémentation complète
                     }));

                payment.ReferenceDescription = GetReferenceDescription(payment.ReferenceType, payment.ReferenceId);

                return payment;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "GetByIdError",
                    $"Erreur lors de la récupération du paiement {id}",
                    details: new { PaymentId = id, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Crée un nouveau paiement
        /// </summary>
        public async Task<OperationResult<PaymentViewModel>> CreatePaymentAsync(CreatePaymentViewModel model, int createdBy)
        {
            try
            {
                // Vérifications préalables
                if (!IsValidReferenceType(model.ReferenceType))
                {
                    return OperationResult<PaymentViewModel>.Error("Type de référence invalide");
                }

                var paymentMethod = await _paymentMethodRepository.GetByIdAsync(model.PaymentMethodId);
                if (paymentMethod == null)
                {
                    return OperationResult<PaymentViewModel>.Error("Méthode de paiement invalide");
                }

                // Vérifier que la référence existe
                bool referenceExists = await ValidateReferenceAsync(model.ReferenceType, model.ReferenceId);
                if (!referenceExists)
                {
                    return OperationResult<PaymentViewModel>.Error($"La référence {model.ReferenceType} #{model.ReferenceId} n'existe pas");
                }

                // Vérifier que le patient existe si spécifié
                if (model.PatientId.HasValue)
                {
                    var patient = await _patientRepository.GetByIdAsync(model.PatientId.Value);
                    if (patient == null)
                    {
                        return OperationResult<PaymentViewModel>.Error("Patient invalide");
                    }
                }

                // Vérifier que le centre existe
                var center = await _hospitalCenterRepository.GetByIdAsync(model.HospitalCenterId);
                if (center == null)
                {
                    return OperationResult<PaymentViewModel>.Error("Centre hospitalier invalide");
                }

                // Vérifier que l'utilisateur qui reçoit le paiement existe
                var receiver = await _userRepository.GetByIdAsync(model.ReceivedById);
                if (receiver == null)
                {
                    return OperationResult<PaymentViewModel>.Error("Récepteur du paiement invalide");
                }

                // Création du paiement
                var payment = new Payment
                {
                    ReferenceType = model.ReferenceType,
                    ReferenceId = model.ReferenceId,
                    PatientId = model.PatientId,
                    HospitalCenterId = model.HospitalCenterId,
                    PaymentMethodId = model.PaymentMethodId,
                    Amount = model.Amount,
                    PaymentDate = model.PaymentDate,
                    ReceivedBy = model.ReceivedById,
                    TransactionReference = model.TransactionReference,
                    Notes = model.Notes,
                    CreatedBy = createdBy,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                var createdPayment = await _paymentRepository.AddAsync(payment);

                // Mettre à jour les soldes selon le type de référence
                await UpdateReferenceBalanceAsync(model.ReferenceType, model.ReferenceId, model.Amount);

                // Audit
                await _auditService.LogActionAsync(
                    createdBy,
                    "PAYMENT_CREATE",
                    "Payment",
                    createdPayment.Id,
                    null,
                    new
                    {
                        ReferenceType = model.ReferenceType,
                        ReferenceId = model.ReferenceId,
                        Amount = model.Amount,
                        PaymentMethodId = model.PaymentMethodId
                    },
                    $"Paiement de {model.Amount:N0} FCFA créé pour {model.ReferenceType} #{model.ReferenceId}"
                );

                // Journalisation
                await _logger.LogInfoAsync("PaymentService", "PaymentCreated",
                    $"Paiement créé pour {model.ReferenceType} #{model.ReferenceId}",
                    createdBy,
                    model.HospitalCenterId,
                    nameof(PaymentViewModel),
                    model.PaymentMethodId,
                    new { PaymentId = createdPayment.Id, Amount = model.Amount });

                // Retourner le paiement créé
                var viewModel = await GetByIdAsync(createdPayment.Id);
                if (viewModel == null)
                {
                    // Cas inhabituel, mais géré pour éviter les erreurs
                    return OperationResult<PaymentViewModel>.Error("Le paiement a été créé mais n'a pas pu être récupéré");
                }

                return OperationResult<PaymentViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "CreatePaymentError",
                    "Erreur lors de la création du paiement",
                    createdBy,
                    model.HospitalCenterId,
                    details: new { Model = model, Error = ex.Message });

                return OperationResult<PaymentViewModel>.Error("Une erreur est survenue lors de la création du paiement");
            }
        }

        /// <summary>
        /// Récupère tous les paiements liés à une référence
        /// </summary>
        public async Task<List<PaymentViewModel>> GetPaymentsByReferenceAsync(string referenceType, int referenceId)
        {
            try
            {
                var payments = await _paymentRepository.QueryListAsync<PaymentViewModel>(q =>
                    q.Where(p => p.ReferenceType == referenceType && p.ReferenceId == referenceId)
                     .Include(p => p.Patient)
                     .Include(p => p.PaymentMethod)
                     .Include(p => p.HospitalCenter)
                     .Include(p => p.ReceivedByNavigation)
                     .OrderByDescending(p => p.PaymentDate)
                     .Select(p => new PaymentViewModel
                     {
                         Id = p.Id,
                         ReferenceType = p.ReferenceType,
                         ReferenceId = p.ReferenceId,
                         PatientId = p.PatientId,
                         PatientName = p.Patient != null ? $"{p.Patient.FirstName} {p.Patient.LastName}" : "",
                         HospitalCenterId = p.HospitalCenterId,
                         HospitalCenterName = p.HospitalCenter.Name,
                         PaymentMethodId = p.PaymentMethodId,
                         PaymentMethodName = p.PaymentMethod.Name,
                         Amount = p.Amount,
                         PaymentDate = p.PaymentDate,
                         ReceivedById = p.ReceivedBy,
                         ReceivedByName = $"{p.ReceivedByNavigation.FirstName} {p.ReceivedByNavigation.LastName}",
                         TransactionReference = p.TransactionReference,
                         Notes = p.Notes,
                         IsCancelled = p.Notes != null && p.Notes.StartsWith("[CANCELLED]"),
                         CancellationReason = p.Notes != null && p.Notes.StartsWith("[CANCELLED]")
                            ? p.Notes.Replace("[CANCELLED]", "").Trim()
                            : null,
                         CreatedAt = p.CreatedAt,
                         CreatedBy = p.CreatedBy,
                         CreatedByName = p.CreatedBy.ToString() // À remplacer par le nom réel dans une implémentation complète
                     }));

                payments.ForEach(p => p.ReferenceDescription = GetReferenceDescription(p.ReferenceType, p.ReferenceId));

                return payments;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "GetPaymentsByReferenceError",
                    $"Erreur lors de la récupération des paiements pour {referenceType} #{referenceId}",
                    details: new { ReferenceType = referenceType, ReferenceId = referenceId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère l'historique des paiements d'un patient
        /// </summary>
        public async Task<List<PaymentViewModel>> GetPatientPaymentHistoryAsync(int patientId)
        {
            try
            {
                var payments = await _paymentRepository.QueryListAsync<PaymentViewModel>(q =>
                    q.Where(p => p.PatientId == patientId)
                     .Include(p => p.PaymentMethod)
                     .Include(p => p.HospitalCenter)
                     .Include(p => p.ReceivedByNavigation)
                     .OrderByDescending(p => p.PaymentDate)
                     .Select(p => new PaymentViewModel
                     {
                         Id = p.Id,
                         ReferenceType = p.ReferenceType,
                         ReferenceId = p.ReferenceId,
                         PatientId = p.PatientId,
                         PatientName = $"{p.Patient.FirstName} {p.Patient.LastName}",
                         HospitalCenterId = p.HospitalCenterId,
                         HospitalCenterName = p.HospitalCenter.Name,
                         PaymentMethodId = p.PaymentMethodId,
                         PaymentMethodName = p.PaymentMethod.Name,
                         Amount = p.Amount,
                         PaymentDate = p.PaymentDate,
                         ReceivedById = p.ReceivedBy,
                         ReceivedByName = $"{p.ReceivedByNavigation.FirstName} {p.ReceivedByNavigation.LastName}",
                         TransactionReference = p.TransactionReference,
                         Notes = p.Notes,
                         IsCancelled = p.Notes != null && p.Notes.StartsWith("[CANCELLED]"),
                         CancellationReason = p.Notes != null && p.Notes.StartsWith("[CANCELLED]")
                            ? p.Notes.Replace("[CANCELLED]", "").Trim()
                            : null,
                         CreatedAt = p.CreatedAt,
                         CreatedBy = p.CreatedBy,
                         CreatedByName = p.CreatedBy.ToString() // À remplacer par le nom réel dans une implémentation complète
                     }));

                payments.ForEach(p => p.ReferenceDescription = GetReferenceDescription(p.ReferenceType, p.ReferenceId));

                return payments;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "GetPatientPaymentHistoryError",
                    $"Erreur lors de la récupération de l'historique des paiements du patient {patientId}",
                    details: new { PatientId = patientId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère les méthodes de paiement disponibles
        /// </summary>
        public async Task<List<PaymentMethodViewModel>> GetPaymentMethodsAsync()
        {
            try
            {
                var methods = await _paymentMethodRepository.QueryListAsync<PaymentMethodViewModel>(q =>
                    q.Where(m => m.IsActive)
                     .OrderBy(m => m.Name)
                     .Select(m => new PaymentMethodViewModel
                     {
                         Id = m.Id,
                         Name = m.Name,
                         RequiresBankAccount = m.RequiresBankAccount,
                         IsActive = m.IsActive
                     }));

                return methods;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "GetPaymentMethodsError",
                    "Erreur lors de la récupération des méthodes de paiement",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère un résumé des paiements pour un patient
        /// </summary>
        public async Task<PaymentSummaryViewModel> GetPatientPaymentSummaryAsync(int patientId)
        {
            try
            {
                var patient = await _patientRepository.GetByIdAsync(patientId);
                if (patient == null)
                {
                    throw new Exception($"Patient {patientId} introuvable");
                }

                var payments = await GetPatientPaymentHistoryAsync(patientId);
                var validPayments = payments.Where(p => !p.IsCancelled).ToList();

                // Calcul du total payé
                decimal totalPaid = validPayments.Sum(p => p.Amount);

                // Calcul du total dû (épisodes de soin + examens)
                decimal totalDue = await CalculatePatientTotalDueAsync(patientId);

                // Répartition par type de référence
                var paymentsByType = validPayments
                    .GroupBy(p => p.ReferenceType)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

                // Répartition par méthode de paiement
                var paymentsByMethod = validPayments
                    .GroupBy(p => p.PaymentMethodName)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

                var summary = new PaymentSummaryViewModel
                {
                    PatientId = patientId,
                    PatientName = $"{patient.FirstName} {patient.LastName}",
                    TotalPaid = totalPaid,
                    TotalDue = totalDue,
                    PaymentCount = validPayments.Count,
                    LastPaymentDate = validPayments.Any() ? validPayments.Max(p => p.PaymentDate) : null,
                    PaymentsByType = paymentsByType,
                    PaymentsByMethod = paymentsByMethod
                };

                return summary;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "GetPatientPaymentSummaryError",
                    $"Erreur lors de la récupération du résumé des paiements du patient {patientId}",
                    details: new { PatientId = patientId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Génère un reçu de paiement en PDF
        /// </summary>
        //public async Task<byte[]> GenerateReceiptAsync(int paymentId)
        //{
        //    try
        //    {
        //        var payment = await GetByIdAsync(paymentId);
        //        if (payment == null)
        //        {
        //            throw new Exception($"Paiement {paymentId} introuvable");
        //        }

        //        var center = await _hospitalCenterRepository.GetByIdAsync(payment.HospitalCenterId);
        //        if (center == null)
        //        {
        //            throw new Exception($"Centre hospitalier {payment.HospitalCenterId} introuvable");
        //        }

        //        // Créer le modèle pour le reçu
        //        var receiptModel = new ReceiptViewModel
        //        {
        //            Payment = payment,
        //            HospitalName = center.Name,
        //            HospitalAddress = center.Address,
        //            HospitalContact = $"Tel: {center.PhoneNumber} | Email: {center.Email}"
        //        };

        //        // Générer le PDF avec QuestPDF
        //        var document = new PaymentReceiptDocument(receiptModel);
        //        return document.GeneratePdf();
        //    }
        //    catch (Exception ex)
        //    {
        //        await _logger.LogErrorAsync("PaymentService", "GenerateReceiptError",
        //            $"Erreur lors de la génération du reçu pour le paiement {paymentId}",
        //            details: new { PaymentId = paymentId, Error = ex.Message });
        //        throw;
        //    }
        //}

        /// <summary>
        /// Récupère les paiements avec pagination et filtres
        /// </summary>
        public async Task<(List<PaymentViewModel> Items, int TotalCount)> GetPaymentsAsync(PaymentFilters filters)
        {
            try
            {
                int totalCount = 0;

                var payment = await _paymentRepository.QueryListAsync<PaymentViewModel>(query =>
                {
                    query = query
                   .Include(p => p.Patient)
                   .Include(p => p.PaymentMethod)
                   .Include(p => p.HospitalCenter)
                   .Include(p => p.ReceivedByNavigation)
                   .AsQueryable();

                    // Appliquer les filtres
                    if (filters.PatientId.HasValue)
                    {
                        query = query.Where(p => p.PatientId == filters.PatientId.Value);
                    }

                    if (filters.HospitalCenterId.HasValue)
                    {
                        query = query.Where(p => p.HospitalCenterId == filters.HospitalCenterId.Value);
                    }

                    if (filters.PaymentMethodId.HasValue)
                    {
                        query = query.Where(p => p.PaymentMethodId == filters.PaymentMethodId.Value);
                    }

                    if (filters.ReceivedBy.HasValue)
                    {
                        query = query.Where(p => p.ReceivedBy == filters.ReceivedBy.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(filters.ReferenceType))
                    {
                        query = query.Where(p => p.ReferenceType == filters.ReferenceType);
                    }

                    if (filters.FromDate.HasValue)
                    {
                        var fromDate = filters.FromDate.Value.Date;
                        query = query.Where(p => p.PaymentDate >= fromDate);
                    }

                    if (filters.ToDate.HasValue)
                    {
                        var toDate = filters.ToDate.Value.Date.AddDays(1).AddMilliseconds(-1);
                        query = query.Where(p => p.PaymentDate <= toDate);
                    }

                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var searchTerm = filters.SearchTerm.ToLower();
                        query = query.Where(p =>
                            (p.Patient != null && (p.Patient.FirstName.ToLower().Contains(searchTerm) ||
                                                  p.Patient.LastName.ToLower().Contains(searchTerm))) ||
                            (p.TransactionReference != null && p.TransactionReference.ToLower().Contains(searchTerm)) ||
                            (p.Notes != null && p.Notes.ToLower().Contains(searchTerm)) ||
                            p.ReferenceType.ToLower().Contains(searchTerm) ||
                            p.ReferenceId.ToString().Contains(searchTerm) ||
                            p.Amount.ToString().Contains(searchTerm)
                        );
                    }

                    // Compter le nombre total
                    int totalCount = query.Count();

                    // Pré-préparer la projection pour la réutiliser
                    return  query
                        .OrderByDescending(p => p.PaymentDate)
                        .Skip((filters.PageIndex - 1) * filters.PageSize)
                        .Take(filters.PageSize)
                        .Select(p => new PaymentViewModel
                        {
                            Id = p.Id,
                            ReferenceType = p.ReferenceType,
                            ReferenceId = p.ReferenceId,
                            ReferenceDescription = p.ReferenceType == "CareEpisode" ? "Épisode de soins" :
                                                  p.ReferenceType == "Examination" ? "Examen" : p.ReferenceType,
                            PatientId = p.PatientId,
                            PatientName = p.Patient != null ? $"{p.Patient.FirstName} {p.Patient.LastName}" : "",
                            HospitalCenterId = p.HospitalCenterId,
                            HospitalCenterName = p.HospitalCenter.Name,
                            PaymentMethodId = p.PaymentMethodId,
                            PaymentMethodName = p.PaymentMethod.Name,
                            Amount = p.Amount,
                            PaymentDate = p.PaymentDate,
                            ReceivedById = p.ReceivedBy,
                            ReceivedByName = $"{p.ReceivedByNavigation.FirstName} {p.ReceivedByNavigation.LastName}",
                            TransactionReference = p.TransactionReference,
                            Notes = p.Notes,
                            IsCancelled = p.Notes != null && p.Notes.StartsWith("[CANCELLED]"),
                            CancellationReason = p.Notes != null && p.Notes.StartsWith("[CANCELLED]")
                                ? p.Notes.Replace("[CANCELLED]", "").Trim()
                                : null,
                            CreatedAt = p.CreatedAt,
                            CreatedBy = p.CreatedBy,
                            CreatedByName = p.CreatedBy.ToString() // À remplacer par le nom réel dans une implémentation complète
                        });

                });


                foreach (var item in payment)
                {
                    item.ReferenceDescription = await GetDetailedReferenceDescriptionAsync(item.ReferenceType, item.ReferenceId);
                }

                return (payment, totalCount);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "GetPaymentsError",
                    "Erreur lors de la récupération des paiements",
                    details: new { Filters = filters, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Annule un paiement
        /// </summary>
        public async Task<OperationResult> CancelPaymentAsync(int paymentId, string reason, int modifiedBy)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return OperationResult.Error("Paiement introuvable");
                }

                // Vérifier si le paiement est déjà annulé
                if (payment.Notes != null && payment.Notes.StartsWith("[CANCELLED]"))
                {
                    return OperationResult.Error("Ce paiement est déjà annulé");
                }

                // Sauvegarder les notes actuelles
                string originalNotes = payment.Notes ?? "";

                // Modifier les notes pour indiquer l'annulation
                payment.Notes = $"[CANCELLED] {reason}\nNotes originales: {originalNotes}";
                payment.ModifiedBy = modifiedBy;
                payment.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                // Mettre à jour le paiement
                await _paymentRepository.UpdateAsync(payment);

                // Annuler l'impact sur les soldes
                await ReverseReferenceBalanceAsync(payment.ReferenceType, payment.ReferenceId, payment.Amount);

                // Audit
                await _auditService.LogActionAsync(
                    modifiedBy,
                    "PAYMENT_CANCEL",
                    "Payment",
                    paymentId,
                    new { OriginalNotes = originalNotes, Amount = payment.Amount },
                    new { Reason = reason },
                    $"Paiement {paymentId} annulé: {reason}"
                );

                // Journalisation
                await _logger.LogWarningAsync("PaymentService", "PaymentCancelled",
                    $"Paiement {paymentId} annulé",
                    modifiedBy,
                    payment.HospitalCenterId,
                    details: new { PaymentId = paymentId, Amount = payment.Amount, Reason = reason });

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "CancelPaymentError",
                    $"Erreur lors de l'annulation du paiement {paymentId}",
                    modifiedBy,
                    null,
                    details: new { PaymentId = paymentId, Reason = reason, Error = ex.Message });

                return OperationResult.Error("Une erreur est survenue lors de l'annulation du paiement");
            }
        }

        // Méthodes privées d'aide

        /// <summary>
        /// Vérifie si un type de référence est valide
        /// </summary>
        private bool IsValidReferenceType(string referenceType)
        {
            return referenceType is "CareEpisode" or "Examination";
        }

        /// <summary>
        /// Valide l'existence d'une référence
        /// </summary>
        private async Task<bool> ValidateReferenceAsync(string referenceType, int referenceId)
        {
            switch (referenceType)
            {
                case "CareEpisode":
                    var careEpisode = await _careEpisodeRepository.GetByIdAsync(referenceId);
                    return careEpisode != null;
                case "Examination":
                    var examination = await _examinationRepository.GetByIdAsync(referenceId);
                    return examination != null;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Obtient une description basique pour une référence
        /// </summary>
        private string GetReferenceDescription(string referenceType, int referenceId)
        {
            return referenceType switch
            {
                "CareEpisode" => "Épisode de soins",
                "Examination" => "Examen",
                _ => referenceType
            };
        }

        /// <summary>
        /// Obtient une description détaillée pour une référence
        /// </summary>
        private async Task<string> GetDetailedReferenceDescriptionAsync(string referenceType, int referenceId)
        {
            try
            {
                switch (referenceType)
                {
                    case "CareEpisode":
                        var careEpisode = await _careEpisodeRepository.QuerySingleAsync(q =>
                            q.Where(ce => ce.Id == referenceId)
                             .Include(ce => ce.Diagnosis)
                             .Select(ce => new { ce.Id, DiagnosisName = ce.Diagnosis.DiagnosisName }));

                        return careEpisode != null
                            ? $"Épisode de soins ({careEpisode.DiagnosisName})"
                            : "Épisode de soins";

                    case "Examination":
                        var examination = await _examinationRepository.QuerySingleAsync(q =>
                            q.Where(e => e.Id == referenceId)
                             .Include(e => e.ExaminationType)
                             .Select(e => new { e.Id, TypeName = e.ExaminationType.Name }));

                        return examination != null
                            ? $"Examen {examination.TypeName}"
                            : "Examen";

                    default:
                        return referenceType;
                }
            }
            catch (Exception)
            {
                // En cas d'erreur, retourner la description basique
                return GetReferenceDescription(referenceType, referenceId);
            }
        }

        /// <summary>
        /// Met à jour le solde d'une référence après un paiement
        /// </summary>
        private async Task UpdateReferenceBalanceAsync(string referenceType, int referenceId, decimal amount)
        {
            try
            {
                switch (referenceType)
                {
                    case "CareEpisode":
                        var careEpisode = await _careEpisodeRepository.GetByIdAsync(referenceId);
                        if (careEpisode != null)
                        {
                            // Incrémenter le montant payé et décrémenter le solde restant
                            careEpisode.AmountPaid += amount;
                            careEpisode.RemainingBalance = Math.Max(0, careEpisode.TotalCost - careEpisode.AmountPaid);

                            // Si tout est payé, mettre à jour le statut si nécessaire
                            if (careEpisode.RemainingBalance == 0 && careEpisode.Status == "Active")
                            {
                                // Optionnel: Mise à jour du statut selon la logique métier
                                // careEpisode.Status = "Completed";
                            }

                            await _careEpisodeRepository.UpdateAsync(careEpisode);
                        }
                        break;

                    case "Examination":
                        // Pour les examens, il n'y a pas de champ de solde à mettre à jour dans le modèle actuel
                        // Dans une implémentation complète, il faudrait ajouter cette logique
                        break;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "UpdateReferenceBalanceError",
                    $"Erreur lors de la mise à jour du solde pour {referenceType} #{referenceId}",
                    details: new { ReferenceType = referenceType, ReferenceId = referenceId, Amount = amount, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Annule l'impact d'un paiement sur le solde d'une référence
        /// </summary>
        private async Task ReverseReferenceBalanceAsync(string referenceType, int referenceId, decimal amount)
        {
            try
            {
                // Inverser le montant pour l'annulation
                await UpdateReferenceBalanceAsync(referenceType, referenceId, -amount);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "ReverseReferenceBalanceError",
                    $"Erreur lors de l'annulation du solde pour {referenceType} #{referenceId}",
                    details: new { ReferenceType = referenceType, ReferenceId = referenceId, Amount = amount, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Calcule le montant total dû par un patient
        /// </summary>
        private async Task<decimal> CalculatePatientTotalDueAsync(int patientId)
        {
            try
            {
                // Somme des épisodes de soins
                var careEpisodesTotal = await _careEpisodeRepository.SumAsync(
                    q => q.Where(ce => ce.PatientId == patientId),
                    ce => ce.TotalCost);

                // Somme des examens (dans une implémentation complète)
                // var examinationsTotal = await _examinationRepository.SumAsync(
                //     q => q.Where(e => e.PatientId == patientId),
                //     e => e.FinalPrice);

                // Pour l'instant, on retourne seulement le total des épisodes de soins
                return careEpisodesTotal;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("PaymentService", "CalculatePatientTotalDueError",
                    $"Erreur lors du calcul du montant total dû par le patient {patientId}",
                    details: new { PatientId = patientId, Error = ex.Message });
                throw;
            }
        }
    }
}