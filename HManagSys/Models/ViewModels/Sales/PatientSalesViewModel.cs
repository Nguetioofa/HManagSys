namespace HManagSys.Models.ViewModels.Sales
{
    /// <summary>
    /// Modèle pour la vue des ventes d'un patient
    /// </summary>
    public class PatientSalesViewModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public List<SaleViewModel> Sales { get; set; } = new();
        public decimal TotalAmount => Sales.Sum(s => s.FinalAmount);
        public decimal TotalPaid => Sales.Sum(s => s.PaidAmount);
        public decimal TotalRemaining => Sales.Sum(s => s.RemainingAmount);
        public int CompletedSales => Sales.Count(s => s.PaymentStatus == "Paid");
        public int PendingSales => Sales.Count(s => s.PaymentStatus == "Pending" || s.PaymentStatus == "Partial");
    }

    /// <summary>
    /// Modèle pour le tableau de bord des ventes
    /// </summary>
    public class SaleDashboardViewModel
    {
        public SaleSummaryViewModel MonthSummary { get; set; } = new();
        public SaleSummaryViewModel WeekSummary { get; set; } = new();
        public SaleSummaryViewModel TodaySummary { get; set; } = new();
    }
}
