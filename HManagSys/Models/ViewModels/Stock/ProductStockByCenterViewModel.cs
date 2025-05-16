namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Stock d'un produit par centre
    /// </summary>
    public class ProductStockByCenterViewModel
    {
        public int HospitalCenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal? MinimumThreshold { get; set; }
        public decimal? MaximumThreshold { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public DateTime? LastMovementDate { get; set; }

        // Propriétés calculées
        public string StatusBadge => StockStatus switch
        {
            "Critical" => "badge bg-danger",
            "Low" => "badge bg-warning",
            "Normal" => "badge bg-success",
            "High" => "badge bg-info",
            _ => "badge bg-secondary"
        };

        public string StatusText => StockStatus switch
        {
            "Critical" => "Critique",
            "Low" => "Bas",
            "Normal" => "Normal",
            "High" => "Élevé",
            _ => "Non défini"
        };

        public bool NeedsAttention => StockStatus is "Critical" or "Low";
    }
}
