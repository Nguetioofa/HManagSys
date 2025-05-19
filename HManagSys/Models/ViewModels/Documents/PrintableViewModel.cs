using HManagSys.Models.ViewModels.Documents;

namespace HManagSys.Models.ViewModels.Documents;

/// <summary>
/// ViewModel de base pour tous les documents imprimables
/// </summary>
public abstract class PrintableViewModel
{
    public string Title { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
    public string HospitalName { get; set; } = string.Empty;
    public string HospitalAddress { get; set; } = string.Empty;
    public string HospitalContact { get; set; } = string.Empty;
    public string HospitalLogo { get; set; } = string.Empty;
    public string FooterText { get; set; } = "Document généré le " + DateTime.Now.ToString("dd/MM/yyyy à HH:mm");
}

/// <summary>
/// ViewModel pour un document PDF
/// </summary>
public class PdfDocumentViewModel : PrintableViewModel
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientInfo { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel pour une prescription imprimable
/// </summary>
public class PrescriptionPdfViewModel : PrintableViewModel
{
    public int PrescriptionId { get; set; }
    public string PrescriptionNumber => $"PRES-{PrescriptionId:D6}";
    public string PatientName { get; set; } = string.Empty;
    public string PatientInfo { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string DiagnosisName { get; set; } = string.Empty;
    public string PrescriptionDate { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<PrescriptionItemPdfViewModel> Items { get; set; } = new();
}

/// <summary>
/// ViewModel pour un item de prescription imprimable
/// </summary>
public class PrescriptionItemPdfViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel pour un résultat d'examen imprimable
/// </summary>
public class ExaminationResultPdfViewModel : PrintableViewModel
{
    public int ExaminationId { get; set; }
    public string ExaminationNumber => $"EXAM-{ExaminationId:D6}";
    public string PatientName { get; set; } = string.Empty;
    public string PatientInfo { get; set; } = string.Empty;
    public string ExaminationType { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string RequestDate { get; set; } = string.Empty;
    public string PerformedDate { get; set; } = string.Empty;
    public string ResultData { get; set; } = string.Empty;
    public string ResultNotes { get; set; } = string.Empty;
    public string AttachmentPath { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel pour un reçu de paiement imprimable
/// </summary>
public class ReceiptPdfViewModel : PrintableViewModel
{
    public int PaymentId { get; set; }
    public string ReceiptNumber => $"REÇU-{PaymentId:D6}";
    public string PatientName { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceDetails { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentDate { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FormattedAmount => $"{Amount:N0} FCFA";
    public string TransactionReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsCancelled { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
}