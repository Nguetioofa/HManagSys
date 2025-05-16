namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Mouvement de stock récent pour la vue d'ensemble
    /// </summary>
    public class RecentStockMovementViewModel
    {
        public DateTime MovementDate { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }

        public string MovementTypeText => MovementType switch
        {
            "Initial" => "Stock initial",
            "Entry" => "Entrée",
            "Sale" => "Vente",
            "Transfer" => "Transfert",
            "Adjustment" => "Ajustement",
            "Care" => "Utilisation soin",
            _ => MovementType
        };

        public string MovementIcon => MovementType switch
        {
            "Initial" => "fas fa-play text-primary",
            "Entry" => "fas fa-arrow-up text-success",
            "Sale" => "fas fa-cash-register text-primary",
            "Transfer" => "fas fa-exchange-alt text-info",
            "Adjustment" => "fas fa-cog text-warning",
            "Care" => "fas fa-stethoscope text-purple",
            _ => "fas fa-circle text-secondary"
        };

        public string QuantityText => $"{(Quantity >= 0 ? "+" : "")}{Quantity:N2} {UnitOfMeasure}";
        public string QuantityClass => Quantity >= 0 ? "text-success" : "text-danger";
        public string TimeText => MovementDate.ToString("HH:mm");
        public string DateText => MovementDate.ToString("dd/MM");

        public string ReferenceText => (ReferenceType, ReferenceId) switch
        {
            ("Sale", var id) when id.HasValue => $"Vente #{id}",
            ("Care", var id) when id.HasValue => $"Soin #{id}",
            ("Transfer", var id) when id.HasValue => $"Transfert #{id}",
            _ => ""
        };
    }
}
