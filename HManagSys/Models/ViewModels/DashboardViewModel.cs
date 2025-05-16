namespace HManagSys.Models.ViewModels
{

    /// <summary>
    /// Modèle de vue pour le tableau de bord
    /// </summary>
    public class DashboardViewModel
    {
        public HManagSys.Models.EfModels.User User { get; set; } = null!;
        public HManagSys.Models.EfModels.HospitalCenter Center { get; set; } = null!;
        public string CurrentRole { get; set; } = string.Empty;
        public string WelcomeMessage { get; set; } = string.Empty;

        // Propriétés pour les statistiques rapides (à implémenter plus tard)
        // public QuickStatsModel QuickStats { get; set; } = new();
        // public List<ActivityModel> RecentActivities { get; set; } = new();
    }
}
