using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Users;

namespace HManagSys.Models.ViewModels
{
    /// <summary>
    /// Modèle de vue pour le tableau de bord administrateur
    /// Vue d'ensemble complète pour la gestion des utilisateurs
    /// </summary>
    public class AdminDashboardViewModel
    {
        public List<UserSummary> Users { get; set; } = new();
        public UserManagementFilters Filters { get; set; } = new();
        public AdminStatistics Statistics { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public List<Models.EfModels.HospitalCenter> AvailableCenters { get; set; } = new();
        public List<SessionInfo> ActiveSessions { get; set; } = new();
    }

 

    /// <summary>
    /// Statistiques administratives
    /// </summary>
    public class AdminStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int UsersLoggedToday { get; set; }
        public int UsersRequiringPasswordChange { get; set; }
        public int TotalActiveSessions { get; set; }
        public int SuperAdmins { get; set; }
        public int MedicalStaff { get; set; }
        public int InactiveUsers => TotalUsers - ActiveUsers;
        public double ActiveUsersPercentage => TotalUsers > 0 ? (double)ActiveUsers / TotalUsers * 100 : 0;
    }

    /// <summary>
    /// Informations de pagination
    /// </summary>
    //public class PaginationInfo
    //{
    //    public int CurrentPage { get; set; }
    //    public int PageSize { get; set; }
    //    public int TotalCount { get; set; }
    //    public int TotalPages { get; set; }
    //    public bool HasPreviousPage => CurrentPage > 1;
    //    public bool HasNextPage => CurrentPage < TotalPages;
    //    public int PreviousPage => CurrentPage - 1;
    //    public int NextPage => CurrentPage + 1;
    //}

    /// <summary>
    /// Résumé d'un utilisateur pour l'affichage administratif
    /// </summary>
    public class UserSummary
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool MustChangePassword { get; set; }
        public List<AssignmentInfo> Assignments { get; set; } = new();
        public int ActiveSessionsCount { get; set; }
        public string StatusBadge => IsActive ? "bg-success" : "bg-secondary";
        public string StatusText => IsActive ? "Actif" : "Inactif";
        public string LastLoginText => LastLoginDate?.ToString("dd/MM/yyyy HH:mm") ?? "Jamais";
        public bool HasMultipleCenters => Assignments.Count > 1;
        public string AssignmentsSummary => string.Join(", ", Assignments.Select(a => $"{a.RoleType} @ {a.HospitalCenterName}"));
    }

    /// <summary>
    /// Informations d'affectation d'un utilisateur
    /// </summary>
    public class AssignmentInfo
    {
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime AssignmentStartDate { get; set; }
        public DateTime? AssignmentEndDate { get; set; }
        public string RoleBadgeClass => RoleType == "SuperAdmin" ? "bg-danger" : "bg-primary";
        public string RoleDisplayName => RoleType == "SuperAdmin" ? "Super Admin" : "Personnel Soignant";
    }

    /// <summary>
    /// Modèle pour le menu de changement de centre
    /// </summary>
    public class CenterSwitchModel
    {
        public int CurrentCenterId { get; set; }
        public string CurrentCenterName { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;
        public List<CenterOption> AvailableCenters { get; set; } = new();
    }

    /// <summary>
    /// Option de centre pour le menu de changement
    /// </summary>
    public class CenterOption
    {
        public int HospitalCenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string RoleInCenter { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public DateTime AssignmentStartDate { get; set; }
        public string RoleBadgeClass => RoleInCenter == "SuperAdmin" ? "bg-danger" : "bg-primary";
        public string RoleDisplayName => RoleInCenter == "SuperAdmin" ? "Super Admin" : "Personnel Soignant";
    }

    /// <summary>
    /// Réponse pour le changement de centre
    /// </summary>
    public class CenterSwitchResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? NewCenterName { get; set; }
        public string? NewRole { get; set; }
        public int? NewCenterId { get; set; }
        public Dictionary<string, object>? UpdatedData { get; set; }
    }

    /// <summary>
    /// Informations de session pour l'affichage admin
    /// </summary>
    public class SessionInfo
    {
        public string SessionToken { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int CurrentHospitalCenterId { get; set; }
        public string CurrentCenterName { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public TimeSpan Duration => DateTime.UtcNow - LoginTime;
        public string DurationText => Duration.TotalHours >= 1
            ? $"{(int)Duration.TotalHours}h {Duration.Minutes}min"
            : $"{Duration.Minutes}min";
        public string LoginTimeText => LoginTime.ToString("dd/MM HH:mm");
        public string ExpiresAtText => ExpiresAt.ToString("HH:mm");
    }
}