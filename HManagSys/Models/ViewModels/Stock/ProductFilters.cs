using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{

    /// <summary>
    /// Filtres pour la liste des produits
    /// </summary>
    public class ProductFilters
    {
        [Display(Name = "Recherche")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Catégorie")]
        public int? CategoryId { get; set; }

        [Display(Name = "Statut")]
        public bool? IsActive { get; set; }

        [Display(Name = "Avec stock faible")]
        public bool ShowLowStockOnly { get; set; }

        [Display(Name = "Prix minimum")]
        public decimal? MinPrice { get; set; }

        [Display(Name = "Prix maximum")]
        public decimal? MaxPrice { get; set; }

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
