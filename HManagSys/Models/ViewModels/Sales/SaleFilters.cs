using HManagSys.Models.ViewModels.Payments;

namespace HManagSys.Models.ViewModels.Sales
{
    /// <summary>
    /// Filtres pour la recherche des ventes
    /// </summary>
    public class SaleFilters
    {
        public string? SearchTerm { get; set; }
        public int? HospitalCenterId { get; set; }
        public int? PatientId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? PaymentStatus { get; set; }
        public int? SoldBy { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Modèle pour l'affichage d'une vente
    /// </summary>
    public class SaleViewModel
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public int SoldBy { get; set; }
        public string SoldByName { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<SaleItemViewModel> Items { get; set; } = new();
        public List<PaymentViewModel> Payments { get; set; } = new();
        public decimal PaidAmount => Payments.Where(p => !p.IsCancelled).Sum(p => p.Amount);
        public decimal RemainingAmount => FinalAmount - PaidAmount;
        public bool IsCancelled { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }

        // Propriétés calculées pour l'affichage
        public string StatusBadgeClass => PaymentStatus switch
        {
            "Paid" => "bg-success",
            "Partial" => "bg-warning",
            "Pending" => "bg-secondary",
            "Cancelled" => "bg-danger",
            _ => "bg-secondary"
        };

        public string StatusText => PaymentStatus switch
        {
            "Paid" => "Payé",
            "Partial" => "Partiel",
            "Pending" => "En attente",
            "Cancelled" => "Annulé",
            _ => PaymentStatus
        };

        public string FormattedSaleDate => SaleDate.ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>
    /// Modèle pour l'affichage d'un article de vente
    /// </summary>
    public class SaleItemViewModel
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedUnitPrice => $"{UnitPrice:N0} FCFA";
        public string FormattedTotalPrice => $"{TotalPrice:N0} FCFA";
        public string FormattedQuantity => $"{Quantity:N2} {UnitOfMeasure}";
    }

    /// <summary>
    /// Modèle pour la création d'une vente
    /// </summary>
    public class CreateSaleViewModel
    {
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? DiscountReason { get; set; }
        public List<CartItemViewModel> Items { get; set; } = new();

        // Pour l'interface de création
        public List<SelectOption>? PaymentMethods { get; set; }
    }

    /// <summary>
    /// Modèle pour la mise à jour d'une vente
    /// </summary>
    public class UpdateSaleViewModel
    {
        public int Id { get; set; }
        public string? Notes { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? DiscountReason { get; set; }
    }

    /// <summary>
    /// Modèle pour le panier d'achat
    /// </summary>
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public decimal SubTotal => Items.Sum(i => i.TotalPrice);
        public decimal DiscountAmount { get; set; }
        public string? DiscountReason { get; set; }
        public decimal FinalAmount => Math.Max(0, SubTotal - DiscountAmount);
        public string? Notes { get; set; }
        public int ItemCount => Items.Count;

        // Propriétés calculées pour l'affichage
        public string FormattedSubTotal => $"{SubTotal:N0} FCFA";
        public string FormattedDiscountAmount => $"{DiscountAmount:N0} FCFA";
        public string FormattedFinalAmount => $"{FinalAmount:N0} FCFA";
    }

    /// <summary>
    /// Modèle pour un article du panier
    /// </summary>
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public decimal AvailableStock { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedUnitPrice => $"{UnitPrice:N0} FCFA";
        public string FormattedTotalPrice => $"{TotalPrice:N0} FCFA";
        public string FormattedQuantity => $"{Quantity:N2} {UnitOfMeasure}";
    }

    /// <summary>
    /// Modèle pour la disponibilité d'un produit
    /// </summary>
    public class ProductAvailabilityViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal? MinimumThreshold { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public bool IsAvailable => CurrentStock > 0;
        public bool IsLowStock => MinimumThreshold.HasValue && CurrentStock <= MinimumThreshold.Value;
        public bool IsCriticalStock => MinimumThreshold.HasValue && CurrentStock <= MinimumThreshold.Value * 0.5m;

        public string StockStatus => CurrentStock <= 0 ? "Rupture" :
                                     IsCriticalStock ? "Critique" :
                                     IsLowStock ? "Faible" :
                                     "Disponible";

        public string StockStatusClass => CurrentStock <= 0 ? "bg-danger" :
                                         IsCriticalStock ? "bg-warning" :
                                         IsLowStock ? "bg-info" :
                                         "bg-success";
    }

    /// <summary>
    /// Modèle pour le résumé des ventes
    /// </summary>
    public class SaleSummaryViewModel
    {
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal NetAmount { get; set; }
        public int TotalItemsSold { get; set; }
        public int TotalPatients { get; set; }
        public Dictionary<string, decimal> SalesByPaymentMethod { get; set; } = new();
        public Dictionary<string, decimal> SalesByStatus { get; set; } = new();
        public Dictionary<string, int> TopSellingProducts { get; set; } = new();

        // Propriétés calculées
        public string FormattedTotalAmount => $"{TotalAmount:N0} FCFA";
        public string FormattedTotalDiscounts => $"{TotalDiscounts:N0} FCFA";
        public string FormattedNetAmount => $"{NetAmount:N0} FCFA";

        public decimal AvgSaleAmount => TotalSales > 0 ? NetAmount / TotalSales : 0;
        public string FormattedAvgSaleAmount => $"{AvgSaleAmount:N0} FCFA";
    }
}