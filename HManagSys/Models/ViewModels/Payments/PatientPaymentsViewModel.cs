namespace HManagSys.Models.ViewModels.Payments
{
    /// <summary>
    /// ViewModel pour la page des paiements d'un patient
    /// </summary>
    public class PatientPaymentsViewModel
    {
        public Models.ViewModels.Patients.PatientViewModel Patient { get; set; } = null!;
        public List<PaymentViewModel> Payments { get; set; } = new List<PaymentViewModel>();
        public PaymentSummaryViewModel Summary { get; set; } = null!;
    }
}
