
using HManagSys.Models.EfModels;
using HManagSys.Models.Interfaces;
using HManagSys.Models.ViewModels.Sales;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des ventes
    /// "Le système de caisse et facturation pour le centre hospitalier"
    /// </summary>
    public interface ISaleService
    {
        // ===== OPÉRATIONS CRUD =====

        /// <summary>
        /// Récupère une vente par son ID
        /// </summary>
        Task<SaleViewModel?> GetByIdAsync(int id);

        Task<List<Sale>> QueryListAsync(Func<IQueryable<Sale>, IQueryable<Sale>> queryBuilder);

        /// <summary>
        /// Récupère les ventes avec filtres et pagination
        /// </summary>
        Task<(List<SaleViewModel> Sales, int TotalCount)> GetSalesAsync(SaleFilters filters);

        /// <summary>
        /// Crée une nouvelle vente
        /// </summary>
        Task<OperationResult<SaleViewModel>> CreateSaleAsync(CreateSaleViewModel model, int createdBy,
            bool immediatePayment = false, int? paymentMethodId = null, string? transactionReference = null);

        /// <summary>
        /// Met à jour une vente existante
        /// </summary>
        Task<OperationResult> UpdateSaleAsync(int id, UpdateSaleViewModel model, int modifiedBy);

        /// <summary>
        /// Annule une vente
        /// </summary>
        Task<OperationResult> CancelSaleAsync(int id, string reason, int modifiedBy);

        // ===== GESTION DU PANIER =====

        /// <summary>
        /// Ajoute un produit au panier
        /// </summary>
        Task<CartViewModel> AddToCartAsync(CartItemViewModel item, CartViewModel? existingCart = null);

        /// <summary>
        /// Supprime un produit du panier
        /// </summary>
        Task<CartViewModel> RemoveFromCartAsync(int productId, CartViewModel existingCart);

        /// <summary>
        /// Met à jour la quantité d'un produit dans le panier
        /// </summary>
        Task<CartViewModel> UpdateCartItemQuantityAsync(int productId, decimal quantity, CartViewModel existingCart);

        /// <summary>
        /// Applique une remise au panier
        /// </summary>
        Task<CartViewModel> ApplyDiscountAsync(decimal discountAmount, string? discountReason, CartViewModel existingCart);

        // ===== OPÉRATIONS SPÉCIFIQUES =====

        /// <summary>
        /// Récupère l'historique des ventes d'un patient
        /// </summary>
        Task<List<SaleViewModel>> GetPatientSalesHistoryAsync(int patientId);

        /// <summary>
        /// Récupère un résumé des ventes pour un centre sur une période
        /// </summary>
        Task<SaleSummaryViewModel> GetSaleSummaryAsync(int hospitalCenterId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Génère un reçu de vente en PDF
        /// </summary>
        Task<byte[]> GenerateReceiptAsync(int saleId);

        // ===== VALIDATION =====

        /// <summary>
        /// Valide le contenu d'un panier avant création de la vente
        /// </summary>
        Task<OperationResult> ValidateCartAsync(CartViewModel cart, int hospitalCenterId);

        /// <summary>
        /// Vérifie la disponibilité des produits dans le stock
        /// </summary>
        Task<List<ProductAvailabilityViewModel>> CheckProductsAvailabilityAsync(List<CartItemViewModel> items, int hospitalCenterId);

        /// <summary>
        /// Met à jour le statut de paiement d'une vente
        /// </summary>
        Task<OperationResult> UpdateSalePaymentStatusAsync(int saleId, string status, int modifiedBy);
    }
}