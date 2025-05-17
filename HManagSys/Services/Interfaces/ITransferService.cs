using HManagSys.Models;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Stock;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des transferts de stock entre centres
    /// </summary>
    public interface ITransferService
    {
        // ===== OPÉRATIONS DE DEMANDE =====

        /// <summary>
        /// Crée une nouvelle demande de transfert
        /// </summary>
        Task<OperationResult<int>> RequestTransferAsync(TransferRequestViewModel model, int requestedBy);

        /// <summary>
        /// Récupère les transferts en attente pour un centre
        /// </summary>
        Task<List<TransferViewModel>> GetPendingTransfersAsync(int centerId, string? role = null);

        /// <summary>
        /// Obtient les transferts en attente d'approbation pour un centre
        /// </summary>
        Task<List<TransferViewModel>> GetTransfersForApprovalAsync(int centerId);

        /// <summary>
        /// Vérifie si un transfert peut être créé
        /// </summary>
        Task<ValidationResult> ValidateTransferRequestAsync(int productId, int fromCenterId, int toCenterId, decimal quantity);

        // ===== OPÉRATIONS D'APPROBATION =====

        /// <summary>
        /// Approuve un transfert
        /// </summary>
        Task<OperationResult> ApproveTransferAsync(int transferId, int approvedBy, string comments);

        /// <summary>
        /// Rejette un transfert
        /// </summary>
        Task<OperationResult> RejectTransferAsync(int transferId, int rejectedBy, string reason);

        /// <summary>
        /// Annule un transfert
        /// </summary>
        Task<OperationResult> CancelTransferAsync(int transferId, int cancelledBy, string reason);

        // ===== OPÉRATIONS D'EXÉCUTION =====

        /// <summary>
        /// Complète un transfert en exécutant le mouvement de stock entre les centres
        /// </summary>
        Task<OperationResult> CompleteTransferAsync(int transferId, int completedBy);

        // ===== OPÉRATIONS DE CONSULTATION =====

        /// <summary>
        /// Récupère un transfert par son ID
        /// </summary>
        Task<TransferViewModel?> GetTransferByIdAsync(int id);

        /// <summary>
        /// Récupère l'historique des transferts avec filtres
        /// </summary>
        Task<(List<TransferViewModel> Transfers, int TotalCount)> GetTransfersAsync(
            TransferFilters filters,
            int? currentCenterId = null,
            int? currentUserId = null);

        /// <summary>
        /// Génère les statistiques des transferts pour un centre
        /// </summary>
        Task<TransferStatisticsViewModel> GetTransferStatisticsAsync(int? centerId = null);

        // ===== OPÉRATIONS DE SUPPORT =====

        /// <summary>
        /// Vérifie si l'utilisateur peut approuver un transfert
        /// </summary>
        Task<bool> CanUserApproveTransferAsync(int transferId, int userId);

        /// <summary>
        /// Récupère les centres disponibles pour un transfert
        /// </summary>
        Task<List<SelectOption>> GetAvailableCentersForTransferAsync(int? excludeCenterId = null);

        /// <summary>
        /// Récupère les produits disponibles dans un centre pour un transfert
        /// </summary>
        Task<List<SelectOption>> GetAvailableProductsForTransferAsync(int fromCenterId);

        /// <summary>
        /// Récupère la quantité disponible d'un produit dans un centre
        /// </summary>
        Task<decimal> GetAvailableQuantityAsync(int productId, int centerId);
    }
}