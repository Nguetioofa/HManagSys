namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour la liste des catégories avec pagination
    /// </summary>
    public class CategoryListViewModel
    {
        public List<CategorySummary> Categories { get; set; } = new();
        //public CategoryFilters Filters { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public CategoryStatistics Statistics { get; set; } = new();
    }
}
