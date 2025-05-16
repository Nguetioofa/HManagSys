namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Item de stock pour la vue d'ensemble
    /// </summary>
    public class StockItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;

        public decimal CurrentQuantity { get; set; }
        public decimal? MinimumThreshold { get; set; }
        public decimal? MaximumThreshold { get; set; }
        public string StockStatus { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }
        public decimal TotalValue => CurrentQuantity * UnitPrice;

        public DateTime? LastMovementDate { get; set; }
        public int MovementsLast30Days { get; set; }

        // Propriétés calculées pour l'affichage
        public string QuantityText => $"{CurrentQuantity:N2} {UnitOfMeasure}";

        public string ThresholdText => MinimumThreshold.HasValue && MaximumThreshold.HasValue
            ? $"{MinimumThreshold:N0} / {MaximumThreshold:N0}"
            : MinimumThreshold.HasValue
                ? $"Min: {MinimumThreshold:N0}"
                : "Non défini";

        public string StatusText => StockStatus switch
        {
            "Critical" => "Critique",
            "Low" => "Bas",
            "Normal" => "Normal",
            "High" => "Élevé",
            "OutOfStock" => "Rupture",
            _ => "Non défini"
        };

        public string StatusBadge => StockStatus switch
        {
            "Critical" => "badge bg-danger",
            "Low" => "badge bg-warning text-dark",
            "Normal" => "badge bg-success",
            "High" => "badge bg-info text-dark",
            "OutOfStock" => "badge bg-dark",
            _ => "badge bg-secondary"
        };

        public string StatusIcon => StockStatus switch
        {
            "Critical" => "fas fa-exclamation-triangle text-danger",
            "Low" => "fas fa-exclamation-circle text-warning",
            "Normal" => "fas fa-check-circle text-success",
            "High" => "fas fa-arrow-up text-info",
            "OutOfStock" => "fas fa-times-circle text-dark",
            _ => "fas fa-question-circle text-secondary"
        };

        public string TotalValueText => $"{TotalValue:N0} FCFA";
        public string LastMovementText => LastMovementDate?.ToString("dd/MM HH:mm") ?? "Aucun";

        public bool NeedsAttention => StockStatus is "Critical" or "Low" or "OutOfStock";
        public bool CanRequestTransfer => StockStatus is "Critical" or "Low" or "OutOfStock";
    }
}
