using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Patients;

/// <summary>
/// Filtres pour la recherche des patients
/// </summary>
public class PatientFilters
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public int? HospitalCenterId { get; set; }
    public string? BloodType { get; set; }
    public string? Gender { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Modèle pour l'affichage d'un patient dans une liste
/// </summary>
public class PatientViewModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? BloodType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Données calculées
    public int? Age => DateOfBirth.HasValue
        ? CalculateAge(DateOfBirth.Value.ToDateTime(TimeOnly.MinValue))
        : null;
    public int DiagnosisCount { get; set; }
    public int CareEpisodeCount { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public bool HasRecentDiagnosis => LastVisitDate.HasValue && (DateTime.Now - LastVisitDate.Value).TotalDays <= 30;

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}

/// <summary>
/// Modèle détaillé d'un patient
/// </summary>
public class PatientDetailsViewModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Relations
    public List<DiagnosisViewModel> Diagnoses { get; set; } = new();
    public List<CareEpisodeViewModel> CareEpisodes { get; set; } = new();
    public List<PrescriptionViewModel> Prescriptions { get; set; } = new();
    public List<ExaminationViewModel> Examinations { get; set; } = new();

    // Statistiques
    public int TotalVisits => CareEpisodes.Count;
    public decimal TotalSpent { get; set; }
    public int? Age => DateOfBirth.HasValue
        ? CalculateAge(DateOfBirth.Value.ToDateTime(TimeOnly.MinValue))
        : null;
    public DateTime? LastVisitDate => CareEpisodes.Any()
        ? CareEpisodes.Max(ce => ce.EpisodeStartDate)
        : null;

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}

/// <summary>
/// Modèle pour la création d'un patient
/// </summary>
public class CreatePatientViewModel
{
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est obligatoire")]
    [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    [Display(Name = "Genre")]
    public string? Gender { get; set; }

    [Required(ErrorMessage = "Le numéro de téléphone est obligatoire")]
    [Phone(ErrorMessage = "Numéro de téléphone invalide")]
    [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string? Email { get; set; }

    public string? Address { get; set; }

    [Display(Name = "Contact d'urgence")]
    public string? EmergencyContactName { get; set; }

    [Display(Name = "Téléphone d'urgence")]
    [Phone(ErrorMessage = "Numéro de téléphone d'urgence invalide")]
    public string? EmergencyContactPhone { get; set; }

    [Display(Name = "Groupe sanguin")]
    public string? BloodType { get; set; }

    [Display(Name = "Allergies")]
    public string? Allergies { get; set; }

    public bool IsActive { get; set; } = true;

    // Options pour les listes déroulantes
    public List<SelectOption> GenderOptions { get; set; } = new();
    public List<SelectOption> BloodTypeOptions { get; set; } = new();
}

/// <summary>
/// Modèle pour la modification d'un patient
/// </summary>
public class EditPatientViewModel : CreatePatientViewModel
{
    public int Id { get; set; }
}

/// <summary>
/// Résultat de recherche patient rapide
/// </summary>
public class PatientSearchResultViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Statistiques sur les patients
/// </summary>
public class PatientStatisticsViewModel
{
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int InactivePatients { get; set; }
    public int NewPatientsThisMonth { get; set; }
    public int PatientsWithDiagnosis { get; set; }
    public int PatientsWithActiveCare { get; set; }

    // Statistiques par genre
    public int MalePatients { get; set; }
    public int FemalePatients { get; set; }
    public int OtherGenderPatients { get; set; }

    // Statistiques d'âge
    public int PatientsUnder18 { get; set; }
    public int Patients18To40 { get; set; }
    public int PatientsOver40 { get; set; }
}