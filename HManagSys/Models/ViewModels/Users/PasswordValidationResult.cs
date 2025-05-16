namespace HManagSys.Models.ViewModels.Users
{
    /// <summary>
    /// Résultat de validation de mot de passe
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public int StrengthScore { get; set; } // 0-100
        public string StrengthLevel { get; set; } = "Weak"; // Weak, Fair, Good, Strong
    }



    /// <summary>
    /// Résultat d'un changement de mot de passe
    /// Avec validation et feedback détaillé
    /// </summary>
    public class PasswordChangeResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public bool RequiresAdditionalVerification { get; set; }
    }

    /// <summary>
    /// Résultat d'une réinitialisation de mot de passe
    /// Avec mot de passe temporaire généré
    /// </summary>
    public class PasswordResetResult
    {
        public bool IsSuccess { get; set; }
        public string? TemporaryPassword { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ExpiresAt { get; set; }
    }


    /// <summary>
    /// Vérification du statut d'un utilisateur
    /// Contrôle global de l'état du compte
    /// </summary>
    public class UserStatusCheck
    {
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int DaysSinceLastLogin { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statistiques d'authentification
    /// Pour les tableaux de bord de sécurité
    /// </summary>
    public class AuthenticationStatistics
    {
        public int TotalLoginAttempts { get; set; }
        public int SuccessfulLogins { get; set; }
        public int FailedLogins { get; set; }
        public int UniqueUsersLoggedIn { get; set; }
        public int PasswordResetsRequested { get; set; }
        public int AccountsLocked { get; set; }
        public int UnauthorizedAccessAttempts { get; set; }
        public double SuccessRate => TotalLoginAttempts > 0 ?
            (double)SuccessfulLogins / TotalLoginAttempts * 100 : 0;
    }
}
