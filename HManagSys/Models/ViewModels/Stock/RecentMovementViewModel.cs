namespace HManagSys.Models.ViewModels.Stock
{

    /// <summary>
    /// Mouvement récent d'un produit
    /// </summary>
    public class RecentMovementViewModel
    {
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Notes { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        // Propriétés calculées
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
            "Initial" => "fas fa-play",
            "Entry" => "fas fa-arrow-up text-success",
            "Sale" => "fas fa-cash-register text-primary",
            "Transfer" => "fas fa-exchange-alt text-info",
            "Adjustment" => "fas fa-cog text-warning",
            "Care" => "fas fa-stethoscope text-purple",
            _ => "fas fa-circle"
        };

        public string QuantityText => $"{(Quantity >= 0 ? "+" : "")}{Quantity:N2}";
        public string QuantityClass => Quantity >= 0 ? "text-success" : "text-danger";
    }
}
