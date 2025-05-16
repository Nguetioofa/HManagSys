using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour modifier une catégorie existante
    /// </summary>
    public class EditCategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de la catégorie est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom de la catégorie")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Catégorie active")]
        public bool IsActive { get; set; }

        // Pour affichage
        public DateTime CreatedAt { get; set; }
        public int ProductCount { get; set; }
    }
}
