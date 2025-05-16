namespace HManagSys.Models.ViewModels.HospitalCenter
{
    /// <summary>
    /// Statistiques du réseau hospitalier
    /// </summary>
    public class NetworkStatistics
    {
        public int TotalCenters { get; set; }
        public int ActiveCenters { get; set; }
        public int TotalUsersNetwork { get; set; }
        public decimal TotalSalesToday { get; set; }
        public int ActiveCareEpisodesNetwork { get; set; }
    }
}
