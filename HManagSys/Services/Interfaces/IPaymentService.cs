using HManagSys.Models.ViewModels.Patients;
using HManagSys.Models.ViewModels.Payments;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des paiements
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Récupère un paiement par son ID
        /// </summary>
        Task<PaymentViewModel?> GetByIdAsync(int id);

        /// <summary>
        /// Crée un nouveau paiement
        /// </summary>
        Task<OperationResult<PaymentViewModel>> CreatePaymentAsync(CreatePaymentViewModel model, int createdBy);

        /// <summary>
        /// Récupère tous les paiements liés à une référence
        /// (ex: examination, careepisode, etc.)
        /// </summary>
        Task<List<PaymentViewModel>> GetPaymentsByReferenceAsync(string referenceType, int referenceId);

        /// <summary>
        /// Récupère l'historique des paiements d'un patient
        /// </summary>
        Task<List<PaymentViewModel>> GetPatientPaymentHistoryAsync(int patientId);

        /// <summary>
        /// Récupère les méthodes de paiement disponibles
        /// </summary>
        Task<List<PaymentMethodViewModel>> GetPaymentMethodsAsync();

        /// <summary>
        /// Récupère un résumé des paiements pour un patient
        /// </summary>
        Task<PaymentSummaryViewModel> GetPatientPaymentSummaryAsync(int patientId);

        /// <summary>
        /// Génère un reçu de paiement
        /// </summary>
       // Task<byte[]> GenerateReceiptAsync(int paymentId);

        /// <summary>
        /// Récupère les paiements avec pagination et filtres
        /// </summary>
        Task<(List<PaymentViewModel> Items, int TotalCount)> GetPaymentsAsync(PaymentFilters filters);

        /// <summary>
        /// Annule un paiement
        /// </summary>
        Task<OperationResult> CancelPaymentAsync(int paymentId, string reason, int modifiedBy);
    }
}