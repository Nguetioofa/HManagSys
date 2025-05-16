namespace HManagSys.Models.ViewModels.Users
{


    /// <summary>
    /// Résultat de validation de session
    /// Avec détails sur l'état et les actions à prendre
    /// </summary>
    public class SessionValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public bool IsExtended { get; set; }
        public SessionInfo? SessionInfo { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? NewExpiryTime { get; set; }
    }
}
