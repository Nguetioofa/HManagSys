using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Filtres pour la liste des catégories
    /// </summary>
    public class ProductCategoryFilters
    {
        [Display(Name = "Recherche")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Statut")]
        public bool? IsActive { get; set; }

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
