using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Finance
{
    /// <summary>
    /// Filtres pour la recherche des financiers
    /// </summary>
    public class FinancierFilters
    {
        public string? SearchTerm { get; set; }
        public int? HospitalCenterId { get; set; }
        public bool? IsActive { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Modèle pour l'affichage d'un financier
    /// </summary>
    public class FinancierViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public string? ModifiedByName { get; set; }
        public DateTime? ModifiedAt { get; set; }

        // Propriétés pour l'affichage
        public string StatusBadge => IsActive ? "badge bg-success" : "badge bg-danger";
        public string StatusText => IsActive ? "Actif" : "Inactif";
        public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy HH:mm");

        // Statistiques
        public int TotalHandovers { get; set; }
        public decimal TotalAmountCollected { get; set; }
        public DateTime? LastHandoverDate { get; set; }

        // Formatage monétaire 
        public string FormattedTotalAmountCollected => $"{TotalAmountCollected:N0} FCFA";
    }

    /// <summary>
    /// Modèle pour la création d'un financier
    /// </summary>
    public class CreateFinancierViewModel
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le centre hospitalier est obligatoire")]
        [Display(Name = "Centre hospitalier")]
        public int HospitalCenterId { get; set; }

        [Display(Name = "Informations de contact")]
        public string? ContactInfo { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Modèle pour la modification d'un financier
    /// </summary>
    public class EditFinancierViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Informations de contact")]
        public string? ContactInfo { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; }
    }
}