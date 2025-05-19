using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models.ViewModels.Documents;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Models.ViewModels.Payments;
using HManagSys.Services.Documents;
using HManagSys.Services.Interfaces;

namespace HManagSys.Services.Implementations;

/// <summary>
/// Implémentation du service de génération de documents
/// </summary>
public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly IExaminationService _examinationService;
    private readonly IPrescriptionService _prescriptionService;
    private readonly IPaymentService _paymentService;
    private readonly IHospitalCenterRepository _hospitalCenterService;
    private readonly IApplicationLogger _logger;

    public DocumentGenerationService(
        IExaminationService examinationService,
        IPrescriptionService prescriptionService,
        IPaymentService paymentService,
        IHospitalCenterRepository hospitalCenterService,
        IApplicationLogger logger)
    {
        _examinationService = examinationService;
        _prescriptionService = prescriptionService;
        _paymentService = paymentService;
        _hospitalCenterService = hospitalCenterService;
        _logger = logger;
    }

    /// <summary>
    /// Génère un PDF pour une prescription
    /// </summary>
    public async Task<byte[]> GeneratePrescriptionPdfAsync(int prescriptionId)
    {
        try
        {
            var prescription = await _prescriptionService.GetByIdAsync(prescriptionId);
            if (prescription == null)
            {
                throw new Exception($"Prescription {prescriptionId} introuvable");
            }

            var center = await _hospitalCenterService.GetByIdAsync(prescription.HospitalCenterId);
            if (center == null)
            {
                throw new Exception($"Centre hospitalier {prescription.HospitalCenterId} introuvable");
            }

            var model = new PrescriptionPdfViewModel
            {
                PrescriptionId = prescription.Id,
                Title = $"Prescription - {prescription.FormattedDate}",
                HospitalName = center.Name,
                HospitalAddress = center.Address,
                HospitalContact = $"Tel: {center.PhoneNumber} | Email: {center.Email}",
                PatientName = prescription.PatientName,
                PatientInfo = $"ID: {prescription.PatientId}",
                DoctorName = prescription.PrescribedByName,
                DiagnosisName = prescription.DiagnosisName ?? "Diagnostic non spécifié",
                PrescriptionDate = prescription.FormattedDate,
                Instructions = prescription.Instructions ?? "",
                Items = prescription.Items.Select(item => new PrescriptionItemPdfViewModel
                {
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitOfMeasure = "", // À compléter si disponible
                    Dosage = item.FormattedDosage,
                    Frequency = item.FormattedFrequency,
                    Duration = item.FormattedDuration,
                    Instructions = item.FormattedInstructions
                }).ToList()
            };

            var document = new PrescriptionDocument(model);
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("DocumentGeneration", "PrescriptionPdfError",
                $"Erreur lors de la génération du PDF pour la prescription {prescriptionId}",
                details: new { PrescriptionId = prescriptionId, Error = ex.Message });
            throw;
        }
    }

    /// <summary>
    /// Génère un PDF pour un résultat d'examen
    /// </summary>
    public async Task<byte[]> GenerateExaminationResultPdfAsync(int examinationId)
    {
        try
        {
            var examination = await _examinationService.GetByIdAsync(examinationId);
            if (examination == null)
            {
                throw new Exception($"Examen {examinationId} introuvable");
            }

            if (examination.Result == null)
            {
                throw new Exception($"Aucun résultat disponible pour l'examen {examinationId}");
            }

            var center = await _hospitalCenterService.GetByIdAsync(examination.HospitalCenterId);
            if (center == null)
            {
                throw new Exception($"Centre hospitalier {examination.HospitalCenterId} introuvable");
            }

            var model = new ExaminationResultPdfViewModel
            {
                ExaminationId = examination.Id,
                Title = $"Résultat d'examen - {examination.ExaminationTypeName}",
                HospitalName = center.Name,
                HospitalAddress = center.Address,
                HospitalContact = $"Tel: {center.PhoneNumber} | Email: {center.Email}",
                PatientName = examination.PatientName,
                PatientInfo = $"ID: {examination.PatientId}",
                ExaminationType = examination.ExaminationTypeName,
                RequestedBy = examination.RequestedByName,
                PerformedBy = examination.PerformedByName ?? "Non spécifié",
                RequestDate = examination.RequestDate.ToString("dd/MM/yyyy HH:mm"),
                PerformedDate = examination.PerformedDate?.ToString("dd/MM/yyyy HH:mm") ?? "Non spécifié",
                ResultData = examination.Result.ResultData ?? "Aucune donnée de résultat",
                ResultNotes = examination.Result.ResultNotes,
                AttachmentPath = examination.Result.AttachmentPath
            };

            var document = new ExaminationResultDocument(model);
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("DocumentGeneration", "ExaminationResultPdfError",
                $"Erreur lors de la génération du PDF pour le résultat d'examen {examinationId}",
                details: new { ExaminationId = examinationId, Error = ex.Message });
            throw;
        }
    }

    /// <summary>
    /// Génère un PDF pour un reçu de paiement
    /// </summary>
    public async Task<byte[]> GenerateReceiptPdfAsync(int paymentId)
    {
        try
        {
            var payment = await _paymentService.GetByIdAsync(paymentId);
            if (payment == null)
            {
                throw new Exception($"Paiement {paymentId} introuvable");
            }

            var center = await _hospitalCenterService.GetByIdAsync(payment.HospitalCenterId);
            if (center == null)
            {
                throw new Exception($"Centre hospitalier {payment.HospitalCenterId} introuvable");
            }

            var model = new ReceiptPdfViewModel
            {
                PaymentId = payment.Id,
                Title = $"Reçu de paiement - {payment.FormattedDate}",
                HospitalName = center.Name,
                HospitalAddress = center.Address,
                HospitalContact = $"Tel: {center.PhoneNumber} | Email: {center.Email}",
                PatientName = payment.PatientName,
                ReferenceType = payment.ReferenceDescription,
                ReferenceDetails = payment.ReferenceText,
                PaymentMethod = payment.PaymentMethodName,
                PaymentDate = payment.FormattedDate,
                ReceivedBy = payment.ReceivedByName,
                Amount = payment.Amount,
                TransactionReference = payment.TransactionReference,
                Notes = payment.Notes,
                IsCancelled = payment.IsCancelled,
                CancellationReason = payment.CancellationReason
            };

            var document = new PaymentReceiptDocument(model);
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("DocumentGeneration", "ReceiptPdfError",
                $"Erreur lors de la génération du PDF pour le reçu de paiement {paymentId}",
                details: new { PaymentId = paymentId, Error = ex.Message });
            throw;
        }
    }
}