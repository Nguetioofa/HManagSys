using HManagSys.Models.ViewModels.Stock;
using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Patients;

/// <summary>
/// Modèle pour terminer un épisode de soins
/// </summary>
public class CompleteCareEpisodeViewModel
{
    [Required]
    public int CareEpisodeId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La date de fin est obligatoire")]
    public DateTime CompletionDate { get; set; } = DateTime.Now;

    public string? Notes { get; set; }
}

/// <summary>
/// Modèle pour interrompre un épisode de soins
/// </summary>
public class InterruptCareEpisodeViewModel
{
    [Required]
    public int CareEpisodeId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La date d'interruption est obligatoire")]
    public DateTime InterruptionDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "La raison de l'interruption est obligatoire")]
    public string InterruptionReason { get; set; } = string.Empty;
}

/// <summary>
/// Modèle pour la modification d'un épisode de soins
/// </summary>
public class EditCareEpisodeViewModel
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Le diagnostic est obligatoire")]
    public int DiagnosisId { get; set; }
    public string DiagnosisName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le soignant principal est obligatoire")]
    public int PrimaryCaregiver { get; set; }
    public string? PrimaryCaregiverName { get; set; }

    [Required(ErrorMessage = "La date de début est obligatoire")]
    public DateTime EpisodeStartDate { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = "Active";

    // Listes pour les dropdowns
    public List<SelectOption> DiagnosisOptions { get; set; } = new();
    public List<SelectOption> CaregiverOptions { get; set; } = new();
}

/// <summary>
/// Modèle pour planifier un examen
/// </summary>
public class ScheduleExaminationViewModel
{
    [Required]
    public int ExaminationId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ExaminationTypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La date de l'examen est obligatoire")]
    public DateTime ScheduledDate { get; set; } = DateTime.Now.AddDays(1);

    [Required(ErrorMessage = "L'exécutant est obligatoire")]
    public int PerformedBy { get; set; }

    public string? Notes { get; set; }

    // Liste pour le dropdown
    public List<SelectOption> PerformedByOptions { get; set; } = new();
}

/// <summary>
/// Modèle pour la modification d'une prescription
/// </summary>
public class EditPrescriptionViewModel
{
    [Required]
    public int Id { get; set; }

    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public int? DiagnosisId { get; set; }
    public string? DiagnosisName { get; set; }

    public int? CareEpisodeId { get; set; }
    public string? CareEpisodeName { get; set; }

    [Required(ErrorMessage = "La date de prescription est obligatoire")]
    public DateTime PrescriptionDate { get; set; }

    public string? Instructions { get; set; }

    public string Status { get; set; } = "Pending";

    // Items de la prescription
    public List<PrescriptionItemViewModel> Items { get; set; } = new();

    // Listes pour les dropdowns
    public List<SelectOption> DiagnosisOptions { get; set; } = new();
    public List<SelectOption> CareEpisodeOptions { get; set; } = new();
}

/// <summary>
/// Modèle pour la création d'un item de prescription
/// </summary>
public class CreatePrescriptionItemViewModel
{
    [Required]
    public int PrescriptionId { get; set; }

    [Required(ErrorMessage = "Le produit est obligatoire")]
    public int ProductId { get; set; }
    public string? ProductName { get; set; }

    [Required(ErrorMessage = "La quantité est obligatoire")]
    [Range(0.01, 1000, ErrorMessage = "La quantité doit être entre 0.01 et 1000")]
    public decimal Quantity { get; set; }

    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }

    // Liste des produits disponibles
    public List<ProductViewModel> AvailableProducts { get; set; } = new();
}
