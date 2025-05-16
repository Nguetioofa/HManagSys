namespace HManagSys.Models.ViewModels.Users
{

    /// <summary>
    /// Historique des connexions d'un utilisateur
    /// Pour l'audit de sécurité et le suivi d'activité
    /// </summary>
    public class UserLoginHistory
    {
        public DateTime LoginTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public bool IsCurrentSession { get; set; }
    }
}
