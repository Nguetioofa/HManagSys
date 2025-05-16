using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour créer une nouvelle catégorie de produit
    /// </summary>
    public class CreateCategoryViewModel
    {
        [Required(ErrorMessage = "Le nom de la catégorie est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom de la catégorie")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Catégorie active")]
        public bool IsActive { get; set; } = true;
    }
}
