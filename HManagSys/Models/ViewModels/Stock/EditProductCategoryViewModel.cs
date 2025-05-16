using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour la modification d'une catégorie
    /// </summary>
    public class EditProductCategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de la catégorie est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom de la catégorie")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; }

        // Pour vérification des contraintes
        [Display(Name = "Nombre de produits associés")]
        public int ProductCount { get; set; }
    }
}
