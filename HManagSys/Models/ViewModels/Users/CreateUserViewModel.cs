using HManagSys.Models.EfModels;
using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Users
{
    /// <summary>
    /// Modèle pour créer un nouvel utilisateur
    /// </summary>
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(256, ErrorMessage = "L'email ne peut pas dépasser 256 caractères")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est obligatoire")]
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string PhoneNumber { get; set; } = string.Empty;

        public List<UserCenterAssignmentDto> CenterAssignments { get; set; } = new();
        public List<Models.EfModels.HospitalCenter> AvailableCenters { get; set; } = new();

        public string FullName => $"{FirstName} {LastName}";
    }

    /// <summary>
    /// Modèle pour modifier un utilisateur existant
    /// </summary>
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(256, ErrorMessage = "L'email ne peut pas dépasser 256 caractères")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est obligatoire")]
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<UserCenterAssignmentDto> CenterAssignments { get; set; } = new();
        public List<Models.EfModels.HospitalCenter> AvailableCenters { get; set; } = new();

        public string FullName => $"{FirstName} {LastName}";
    }

    /// <summary>
    /// DTO pour les affectations de centre d'un utilisateur
    /// </summary>
    public class UserCenterAssignmentDto
    {
        public int HospitalCenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public string RoleType { get; set; } = "MedicalStaff";
        public bool IsSelected { get; set; } = false;
        public DateTime? AssignmentStartDate { get; set; }
        public DateTime? AssignmentEndDate { get; set; }

        public string RoleDisplayName => RoleType == "SuperAdmin" ? "Super Administrateur" : "Personnel Soignant";
        public string RoleBadgeClass => RoleType == "SuperAdmin" ? "bg-danger" : "bg-primary";
    }

    /// <summary>
    /// Modèle pour afficher les détails complets d'un utilisateur
    /// </summary>
    public class UserDetailsViewModel
    {
        public User User { get; set; } = null!;
        public List<UserLoginHistory> LoginHistory { get; set; } = new();
        public List<SessionInfo> ActiveSessions { get; set; } = new();
        public bool CanEdit { get; set; }
        public bool CanResetPassword { get; set; }

        // Propriétés calculées pour l'affichage
        public string FullName => $"{User.FirstName} {User.LastName}";
        public string StatusText => User.IsActive ? "Actif" : "Inactif";
        public string StatusBadgeClass => User.IsActive ? "bg-success" : "bg-secondary";
        public bool HasActiveSessions => ActiveSessions.Any();
        public int TotalSessions => ActiveSessions.Count;
        public DateTime? LastConnection => LoginHistory.FirstOrDefault()?.LoginTime;
        public int DaysSinceLastConnection => LastConnection.HasValue ?
            (DateTime.Now - LastConnection.Value).Days : -1;

        // Récupérer les centres avec rôles
        public List<(string CenterName, string Role, bool IsActive)> CenterAssignments =>
            User.UserCenterAssignments
                .Where(a => a.IsActive)
                .Select(a => (a.HospitalCenter.Name, a.RoleType, a.IsActive))
                .ToList();
    }

    /// <summary>
    /// Modèle pour l'affichage des filtres avec correction
    /// </summary>
    public class UserManagementFilters
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public string? RoleFilter { get; set; }
        public int? HospitalCenterId { get; set; }
        public bool? RequiresPasswordChange { get; set; }
        public bool? HasActiveSessions { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Propriétés pour l'affichage des filtres actifs
        public bool HasActiveFilters => !string.IsNullOrEmpty(SearchTerm) ||
                                       IsActive.HasValue ||
                                       !string.IsNullOrEmpty(RoleFilter) ||
                                       HospitalCenterId.HasValue ||
                                       RequiresPasswordChange.HasValue ||
                                       HasActiveSessions.HasValue;

        public string ActiveFiltersText
        {
            get
            {
                var filters = new List<string>();
                if (!string.IsNullOrEmpty(SearchTerm)) filters.Add($"Recherche: '{SearchTerm}'");
                if (IsActive.HasValue) filters.Add($"Statut: {(IsActive.Value ? "Actif" : "Inactif")}");
                if (!string.IsNullOrEmpty(RoleFilter)) filters.Add($"Rôle: {RoleFilter}");
                if (RequiresPasswordChange.HasValue) filters.Add("Nécessite changement mot de passe");
                if (HasActiveSessions.HasValue) filters.Add("A des sessions actives");
                return string.Join(", ", filters);
            }
        }
    }

    /// <summary>
    /// Résultat de recherche d'utilisateurs avec métadonnées
    /// </summary>
    public class UserSearchResult
    {
        public List<UserSummary> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public UserManagementFilters AppliedFilters { get; set; } = new();
        public bool HasResults => Users.Any();
        public string ResultSummary => $"{TotalCount} utilisateur{(TotalCount > 1 ? "s" : "")} trouvé{(TotalCount > 1 ? "s" : "")}";
    }

    /// <summary>
    /// Historique de connexion détaillé
    /// </summary>
    public class UserLoginHistory
    {
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public bool IsCurrentSession { get; set; }
        public TimeSpan? SessionDuration => LogoutTime.HasValue ? LogoutTime.Value - LoginTime : null;
        public string DurationText => SessionDuration?.TotalHours > 0
            ? $"{(int)SessionDuration.Value.TotalHours}h {SessionDuration.Value.Minutes}min"
            : "En cours";
        public string LoginTimeText => LoginTime.ToString("dd/MM/yyyy HH:mm");
        public string LogoutTimeText => LogoutTime?.ToString("dd/MM/yyyy HH:mm") ?? "-";
    }
}