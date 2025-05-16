namespace HManagSys.Models.ViewModels.Stock
{

    /// <summary>
    /// Alerte de stock critique
    /// </summary>
    public class StockAlertViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal? MinimumThreshold { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime? LastMovementDate { get; set; }

        public string SeverityText => Severity switch
        {
            "Critical" => "Critique",
            "Low" => "Attention",
            "OutOfStock" => "Rupture",
            _ => Severity
        };

        public string SeverityBadge => Severity switch
        {
            "Critical" => "badge bg-danger",
            "Low" => "badge bg-warning text-dark",
            "OutOfStock" => "badge bg-dark",
            _ => "badge bg-secondary"
        };

        public string QuantityText => $"{CurrentQuantity:N2} {UnitOfMeasure}";
        public string AlertMessage => Severity switch
        {
            "Critical" => $"Stock critique : {QuantityText} (seuil : {MinimumThreshold:N0})",
            "Low" => $"Stock bas : {QuantityText} (seuil : {MinimumThreshold:N0})",
            "OutOfStock" => "Rupture de stock",
            _ => $"Stock : {QuantityText}"
        };
    }
}
