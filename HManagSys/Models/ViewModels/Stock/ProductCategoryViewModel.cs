using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour l'affichage d'une catégorie de produits
    /// </summary>
    public class ProductCategoryViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nom de la catégorie")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Statut")]
        public bool IsActive { get; set; }

        [Display(Name = "Nombre de produits")]
        public int ProductCount { get; set; }

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
    }
}
