using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Stock;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service global pour la gestion des stocks
    /// Comme un directeur logistique qui supervise tous les mouvements
    /// </summary>
    public interface IStockService
    {
        // ===== MOUVEMENTS DE STOCK =====

        /// <summary>
        /// Enregistre un mouvement de stock
        /// </summary>
        Task<OperationResult> RecordMovementAsync(StockMovementRequest request);

        /// <summary>
        /// Enregistre plusieurs mouvements de stock en une transaction
        /// </summary>
        Task<OperationResult> RecordBulkMovementsAsync(List<StockMovementRequest> movements);

        /// <summary>
        /// Récupère l'historique des mouvements
        /// </summary>
        Task<(List<StockMovementViewModel> Movements, int TotalCount)> GetMovementHistoryAsync(
            StockMovementFilters filters);

        /// <summary>
        /// Récupère les mouvements d'un produit spécifique
        /// </summary>
        Task<List<StockMovementViewModel>> GetProductMovementsAsync(
            int productId,
            int? centerId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        // ===== SURVEILLANCE DU STOCK =====

        /// <summary>
        /// Vérifie tous les seuils et génère les alertes
        /// </summary>
        Task<OperationResult> CheckStockThresholdsAsync(int? centerId = null);

        /// <summary>
        /// Récupère toutes les alertes de stock
        /// </summary>
        Task<List<StockAlertDetailViewModel>> GetStockAlertsAsync(
            int? centerId = null,
            string? severity = null);

        /// <summary>
        /// Marque une alerte comme traitée
        /// </summary>
        Task<OperationResult> MarkAlertAsHandledAsync(int alertId, int handledBy, string? notes = null);

        /// <summary>
        /// Calcule le statut du stock pour un produit dans un centre
        /// </summary>
        Task<string> CalculateStockStatusAsync(int productId, int centerId);

        // ===== INVENTAIRES =====

        /// <summary>
        /// Lance un inventaire physique
        /// </summary>
        Task<OperationResult<InventorySession>> StartInventoryAsync(
            int centerId,
            int startedBy,
            List<int>? productIds = null);

        /// <summary>
        /// Enregistre un comptage d'inventaire
        /// </summary>
        Task<OperationResult> RecordInventoryCountAsync(
            int inventorySessionId,
            int productId,
            decimal countedQuantity,
            int countedBy,
            string? notes = null);

        /// <summary>
        /// Finalise un inventaire et ajuste les stocks
        /// </summary>
        Task<OperationResult<InventoryResult>> FinalizeInventoryAsync(
            int inventorySessionId,
            int finalizedBy);

        /// <summary>
        /// Récupère les sessions d'inventaire
        /// </summary>
        Task<List<InventorySessionViewModel>> GetInventorySessionsAsync(
            int? centerId = null,
            bool? isCompleted = null);

        // ===== ANALYSES ET RAPPORTS =====

        /// <summary>
        /// Génère un rapport de valorisation des stocks
        /// </summary>
        Task<StockValuationReportViewModel> GenerateValuationReportAsync(
            int? centerId = null,
            DateTime? asOfDate = null);

        /// <summary>
        /// Calcule les statistiques de rotation des stocks
        /// </summary>
        Task<List<StockTurnoverViewModel>> CalculateTurnoverRatesAsync(
            int centerId,
            int days = 30);

        /// <summary>
        /// Analyse la consommation des produits
        /// </summary>
        Task<List<ConsumptionAnalysisViewModel>> AnalyzeConsumptionAsync(
            int centerId,
            int days = 30);

        /// <summary>
        /// Prévoit les besoins futurs
        /// </summary>
        Task<List<DemandForecastViewModel>> ForecastDemandAsync(
            int centerId,
            int daysAhead = 30);

        // ===== CONSOLIDATION ET TRANSFERTS =====

        /// <summary>
        /// Calcule les opportunités de consolidation entre centres
        /// </summary>
        Task<List<ConsolidationOpportunityViewModel>> FindConsolidationOpportunitiesAsync();

        /// <summary>
        /// Suggère des transferts optimaux
        /// </summary>
        Task<List<OptimalTransferSuggestionViewModel>> SuggestOptimalTransfersAsync(
            int fromCenterId,
            int toCenterId);

        // ===== CONFIGURATION ET MAINTENANCE =====

        /// <summary>
        /// Met à jour les seuils de stock en masse
        /// </summary>
        Task<OperationResult> UpdateBulkThresholdsAsync(
            List<BulkThresholdUpdateRequest> updates,
            int modifiedBy);

        /// <summary>
        /// Nettoie les anciens mouvements de stock
        /// </summary>
        Task<OperationResult<int>> CleanupOldMovementsAsync(
            DateTime beforeDate,
            bool preserveAuditTrail = true);

        /// <summary>
        /// Recalcule tous les stocks d'un centre
        /// </summary>
        Task<OperationResult> RecalculateStockLevelsAsync(
            int centerId,
            int requestedBy);

        /// <summary>
        /// Synchronise les stocks entre centres (pour les audits)
        /// </summary>
        Task<OperationResult<StockSyncReport>> SynchronizeStockDataAsync(
            List<int> centerIds,
            int requestedBy);
    }

    // ===== CLASSES SUPPORT =====

    /// <summary>
    /// Demande de mouvement de stock
    /// </summary>
    public class StockMovementRequest
    {
        public int ProductId { get; set; }
        public int HospitalCenterId { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Notes { get; set; }
        public DateTime MovementDate { get; set; }
        public int CreatedBy { get; set; }
    }

    /// <summary>
    /// Mouvement de stock pour affichage
    /// </summary>
    public class StockMovementViewModel : RecentMovementViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int HospitalCenterId { get; set; }
        public decimal NewStockLevel { get; set; }
        public string NewStockText => $"{NewStockLevel:N2}";
    }

    /// <summary>
    /// Filtres pour l'historique des mouvements
    /// </summary>
    public class StockMovementFilters
    {
        public int? ProductId { get; set; }
        public int? HospitalCenterId { get; set; }
        public string? MovementType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ReferenceType { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// Alerte de stock détaillée
    /// </summary>
    public class StockAlertDetailViewModel : StockAlertViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsHandled { get; set; }
        public int? HandledBy { get; set; }
        public string? HandledByName { get; set; }
        public DateTime? HandledAt { get; set; }
        public string? HandlingNotes { get; set; }
        public int DaysActive { get; set; }

        public string StatusText => IsHandled ? "Traitée" : "Active";
        public string StatusBadge => IsHandled ? "badge bg-success" : "badge bg-warning";
        public string DaysActiveText => $"{DaysActive} jour(s)";
    }

    /// <summary>
    /// Session d'inventaire
    /// </summary>
    public class InventorySession
    {
        public int Id { get; set; }
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int StartedBy { get; set; }
        public string StartedByName { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public int? CompletedBy { get; set; }
        public string? CompletedByName { get; set; }
        public bool IsCompleted { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Session d'inventaire pour affichage
    /// </summary>
    public class InventorySessionViewModel : InventorySession
    {
        public int TotalProducts { get; set; }
        public int CountedProducts { get; set; }
        public int DiscrepanciesFound { get; set; }
        public decimal TotalAdjustmentValue { get; set; }

        public double ProgressPercentage =>
            TotalProducts > 0 ? (double)CountedProducts / TotalProducts * 100 : 0;

        public string StatusText => IsCompleted ? "Terminé" : "En cours";
        public string StatusBadge => IsCompleted ? "badge bg-success" : "badge bg-warning";
        public string ProgressText => $"{CountedProducts} / {TotalProducts}";
    }

    /// <summary>
    /// Résultat d'un inventaire
    /// </summary>
    public class InventoryResult
    {
        public int SessionId { get; set; }
        public int ProductsCounted { get; set; }
        public int DiscrepanciesFound { get; set; }
        public int AdjustmentsMade { get; set; }
        public decimal TotalValueAdjustment { get; set; }
        public List<InventoryDiscrepancy> Discrepancies { get; set; } = new();
    }

    /// <summary>
    /// Écart d'inventaire
    /// </summary>
    public class InventoryDiscrepancy
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal BookQuantity { get; set; }
        public decimal CountedQuantity { get; set; }
        public decimal Difference { get; set; }
        public decimal ValueImpact { get; set; }
        public string? Notes { get; set; }

        public string DifferenceText => $"{(Difference >= 0 ? "+" : "")}{Difference:N2}";
        public string ValueImpactText => $"{(ValueImpact >= 0 ? "+" : "")}{ValueImpact:N0} FCFA";
    }

    /// <summary>
    /// Rapport de valorisation des stocks
    /// </summary>
    public class StockValuationReportViewModel
    {
        public DateTime AsOfDate { get; set; }
        public int? CenterId { get; set; }
        public string? CenterName { get; set; }
        public List<StockValuationItem> Items { get; set; } = new();
        public StockValuationSummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Item de valorisation
    /// </summary>
    public class StockValuationItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
    }

    /// <summary>
    /// Résumé de valorisation
    /// </summary>
    public class StockValuationSummary
    {
        public int TotalProducts { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageValue { get; set; }
        public int ProductsOutOfStock { get; set; }
        public Dictionary<string, decimal> ValueByCategory { get; set; } = new();
    }

    /// <summary>
    /// Rotation des stocks
    /// </summary>
    public class StockTurnoverViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TurnoverRate { get; set; }
        public decimal AverageStock { get; set; }
        public decimal TotalConsumed { get; set; }
        public int DaysAnalyzed { get; set; }
        public string Status { get; set; } = string.Empty;

        public string TurnoverText => $"{TurnoverRate:N2}x";
        public string StatusBadge => Status switch
        {
            "Slow" => "badge bg-warning",
            "Normal" => "badge bg-success",
            "Fast" => "badge bg-info",
            "Very Fast" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }

    /// <summary>
    /// Analyse de consommation
    /// </summary>
    public class ConsumptionAnalysisViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalConsumed { get; set; }
        public decimal AverageDaily { get; set; }
        public decimal TrendPercentage { get; set; }
        public string TrendDirection { get; set; } = string.Empty;
        public List<DailyConsumption> DailyData { get; set; } = new();

        public string TrendText => $"{(TrendPercentage >= 0 ? "+" : "")}{TrendPercentage:N1}%";
        public string TrendIcon => TrendDirection switch
        {
            "UP" => "fas fa-arrow-up text-success",
            "DOWN" => "fas fa-arrow-down text-danger",
            "STABLE" => "fas fa-arrow-right text-muted",
            _ => "fas fa-minus text-muted"
        };
    }

    /// <summary>
    /// Consommation journalière
    /// </summary>
    public class DailyConsumption
    {
        public DateTime Date { get; set; }
        public decimal Quantity { get; set; }
    }

    /// <summary>
    /// Prévision de demande
    /// </summary>
    public class DemandForecastViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal PredictedDemand { get; set; }
        public decimal CurrentStock { get; set; }
        public int EstimatedStockOutDays { get; set; }
        public decimal SuggestedReorderQuantity { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public List<ForecastDataPoint> ForecastData { get; set; } = new();

        public string ConfidenceText => $"{ConfidenceLevel:N0}%";
        public string StockOutRisk => EstimatedStockOutDays switch
        {
            <= 3 => "Très élevé",
            <= 7 => "Élevé",
            <= 14 => "Moyen",
            <= 30 => "Bas",
            _ => "Très bas"
        };
    }

    /// <summary>
    /// Point de données de prévision
    /// </summary>
    public class ForecastDataPoint
    {
        public DateTime Date { get; set; }
        public decimal PredictedQuantity { get; set; }
        public decimal LowerBound { get; set; }
        public decimal UpperBound { get; set; }
    }

    /// <summary>
    /// Opportunité de consolidation
    /// </summary>
    public class ConsolidationOpportunityViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public List<CenterStockInfo> CentersInfo { get; set; } = new();
        public decimal TotalSystemStock { get; set; }
        public decimal AverageConsumption { get; set; }
        public int EstimatedDaysOfStock { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;

        public bool HasImbalance => CentersInfo.Any(c => c.Status == "Excess") &&
                                   CentersInfo.Any(c => c.Status == "Deficit");
    }

    /// <summary>
    /// Information de stock par centre
    /// </summary>
    public class CenterStockInfo
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal OptimalStock { get; set; }
        public decimal Variance { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Suggestion de transfert optimal
    /// </summary>
    public class OptimalTransferSuggestionViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int FromCenterId { get; set; }
        public string FromCenterName { get; set; } = string.Empty;
        public int ToCenterId { get; set; }
        public string ToCenterName { get; set; } = string.Empty;
        public decimal SuggestedQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal BenefitScore { get; set; }
        public int PriorityLevel { get; set; }

        public string PriorityText => PriorityLevel switch
        {
            1 => "Critique",
            2 => "Élevé",
            3 => "Moyen",
            4 => "Bas",
            _ => "Normal"
        };
    }

    /// <summary>
    /// Demande de mise à jour de seuils en masse
    /// </summary>
    public class BulkThresholdUpdateRequest
    {
        public int ProductId { get; set; }
        public int CenterId { get; set; }
        public decimal? MinimumThreshold { get; set; }
        public decimal? MaximumThreshold { get; set; }
    }

    /// <summary>
    /// Rapport de synchronisation des stocks
    /// </summary>
    public class StockSyncReport
    {
        public DateTime SyncDate { get; set; }
        public List<int> CenterIds { get; set; } = new();
        public int TotalProductsChecked { get; set; }
        public int DiscrepanciesFound { get; set; }
        public int ItemsFixed { get; set; }
        public List<StockSyncDiscrepancy> Discrepancies { get; set; } = new();
    }

    /// <summary>
    /// Écart de synchronisation
    /// </summary>
    public class StockSyncDiscrepancy
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public decimal ExpectedStock { get; set; }
        public decimal ActualStock { get; set; }
        public decimal Difference { get; set; }
        public string Issue { get; set; } = string.Empty;
        public bool WasFixed { get; set; }
    }
}