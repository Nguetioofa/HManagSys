using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Patients;




/// <summary>
/// Filtres pour les examens
/// </summary>
public class ExaminationFilters
{
    public string? SearchTerm { get; set; }
    public int? PatientId { get; set; }
    public int? HospitalCenterId { get; set; }
    public int? ExaminationTypeId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? RequestedBy { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Modèle pour la création d'un examen
/// </summary>
public class CreateExaminationViewModel
{
    [Required(ErrorMessage = "Le patient est obligatoire")]
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le type d'examen est obligatoire")]
    public int ExaminationTypeId { get; set; }

    public int? CareEpisodeId { get; set; }
    public string? CareEpisodeName { get; set; }

    [Required(ErrorMessage = "La date de demande est obligatoire")]
    public DateTime RequestDate { get; set; } = DateTime.Now;

    public DateTime? ScheduledDate { get; set; }

    [Required(ErrorMessage = "Le prix est obligatoire")]
    [Range(0, 1000000, ErrorMessage = "Le prix doit être entre 0 et 1,000,000")]
    public decimal FinalPrice { get; set; }

    [Range(0, 1000000, ErrorMessage = "La remise doit être entre 0 et 1,000,000")]
    public decimal DiscountAmount { get; set; }

    public string? Notes { get; set; }

    public int HospitalCenterId { get; set; }

    // Listes pour les dropdowns
    public List<SelectOption> ExaminationTypeOptions { get; set; } = new();
    public List<SelectOption> CareEpisodeOptions { get; set; } = new();
}

/// <summary>
/// Modèle pour la création d'un résultat d'examen
/// </summary>
public class CreateExaminationResultViewModel
{
    [Required]
    public int ExaminationId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ExaminationTypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Les données du résultat sont obligatoires")]
    public string ResultData { get; set; } = string.Empty;

    public string? ResultNotes { get; set; }

    public IFormFile? Attachment { get; set; }

    [Required(ErrorMessage = "La date du rapport est obligatoire")]
    public DateTime ReportDate { get; set; } = DateTime.Now;
}

public class CompleteExaminationViewModel
{
    public DateTime PerformedDate { get; set; }
}