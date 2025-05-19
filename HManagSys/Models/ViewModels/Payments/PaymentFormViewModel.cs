using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Payments
{
    /// <summary>
    /// ViewModel pour le formulaire de paiement (utilisé dans la vue partielle _PaymentForm)
    /// </summary>
    public class PaymentFormViewModel
    {
        public string ReferenceType { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public int? PatientId { get; set; }
        public int HospitalCenterId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingAmount => TotalAmount - AmountPaid;
        public decimal SuggestedAmount => RemainingAmount;
        public decimal MaxAmount => RemainingAmount;
        public string ReferenceDescription { get; set; } = string.Empty;

        public List<PaymentMethodViewModel> PaymentMethods { get; set; } = new();
    }

}