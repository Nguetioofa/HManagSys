using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour l'affichage d'un produit
    /// </summary>
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nom du produit")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Catégorie")]
        public string CategoryName { get; set; } = string.Empty;

        public int ProductCategoryId { get; set; }

        [Display(Name = "Unité de mesure")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Display(Name = "Prix de vente")]
        [DisplayFormat(DataFormatString = "{0:N0} FCFA")]
        public decimal SellingPrice { get; set; }

        [Display(Name = "Statut")]
        public bool IsActive { get; set; }

        [Display(Name = "Créé par")]
        public string CreatedByName { get; set; } = string.Empty;

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Modifié par")]
        public string? ModifiedByName { get; set; }

        [Display(Name = "Date de modification")]
        public DateTime? ModifiedAt { get; set; }

        // Propriétés calculées pour l'affichage
        public string StatusText => IsActive ? "Actif" : "Inactif";
        public string StatusBadge => IsActive ? "badge bg-success" : "badge bg-secondary";
        public string PriceText => $"{SellingPrice:N0} FCFA";

        // Informations de stock (pour la vue d'ensemble)
        public int TotalCentersWithStock { get; set; }
        public decimal TotalWithStock { get; set; }
        public bool HasLowStock { get; set; }
        public bool HasCriticalStock { get; set; }
    }
}
