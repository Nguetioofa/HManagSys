using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Payments
{
    /// <summary>
    /// Filtres pour la recherche de paiements
    /// </summary>
    public class PaymentFilters
    {
        public string? SearchTerm { get; set; }
        public int? PatientId { get; set; }
        public int? HospitalCenterId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? PaymentMethodId { get; set; }
        public int? ReceivedBy { get; set; }
        public string? ReferenceType { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// ViewModel pour l'affichage d'un paiement
    /// </summary>
    public class PaymentViewModel
    {
        public int Id { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public string ReferenceDescription { get; set; } = string.Empty;
        public int? PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public int PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public int ReceivedById { get; set; }
        public string ReceivedByName { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
        public bool IsCancelled { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        // Propriétés calculées pour l'affichage
        public string FormattedAmount => $"{Amount:N0} FCFA";
        public string FormattedDate => PaymentDate.ToString("dd/MM/yyyy HH:mm");
        public string ReferenceText => $"{ReferenceType} #{ReferenceId}";
        public string StatusBadge => IsCancelled
            ? "badge bg-danger"
            : "badge bg-success";
        public string StatusText => IsCancelled
            ? "Annulé"
            : "Validé";
    }

    /// <summary>
    /// ViewModel pour la création d'un paiement
    /// </summary>
    public class CreatePaymentViewModel
    {
        [Required(ErrorMessage = "Le type de référence est obligatoire")]
        [Display(Name = "Type de référence")]
        public string ReferenceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'ID de référence est obligatoire")]
        [Display(Name = "Référence")]
        public int ReferenceId { get; set; }

        [Display(Name = "Description")]
        public string ReferenceDescription { get; set; } = string.Empty;

        [Display(Name = "Patient")]
        public int? PatientId { get; set; }

        [Display(Name = "Nom du patient")]
        public string PatientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le centre hospitalier est obligatoire")]
        [Display(Name = "Centre hospitalier")]
        public int HospitalCenterId { get; set; }

        [Required(ErrorMessage = "La méthode de paiement est obligatoire")]
        [Display(Name = "Méthode de paiement")]
        public int PaymentMethodId { get; set; }

        [Required(ErrorMessage = "Le montant est obligatoire")]
        [Range(1, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        [Display(Name = "Montant")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "La date de paiement est obligatoire")]
        [Display(Name = "Date de paiement")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Le récepteur du paiement est obligatoire")]
        [Display(Name = "Reçu par")]
        public int ReceivedById { get; set; }

        [Display(Name = "Référence de transaction")]
        public string? TransactionReference { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Méthodes de paiement disponibles
        public List<PaymentMethodViewModel> PaymentMethods { get; set; } = new List<PaymentMethodViewModel>();

        // Référence restante à payer (le cas échéant)
        public decimal? RemainingAmount { get; set; }

        // Montant total de la référence (le cas échéant)
        public decimal? TotalAmount { get; set; }
    }

    /// <summary>
    /// ViewModel pour les méthodes de paiement
    /// </summary>
    public class PaymentMethodViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool RequiresBankAccount { get; set; }
        public bool IsActive { get; set; }

        // Propriété pour les listes déroulantes
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Résumé des paiements d'un patient
    /// </summary>
    public class PaymentSummaryViewModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
        public decimal Balance => TotalDue - TotalPaid;
        public int PaymentCount { get; set; }
        public DateTime? LastPaymentDate { get; set; }

        public Dictionary<string, decimal> PaymentsByType { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> PaymentsByMethod { get; set; } = new Dictionary<string, decimal>();

        // Propriétés calculées
        public string FormattedTotalPaid => $"{TotalPaid:N0} FCFA";
        public string FormattedTotalDue => $"{TotalDue:N0} FCFA";
        public string FormattedBalance => $"{Balance:N0} FCFA";
        public bool HasDebt => Balance > 0;
        public string BalanceClass => Balance > 0 ? "text-danger" : "text-success";
    }

    /// <summary>
    /// ViewModel pour le reçu de paiement
    /// </summary>
    public class ReceiptViewModel
    {
        public PaymentViewModel Payment { get; set; } = new PaymentViewModel();
        public string HospitalName { get; set; } = string.Empty;
        public string HospitalAddress { get; set; } = string.Empty;
        public string HospitalContact { get; set; } = string.Empty;
        public string ReceiptNumber => $"REÇU-{Payment.Id:D6}";
        public DateTime ReceiptDate => Payment.PaymentDate;
    }
}