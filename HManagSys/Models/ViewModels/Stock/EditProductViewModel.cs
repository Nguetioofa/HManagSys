using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour la modification d'un produit
    /// </summary>
    public class EditProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom du produit est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom du produit")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Veuillez sélectionner une catégorie")]
        [Display(Name = "Catégorie")]
        public int ProductCategoryId { get; set; }

        [Required(ErrorMessage = "L'unité de mesure est requise")]
        [StringLength(50, ErrorMessage = "L'unité ne peut pas dépasser 50 caractères")]
        [Display(Name = "Unité de mesure")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prix de vente est requis")]
        [Range(0, double.MaxValue, ErrorMessage = "Le prix doit être positif")]
        [Display(Name = "Prix de vente (FCFA)")]
        public decimal SellingPrice { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; }

        // Liste des catégories disponibles
        public List<ProductCategorySelectViewModel> AvailableCategories { get; set; } = new();

        // Pour vérification des contraintes
        [Display(Name = "Centres avec stock")]
        public int CentersWithStock { get; set; }

        [Display(Name = "Mouvements enregistrés")]
        public int MovementCount { get; set; }
    }
}
