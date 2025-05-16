namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Statistiques des catégories
    /// </summary>
    public class ProductCategoryStatistics
    {
        public int TotalCategories { get; set; }
        public int ActiveCategories { get; set; }
        public int InactiveCategories { get; set; }
        public double ActivePercentage => TotalCategories > 0 ? (double)ActiveCategories / TotalCategories * 100 : 0;
    }
}
