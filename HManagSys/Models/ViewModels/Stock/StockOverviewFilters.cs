using HManagSys.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Filtres pour la vue d'ensemble des stocks
    /// </summary>
    public class StockOverviewFilters
    {
        [Display(Name = "Recherche produit")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Catégorie")]
        public int? CategoryId { get; set; }

        [Display(Name = "Statut du stock")]
        public string? StockStatus { get; set; }

        [Display(Name = "Afficher seulement")]
        public StockFilterType FilterType { get; set; } = StockFilterType.All;

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Options prédéfinies pour les statuts
        public List<SelectOption> StatusOptions { get; set; } = new()
        {
            new("", "Tous les statuts"),
            new("Critical", "Stock critique"),
            new("Low", "Stock bas"),
            new("Normal", "Stock normal"),
            new("High", "Stock élevé"),
            new("OutOfStock", "Rupture")
        };
    }
}
