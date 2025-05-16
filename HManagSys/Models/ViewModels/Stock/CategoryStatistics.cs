namespace HManagSys.Models.ViewModels.Stock
{

    /// <summary>
    /// Statistiques sur les catégories
    /// </summary>
    public class CategoryStatistics
    {
        public int TotalCategories { get; set; }
        public int ActiveCategories { get; set; }
        public int InactiveCategories { get; set; }
        public int TotalProducts { get; set; }

        public double ActivePercentage =>
            TotalCategories > 0 ? (double)ActiveCategories / TotalCategories * 100 : 0;
    }

}
