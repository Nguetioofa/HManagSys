using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Stock;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des produits
    /// Comme un pharmacien en chef qui gère tout l'inventaire médical
    /// </summary>
    public interface IProductService
    {
        // ===== OPÉRATIONS CRUD =====

        /// <summary>
        /// Récupère tous les produits avec filtres et pagination
        /// </summary>
        Task<(List<ProductViewModel> Products, int TotalCount)> GetProductsAsync(
            ProductFilters filters);

        Task<Product?> GetProductByNameAsync(string name);
        /// <summary>
        /// Récupère un produit par son ID avec toutes ses informations
        /// </summary>
        Task<ProductViewModel?> GetProductByIdAsync(int id);

        /// <summary>
        /// Récupère les détails complets d'un produit
        /// </summary>
        Task<ProductDetailsViewModel?> GetProductDetailsAsync(int id, int? centerId = null);

        /// <summary>
        /// Crée un nouveau produit
        /// </summary>
        Task<OperationResult<ProductViewModel>> CreateProductAsync(
            CreateProductViewModel model,
            int createdBy);

        /// <summary>
        /// Modifie un produit existant
        /// </summary>
        Task<OperationResult<ProductViewModel>> UpdateProductAsync(
            int id,
            EditProductViewModel model,
            int modifiedBy);

        /// <summary>
        /// Supprime un produit (soft delete si possible)
        /// </summary>
        Task<OperationResult> DeleteProductAsync(int id, int deletedBy);

        /// <summary>
        /// Active ou désactive un produit
        /// </summary>
        Task<OperationResult> ToggleProductStatusAsync(int id, bool isActive, int modifiedBy);

        // ===== GESTION DU STOCK =====

        /// <summary>
        /// Initialise le stock d'un produit dans un centre
        /// </summary>
        Task<OperationResult> InitializeStockAsync(
            int productId,
            int centerId,
            decimal quantity,
            decimal? minThreshold,
            decimal? maxThreshold,
            int createdBy);

        /// <summary>
        /// Initialise le stock pour plusieurs produits dans un centre
        /// </summary>
        Task<OperationResult> InitializeBulkStockAsync(
            InitializeStockViewModel model,
            int createdBy);

        /// <summary>
        /// Met à jour les seuils d'un produit dans un centre
        /// </summary>
        Task<OperationResult> UpdateStockThresholdsAsync(
            int productId,
            int centerId,
            decimal? minThreshold,
            decimal? maxThreshold,
            int modifiedBy);

        /// <summary>
        /// Ajuste manuellement le stock d'un produit
        /// </summary>
        Task<OperationResult> AdjustStockAsync(
            int productId,
            int centerId,
            decimal quantity,
            string reason,
            int adjustedBy);

        // ===== REQUÊTES SPÉCIALISÉES =====

        /// <summary>
        /// Récupère les produits actifs pour les listes déroulantes
        /// </summary>
        Task<List<ProductSelectViewModel>> GetActiveProductsForSelectAsync(int? categoryId = null);

        /// <summary>
        /// Récupère les produits avec stock faible
        /// </summary>
        Task<List<StockAlertViewModel>> GetLowStockProductsAsync(int? centerId = null);

        /// <summary>
        /// Récupère les produits avec stock critique
        /// </summary>
        Task<List<StockAlertViewModel>> GetCriticalStockProductsAsync(int? centerId = null);

        /// <summary>
        /// Récupère l'état des stocks pour un centre
        /// </summary>
        Task<StockOverviewViewModel> GetStockOverviewAsync(
            int centerId,
            StockOverviewFilters filters);

        /// <summary>
        /// Récupère l'historique des mouvements d'un produit
        /// </summary>
        Task<List<RecentMovementViewModel>> GetProductMovementHistoryAsync(
            int productId,
            int? centerId = null,
            int days = 30);

        /// <summary>
        /// Récupère les mouvements récents pour un centre
        /// </summary>
        Task<List<RecentStockMovementViewModel>> GetRecentMovementsAsync(
            int centerId,
            int limit = 10);

        /// <summary>
        /// Récupère les statistiques générales des produits
        /// </summary>
        Task<ProductStatistics> GetProductStatisticsAsync(int? centerId = null);

        /// <summary>
        /// Recherche des produits par nom ou code
        /// </summary>
        Task<List<ProductSelectViewModel>> SearchProductsAsync(
            string searchTerm,
            int? categoryId = null,
            int? centerId = null);

        // ===== VALIDATION =====

        /// <summary>
        /// Vérifie l'unicité du nom d'un produit
        /// </summary>
        Task<bool> IsProductNameUniqueAsync(string name, int? excludeId = null);

        /// <summary>
        /// Vérifie si un produit peut être supprimé
        /// </summary>
        Task<bool> CanDeleteProductAsync(int id);

        /// <summary>
        /// Valide les données d'un produit avant création/modification
        /// </summary>
        Task<ValidationResult> ValidateProductAsync(
            string name,
            int categoryId,
            decimal price,
            int? excludeId = null);

        /// <summary>
        /// Valide les seuils de stock
        /// </summary>
        Task<ValidationResult> ValidateStockThresholdsAsync(
            decimal? minThreshold,
            decimal? maxThreshold);

        // ===== RAPPORTS ET ANALYSES =====

        /// <summary>
        /// Génère un rapport de stock pour un centre
        /// </summary>
        Task<StockReportViewModel> GenerateStockReportAsync(
            int centerId,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// Calcule la valeur totale du stock d'un centre
        /// </summary>
        Task<decimal> GetStockValueAsync(int centerId);

        /// <summary>
        /// Récupère les produits les plus vendus
        /// </summary>
        Task<List<TopSellingProductViewModel>> GetTopSellingProductsAsync(
            int centerId,
            int days = 30,
            int limit = 10);

        /// <summary>
        /// Récupère les produits les plus utilisés dans les soins
        /// </summary>
        Task<List<TopUsedProductViewModel>> GetTopUsedProductsAsync(
            int centerId,
            int days = 30,
            int limit = 10);

        // ===== PRÉVISIONS ET ALERTES =====

        /// <summary>
        /// Prédit les besoins de réapprovisionnement
        /// </summary>
        Task<List<ReorderSuggestionViewModel>> GetReorderSuggestionsAsync(
            int centerId,
            int daysAhead = 30);

        /// <summary>
        /// Calcule la vitesse de rotation d'un produit
        /// </summary>
        Task<decimal> GetProductTurnoverRateAsync(int productId, int centerId, int days = 30);

        /// <summary>
        /// Récupère les alertes de stock pour un centre
        /// </summary>
        Task<List<StockAlertViewModel>> GetStockAlertsAsync(int? centerId = null, string? severity = null);
    }

    // ===== CLASSES SUPPORT POUR LES RAPPORTS =====

    /// <summary>
    /// Rapport de stock complet
    /// </summary>
    public class StockReportViewModel
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; }

        public StockReportSummary Summary { get; set; } = new();
        public List<StockReportItem> Items { get; set; } = new();
        public List<StockMovementSummary> Movements { get; set; } = new();
        public List<StockAlertViewModel> Alerts { get; set; } = new();
    }

    /// <summary>
    /// Résumé du rapport de stock
    /// </summary>
    public class StockReportSummary
    {
        public int TotalProducts { get; set; }
        public int ProductsInStock { get; set; }
        public int ProductsOutOfStock { get; set; }
        public int ProductsLowStock { get; set; }
        public int ProductsCriticalStock { get; set; }
        public decimal TotalValue { get; set; }
        public int TotalMovements { get; set; }

        public double InStockPercentage =>
            TotalProducts > 0 ? (double)ProductsInStock / TotalProducts * 100 : 0;
    }

    /// <summary>
    /// Item du rapport de stock
    /// </summary>
    public class StockReportItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? MinThreshold { get; set; }
        public decimal? MaxThreshold { get; set; }
        public int MovementsCount { get; set; }
    }

    /// <summary>
    /// Résumé des mouvements par type
    /// </summary>
    public class StockMovementSummary
    {
        public string MovementType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal AverageQuantity { get; set; }
    }

    /// <summary>
    /// Produit le plus vendu
    /// </summary>
    public class TopSellingProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;

        public string QuantityText => $"{QuantitySold:N2} {UnitOfMeasure}";
        public string RevenueText => $"{Revenue:N0} FCFA";
    }

    /// <summary>
    /// Produit le plus utilisé dans les soins
    /// </summary>
    public class TopUsedProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal QuantityUsed { get; set; }
        public int CareServicesCount { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;

        public string QuantityText => $"{QuantityUsed:N2} {UnitOfMeasure}";
    }

    /// <summary>
    /// Suggestion de réapprovisionnement
    /// </summary>
    public class ReorderSuggestionViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal? MinThreshold { get; set; }
        public decimal SuggestedQuantity { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public int DaysUntilStockOut { get; set; }
        public decimal AverageConsumption { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;

        public string ReasonText => ReasonCode switch
        {
            "BelowMinimum" => "En dessous du seuil minimum",
            "PredictedStockOut" => "Rupture prévue",
            "HighConsumption" => "Consommation élevée",
            "SeasonalDemand" => "Demande saisonnière",
            _ => ReasonCode
        };

        public string PriorityLevel => DaysUntilStockOut switch
        {
            <= 3 => "Urgent",
            <= 7 => "Élevé",
            <= 14 => "Moyen",
            _ => "Bas"
        };

        public string PriorityBadge => PriorityLevel switch
        {
            "Urgent" => "badge bg-danger",
            "Élevé" => "badge bg-warning text-dark",
            "Moyen" => "badge bg-info text-dark",
            "Bas" => "badge bg-success",
            _ => "badge bg-secondary"
        };
    }
}