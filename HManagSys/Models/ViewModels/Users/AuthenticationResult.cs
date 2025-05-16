using HManagSys.Models.EfModels;

namespace HManagSys.Models.ViewModels.Users
{

    /// <summary>
    /// Résultat détaillé d'une tentative d'authentification
    /// Plus riche qu'un simple booléen success/failure
    /// </summary>
    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public User? User { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public bool AccountLocked { get; set; }
        public int? RemainingAttempts { get; set; }
        public DateTime? LockoutUntil { get; set; }
    }
}
