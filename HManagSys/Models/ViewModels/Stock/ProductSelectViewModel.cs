namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel simple pour les listes déroulantes de produits
    /// </summary>
    public class ProductSelectViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public bool IsActive { get; set; }

        public string DisplayText => $"{Name} ({UnitOfMeasure})";
        public string PriceText => $"{SellingPrice:N0} FCFA";
    }
}
