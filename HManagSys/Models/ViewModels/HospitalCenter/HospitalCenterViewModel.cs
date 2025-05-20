using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.HospitalCenter
{
    public class HospitalCenterViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nom")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Adresse")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Téléphone")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; }

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Date de modification")]
        public DateTime? ModifiedAt { get; set; }

        [Display(Name = "Utilisateurs actifs")]
        public int ActiveUsersCount { get; set; }

        // Propriétés calculées
        public string StatusText => IsActive ? "Actif" : "Inactif";
        public string StatusBadge => IsActive ? "badge bg-success" : "badge bg-secondary";
        public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy HH:mm");
        public string FormattedModifiedAt => ModifiedAt?.ToString("dd/MM/yyyy HH:mm") ?? "-";
    }

    public class HospitalCenterDetailsViewModel : HospitalCenterViewModel
    {
        [Display(Name = "Créé par")]
        public string CreatedByName { get; set; } = string.Empty;

        [Display(Name = "Modifié par")]
        public string? ModifiedByName { get; set; }

        [Display(Name = "Produits en stock")]
        public int ProductsInStock { get; set; }

        [Display(Name = "Ventes totales")]
        public int TotalSales { get; set; }

        [Display(Name = "Épisodes de soins actifs")]
        public int ActiveCareEpisodes { get; set; }

        [Display(Name = "Utilisateurs actifs")]
        public int ActiveUsers { get; set; }
    }

    public class CreateHospitalCenterViewModel
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom du centre")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse est obligatoire")]
        [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        [Display(Name = "Adresse")]
        public string Address { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
        [Display(Name = "Téléphone")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; } = true;
    }

    public class EditHospitalCenterViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom du centre")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse est obligatoire")]
        [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        [Display(Name = "Adresse")]
        public string Address { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
        [Display(Name = "Téléphone")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; }
    }

    public class HospitalCenterFilters
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public string? Region { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class HospitalCenterListViewModel
    {
        public List<HospitalCenterViewModel> Centers { get; set; } = new();
        public HospitalCenterFilters Filters { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public NetworkStatistics Statistics { get; set; } = new();
    }

    public class NetworkStatistics
    {
        public int TotalCenters { get; set; }
        public int ActiveCenters { get; set; }
        public int TotalUsersNetwork { get; set; }
        public decimal TotalSalesToday { get; set; }
        public int ActiveCareEpisodesNetwork { get; set; }

        public string FormattedTotalSalesToday => $"{TotalSalesToday:N0} FCFA";
    }

    public class CenterActivityReport
    {
        public int HospitalCenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CareEpisodesCreated { get; set; }
        public int ExaminationsPerformed { get; set; }
        public int UniquePatients { get; set; }
        public DateTime ReportGeneratedAt { get; set; }

        public string FormattedFromDate => FromDate.ToString("dd/MM/yyyy");
        public string FormattedToDate => ToDate.ToString("dd/MM/yyyy");
        public string FormattedTotalRevenue => $"{TotalRevenue:N0} FCFA";
        public string FormattedReportDate => ReportGeneratedAt.ToString("dd/MM/yyyy HH:mm");
    }
}