namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour la liste des catégories avec filtres
    /// </summary>
    public class ProductCategoryListViewModel
    {
        public List<ProductCategoryViewModel> Categories { get; set; } = new();
        public ProductCategoryFilters Filters { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public CategoryStatistics Statistics { get; set; } = new();
    }

}
