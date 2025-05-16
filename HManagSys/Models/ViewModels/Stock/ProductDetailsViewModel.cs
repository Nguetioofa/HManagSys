namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour les détails d'un produit
    /// </summary>
    public class ProductDetailsViewModel
    {
        public ProductViewModel Product { get; set; } = new();
        public List<ProductStockByCenterViewModel> StockByCenter { get; set; } = new();
        public List<RecentMovementViewModel> RecentMovements { get; set; } = new();
        public ProductStatsByCenterViewModel Statistics { get; set; } = new();
    }
}
