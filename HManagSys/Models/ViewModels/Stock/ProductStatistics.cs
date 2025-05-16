namespace HManagSys.Models.ViewModels.Stock
{

    /// <summary>
    /// Statistiques sur les produits
    /// </summary>
    public class ProductStatistics
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int ProductsWithLowStock { get; set; }
        public int ProductsWithCriticalStock { get; set; }
        public decimal AveragePrice { get; set; }
        public int CategoriesUsed { get; set; }

        public double ActivePercentage =>
            TotalProducts > 0 ? (double)ActiveProducts / TotalProducts * 100 : 0;

        public double LowStockPercentage =>
            ActiveProducts > 0 ? (double)ProductsWithLowStock / ActiveProducts * 100 : 0;
    }
}
