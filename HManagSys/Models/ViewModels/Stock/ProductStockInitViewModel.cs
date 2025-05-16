using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Initialisation du stock pour un produit dans un centre
    /// </summary>
    public class ProductStockInitViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "La quantité doit être positive")]
        [Display(Name = "Quantité initiale")]
        public decimal InitialQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le seuil minimum doit être positif")]
        [Display(Name = "Seuil minimum")]
        public decimal? MinimumThreshold { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le seuil maximum doit être positif")]
        [Display(Name = "Seuil maximum")]
        public decimal? MaximumThreshold { get; set; }

        [Display(Name = "Activer le stock")]
        public bool IsEnabled { get; set; } = true;

        public bool HasExistingStock { get; set; }
        public decimal CurrentQuantity { get; set; }
    }
}
