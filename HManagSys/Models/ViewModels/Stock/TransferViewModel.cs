using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock;

/// <summary>
/// ViewModel pour afficher un transfert
/// </summary>
public class TransferViewModel
{
    public int Id { get; set; }

    [Display(Name = "Produit")]
    public int ProductId { get; set; }

    [Display(Name = "Nom du produit")]
    public string ProductName { get; set; } = string.Empty;

    [Display(Name = "Catégorie")]
    public string ProductCategory { get; set; } = string.Empty;

    [Display(Name = "Centre source")]
    public int FromHospitalCenterId { get; set; }

    [Display(Name = "Centre source")]
    public string FromHospitalCenterName { get; set; } = string.Empty;

    [Display(Name = "Centre destination")]
    public int ToHospitalCenterId { get; set; }

    [Display(Name = "Centre destination")]
    public string ToHospitalCenterName { get; set; } = string.Empty;

    [Display(Name = "Quantité")]
    [DisplayFormat(DataFormatString = "{0:N2}")]
    public decimal Quantity { get; set; }

    [Display(Name = "Unité")]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Display(Name = "Motif du transfert")]
    public string? TransferReason { get; set; }

    [Display(Name = "Statut")]
    public string Status { get; set; } = string.Empty;

    [Display(Name = "Commentaires d'approbation")]
    public string? ApprovalComments { get; set; }

    [Display(Name = "Date de demande")]
    public DateTime RequestDate { get; set; }

    [Display(Name = "Date d'approbation")]
    public DateTime? ApprovedDate { get; set; }

    [Display(Name = "Date de réalisation")]
    public DateTime? CompletedDate { get; set; }

    [Display(Name = "Demandé par")]
    public string RequestedByName { get; set; } = string.Empty;

    [Display(Name = "Approuvé par")]
    public string? ApprovedByName { get; set; }

    [Display(Name = "Stock disponible (source)")]
    public decimal SourceStockQuantity { get; set; }

    // Propriétés calculées
    public string StatusBadgeClass => GetStatusBadgeClass();
    public string StatusText => GetStatusText();
    public string QuantityText => $"{Quantity:N2} {UnitOfMeasure}";
    public bool CanBeApproved => Status == "Requested" || Status == "Pending";
    public bool CanBeRejected => Status == "Requested" || Status == "Pending" || Status == "Approved";
    public bool CanBeCompleted => Status == "Approved";
    public bool CanBeCancelled => Status == "Requested" || Status == "Pending" || Status == "Approved";
    public bool IsFinished => Status == "Completed" || Status == "Rejected" || Status == "Cancelled";
    public bool HasEnoughStock => SourceStockQuantity >= Quantity;

    private string GetStatusBadgeClass()
    {
        return Status switch
        {
            "Requested" => "bg-info",
            "Pending" => "bg-warning",
            "Approved" => "bg-primary",
            "Completed" => "bg-success",
            "Rejected" => "bg-danger",
            "Cancelled" => "bg-secondary",
            _ => "bg-secondary"
        };
    }

    private string GetStatusText()
    {
        return Status switch
        {
            "Requested" => "Demandé",
            "Pending" => "En attente",
            "Approved" => "Approuvé",
            "Completed" => "Terminé",
            "Rejected" => "Rejeté",
            "Cancelled" => "Annulé",
            _ => Status
        };
    }
}

/// <summary>
/// ViewModel pour créer une demande de transfert
/// </summary>
public class TransferRequestViewModel
{
    [Required(ErrorMessage = "Le produit est requis")]
    [Display(Name = "Produit")]
    public int ProductId { get; set; }

    [Display(Name = "Nom du produit")]
    public string? ProductName { get; set; }

    [Required(ErrorMessage = "Le centre source est requis")]
    [Display(Name = "Centre source")]
    public int FromHospitalCenterId { get; set; }

    [Required(ErrorMessage = "Le centre destination est requis")]
    [Display(Name = "Centre destination")]
    public int ToHospitalCenterId { get; set; }

    [Required(ErrorMessage = "La quantité est requise")]
    [Range(0.01, double.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
    [Display(Name = "Quantité")]
    public decimal Quantity { get; set; }

    [StringLength(500, ErrorMessage = "Le motif ne peut pas dépasser 500 caractères")]
    [Display(Name = "Motif du transfert")]
    public string? TransferReason { get; set; }

    // Propriétés pour la vue
    public List<SelectOption> AvailableProducts { get; set; } = new();
    public List<SelectOption> AvailableCenters { get; set; } = new();
    public decimal AvailableQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel pour l'approbation d'un transfert
/// </summary>
public class TransferApprovalViewModel
{
    public int TransferId { get; set; }

    [Required(ErrorMessage = "Les commentaires sont requis pour l'approbation")]
    [StringLength(500, ErrorMessage = "Les commentaires ne peuvent pas dépasser 500 caractères")]
    [Display(Name = "Commentaires")]
    public string Comments { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel pour le rejet d'un transfert
/// </summary>
public class TransferRejectionViewModel
{
    public int TransferId { get; set; }

    [Required(ErrorMessage = "Le motif de rejet est requis")]
    [StringLength(500, ErrorMessage = "Le motif ne peut pas dépasser 500 caractères")]
    [Display(Name = "Motif de rejet")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel pour les filtres de transferts
/// </summary>
public class TransferFilters
{
    [Display(Name = "Statut")]
    public string? Status { get; set; }

    [Display(Name = "Centre source")]
    public int? FromCenterId { get; set; }

    [Display(Name = "Centre destination")]
    public int? ToCenterId { get; set; }

    [Display(Name = "Produit")]
    public int? ProductId { get; set; }

    [Display(Name = "Période (jours)")]
    public int Days { get; set; } = 30;

    [Display(Name = "Uniquement mes demandes")]
    public bool OnlyMyRequests { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// ViewModel pour les statistiques de transferts
/// </summary>
public class TransferStatisticsViewModel
{
    public int TotalTransfers { get; set; }
    public int PendingTransfers { get; set; }
    public int ApprovedTransfers { get; set; }
    public int CompletedTransfers { get; set; }
    public int RejectedTransfers { get; set; }
    public int CancelledTransfers { get; set; }

    public List<ProductTransferCount> TopTransferredProducts { get; set; } = new();
    public List<CenterTransferCount> TopSourceCenters { get; set; } = new();
    public List<CenterTransferCount> TopDestinationCenters { get; set; } = new();

    public double CompletionRate => TotalTransfers > 0
        ? (double)CompletedTransfers / TotalTransfers * 100
        : 0;

    public double RejectionRate => TotalTransfers > 0
        ? (double)RejectedTransfers / TotalTransfers * 100
        : 0;
}

public class ProductTransferCount
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TransferCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
}

public class CenterTransferCount
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;
    public int TransferCount { get; set; }
}