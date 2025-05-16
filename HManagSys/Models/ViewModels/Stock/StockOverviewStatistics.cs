namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Statistiques pour la vue d'ensemble des stocks
    /// </summary>
    public class StockOverviewStatistics
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int ProductsInStock { get; set; }
        public int ProductsOutOfStock { get; set; }
        public int CriticalStockAlerts { get; set; }
        public int LowStockAlerts { get; set; }
        public int MovementsToday { get; set; }
        public int PendingTransfers { get; set; }
        public decimal TotalStockValue { get; set; }

        // Pourcentages calculés
        public double ProductsInStockPercentage =>
            ActiveProducts > 0 ? (double)ProductsInStock / ActiveProducts * 100 : 0;

        public double CriticalStockPercentage =>
            ProductsInStock > 0 ? (double)CriticalStockAlerts / ProductsInStock * 100 : 0;

        public string TotalValueText => $"{TotalStockValue:N0} FCFA";
    }
}
