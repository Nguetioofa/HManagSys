namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour la liste des produits avec filtres
    /// </summary>
    public class ProductListViewModel
    {
        public List<ProductViewModel> Products { get; set; } = new();
        public ProductFilters Filters { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public ProductStatistics Statistics { get; set; } = new();
        public List<ProductCategorySelectViewModel> AvailableCategories { get; set; } = new();
    }
}
