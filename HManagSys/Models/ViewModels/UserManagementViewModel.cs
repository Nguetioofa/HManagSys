namespace HManagSys.Models.ViewModels
{
    /// <summary>
    /// Modèle pour la gestion des utilisateurs
    /// Comme un carnet de personnel avec toutes les informations
    /// </summary>
    public class UserManagementViewModel
    {
        public List<UserSummary> Users { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public string? StatusFilter { get; set; }
    }

    public class UserSummary
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool MustChangePassword { get; set; }
        public List<AssignmentInfo> Assignments { get; set; } = new();
    }

    public class AssignmentInfo
    {
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
