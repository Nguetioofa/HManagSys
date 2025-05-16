namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel principal pour la vue d'ensemble des stocks
    /// </summary>
    public class StockOverviewViewModel
    {
        public int CurrentCenterId { get; set; }
        public string CurrentCenterName { get; set; } = string.Empty;
        public string CurrentUserRole { get; set; } = string.Empty;

        // Statistiques rapides
        public StockOverviewStatistics Statistics { get; set; } = new();

        // Filtres et pagination
        public StockOverviewFilters Filters { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();

        // Données principales
        public List<StockItemViewModel> StockItems { get; set; } = new();
        public List<StockAlertViewModel> CriticalAlerts { get; set; } = new();
        public List<ProductCategorySelectViewModel> AvailableCategories { get; set; } = new();

        // Dernières activités
        public List<RecentStockMovementViewModel> RecentMovements { get; set; } = new();
    }
}
