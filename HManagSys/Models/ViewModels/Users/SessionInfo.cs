namespace HManagSys.Models.ViewModels.Users
{

    /// <summary>
    /// Informations détaillées d'une session
    /// Pour la gestion et le monitoring
    /// </summary>
    public class SessionInfo
    {
        public string SessionToken { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int CurrentHospitalCenterId { get; set; }
        public string CurrentCenterName { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}
