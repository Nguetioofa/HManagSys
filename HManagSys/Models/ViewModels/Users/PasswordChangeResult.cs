namespace HManagSys.Models.ViewModels.Users
{
    /// <summary>
    /// Statistiques globales sur les utilisateurs
    /// Pour les tableaux de bord et rapports
    /// </summary>
    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int SuperAdmins { get; set; }
        public int MedicalStaff { get; set; }
        public int UsersLoggedToday { get; set; }
        public int UsersLoggedThisWeek { get; set; }
        public int UsersRequiringPasswordChange { get; set; }
    }
}
