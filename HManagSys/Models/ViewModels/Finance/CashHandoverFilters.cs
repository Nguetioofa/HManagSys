using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Finance
{
    /// <summary>
    /// Filtres pour la recherche des remises d'espèces
    /// </summary>
    public class CashHandoverFilters
    {
        public int? FinancierId { get; set; }
        public int? HospitalCenterId { get; set; }
        public int? HandedOverBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Modèle pour l'affichage d'une remise d'espèces
    /// </summary>
    public class CashHandoverViewModel
    {
        public int Id { get; set; }
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public int FinancierId { get; set; }
        public string FinancierName { get; set; } = string.Empty;
        public DateTime HandoverDate { get; set; }
        public decimal TotalCashAmount { get; set; }
        public decimal HandoverAmount { get; set; }
        public decimal RemainingCashAmount { get; set; }
        public int HandedOverBy { get; set; }
        public string HandedOverByName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        // Propriétés pour l'affichage
        public string FormattedHandoverDate => HandoverDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedTotalCashAmount => $"{TotalCashAmount:N0} FCFA";
        public string FormattedHandoverAmount => $"{HandoverAmount:N0} FCFA";
        public string FormattedRemainingCashAmount => $"{RemainingCashAmount:N0} FCFA";

        // Propriété calculée
        public decimal HandoverPercentage => TotalCashAmount > 0
            ? Math.Round((HandoverAmount / TotalCashAmount) * 100, 2)
            : 0;
    }

    /// <summary>
    /// Modèle pour la création d'une remise d'espèces
    /// </summary>
    public class CreateCashHandoverViewModel
    {
        [Required(ErrorMessage = "Le centre hospitalier est obligatoire")]
        [Display(Name = "Centre hospitalier")]
        public int HospitalCenterId { get; set; }

        [Required(ErrorMessage = "Le financier est obligatoire")]
        [Display(Name = "Financier")]
        public int FinancierId { get; set; }

        [Required(ErrorMessage = "La date de remise est obligatoire")]
        [Display(Name = "Date de remise")]
        public DateTime HandoverDate { get; set; } = DateTime.Now;

        [Display(Name = "Montant total en caisse")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être positif ou nul")]
        public decimal TotalCashAmount { get; set; }

        [Required(ErrorMessage = "Le montant de la remise est obligatoire")]
        [Display(Name = "Montant remis")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant remis doit être supérieur à 0")]
        public decimal HandoverAmount { get; set; }

        [Display(Name = "Montant restant en caisse")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant restant doit être positif ou nul")]
        public decimal RemainingCashAmount { get; set; }

        [Required(ErrorMessage = "Le responsable de la remise est obligatoire")]
        [Display(Name = "Remis par")]
        public int HandedOverBy { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Propriétés pour la gestion de l'interface
        [Display(Name = "Financier")]
        public string FinancierName { get; set; } = string.Empty;

        [Display(Name = "Centre")]
        public string HospitalCenterName { get; set; } = string.Empty;

        public List<SelectOption>? FinancierOptions { get; set; }
    }

    /// <summary>
    /// Modèle pour la réconciliation de caisse
    /// </summary>
    public class CashReconciliationViewModel
    {
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public DateTime LastHandoverDate { get; set; }
        public decimal LastHandoverRemainingAmount { get; set; }
        public decimal TotalCashReceiptsSince { get; set; }
        public decimal ExpectedCashBalance => LastHandoverRemainingAmount + TotalCashReceiptsSince;
        public int PaymentCount { get; set; }

        // Détails des paiements
        public List<CashPaymentSummary> PaymentDetails { get; set; } = new();

        // Propriétés formattées
        public string FormattedLastHandoverDate => LastHandoverDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedLastHandoverRemainingAmount => $"{LastHandoverRemainingAmount:N0} FCFA";
        public string FormattedTotalCashReceiptsSince => $"{TotalCashReceiptsSince:N0} FCFA";
        public string FormattedExpectedCashBalance => $"{ExpectedCashBalance:N0} FCFA";
    }

    /// <summary>
    /// Récapitulatif des paiements en espèces
    /// </summary>
    public class CashPaymentSummary
    {
        public string ReferenceType { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }

        // Propriété formatée
        public string FormattedTotalAmount => $"{TotalAmount:N0} FCFA";
    }

    /// <summary>
    /// Modèle pour les mouvements de caisse
    /// </summary>
    public class CashMovementViewModel
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Direction { get; set; } = string.Empty; // IN ou OUT
        public decimal Balance { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }

        // Propriétés pour l'affichage
        public string FormattedDate => Date.ToString("dd/MM/yyyy HH:mm");
        public string FormattedAmount => $"{Amount:N0} FCFA";
        public string FormattedBalance => $"{Balance:N0} FCFA";
        public string AmountClass => Direction == "IN" ? "text-success" : "text-danger";
        public string AmountPrefix => Direction == "IN" ? "+" : "-";
    }

    /// <summary>
    /// Modèle pour l'état de la caisse
    /// </summary>
    public class CashPositionViewModel
    {
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public DateTime LastHandoverDate { get; set; }
        public decimal LastHandoverAmount { get; set; }
        public decimal ReceiptsSinceLastHandover { get; set; }
        public int DaysSinceLastHandover { get; set; }
        public decimal AverageDailyReceipts { get; set; }

        // Propriétés pour l'affichage
        public string FormattedCurrentBalance => $"{CurrentBalance:N0} FCFA";
        public string FormattedLastHandoverDate => LastHandoverDate.ToString("dd/MM/yyyy");
        public string FormattedLastHandoverAmount => $"{LastHandoverAmount:N0} FCFA";
        public string FormattedReceiptsSinceLastHandover => $"{ReceiptsSinceLastHandover:N0} FCFA";
        public string FormattedAverageDailyReceipts => $"{AverageDailyReceipts:N0} FCFA";
    }
}