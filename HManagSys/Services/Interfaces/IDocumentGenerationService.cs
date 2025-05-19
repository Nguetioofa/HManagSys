using HManagSys.Models.ViewModels.Documents;

namespace HManagSys.Services.Interfaces;

/// <summary>
/// Service pour la génération de documents PDF
/// </summary>
public interface IDocumentGenerationService
{
    /// <summary>
    /// Génère un PDF pour une prescription
    /// </summary>
    Task<byte[]> GeneratePrescriptionPdfAsync(int prescriptionId);

    /// <summary>
    /// Génère un PDF pour un résultat d'examen
    /// </summary>
    Task<byte[]> GenerateExaminationResultPdfAsync(int examinationId);

    /// <summary>
    /// Génère un PDF pour un reçu de paiement
    /// </summary>
    Task<byte[]> GenerateReceiptPdfAsync(int paymentId);
}