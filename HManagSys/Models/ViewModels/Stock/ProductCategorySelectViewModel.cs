namespace HManagSys.Models.ViewModels.Stock
{

    /// <summary>
    /// ViewModel simple pour les listes déroulantes
    /// </summary>
    public class ProductCategorySelectViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
