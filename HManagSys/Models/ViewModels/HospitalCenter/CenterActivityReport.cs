namespace HManagSys.Models.ViewModels.HospitalCenter
{

    /// <summary>
    /// Rapport d'activité d'un centre
    /// </summary>
    public class CenterActivityReport
    {
        public int HospitalCenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CareEpisodesCreated { get; set; }
        public int ExaminationsPerformed { get; set; }
        public int UniquePatients { get; set; }
        public DateTime ReportGeneratedAt { get; set; }
    }


}
