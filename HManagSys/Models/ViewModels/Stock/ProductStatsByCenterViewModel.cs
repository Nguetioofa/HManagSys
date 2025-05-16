namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Statistiques d'un produit par centre
    /// </summary>
    public class ProductStatsByCenterViewModel
    {
        public int TotalCenters { get; set; }
        public int CentersWithStock { get; set; }
        public int CentersLowStock { get; set; }
        public int CentersCriticalStock { get; set; }
        public decimal TotalStock { get; set; }
        public decimal TotalValue { get; set; }
        public int TotalMovements30Days { get; set; }
    }
}
