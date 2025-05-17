using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Patients;

/// <summary>
/// Modèle pour l'affichage d'un diagnostic
/// </summary>
public class DiagnosisViewModel
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int HospitalCenterId { get; set; }
    public string HospitalCenterName { get; set; } = string.Empty;
    public int DiagnosedById { get; set; }
    public string DiagnosedByName { get; set; } = string.Empty;
    public string? DiagnosisCode { get; set; }
    public string DiagnosisName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Severity { get; set; }
    public DateTime DiagnosisDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Données calculées
    public bool IsRecent => (DateTime.Now - DiagnosisDate).TotalDays <= 30;
    public string SeverityClass => Severity switch
    {
        "Critical" => "text-danger",
        "Severe" => "text-warning",
        "Moderate" => "text-primary",
        "Mild" => "text-success",
        _ => "text-secondary"
    };
}

/// <summary>
/// Modèle pour la création d'un diagnostic
/// </summary>
public class CreateDiagnosisViewModel
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom du diagnostic est obligatoire")]
    [Display(Name = "Nom du diagnostic")]
    public string DiagnosisName { get; set; } = string.Empty;

    [Display(Name = "Code du diagnostic")]
    public string? DiagnosisCode { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Sévérité")]
    public string? Severity { get; set; }

    [Display(Name = "Date du diagnostic")]
    [Required(ErrorMessage = "La date du diagnostic est obligatoire")]
    public DateTime DiagnosisDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    // Le centre hospitalier actuel et l'utilisateur sont définis côté serveur
    public int HospitalCenterId { get; set; }

    // Options pour les listes déroulantes
    public List<SelectOption> SeverityOptions { get; set; } = new();
}

/// <summary>
/// Historique médical complet d'un patient
/// </summary>
public class PatientHistoryViewModel
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    // Historique chronologique (tous types confondus)
    public List<MedicalEvent> ChronologicalHistory { get; set; } = new();

    // Stats rapides
    public int TotalDiagnoses { get; set; }
    public int TotalCareEpisodes { get; set; }
    public int TotalExaminations { get; set; }
    public int TotalPrescriptions { get; set; }
}

/// <summary>
/// Événement médical pour l'historique
/// </summary>
public class MedicalEvent
{
    public DateTime Date { get; set; }
    public string EventType { get; set; } = string.Empty; // "Diagnosis", "CareEpisode", "Examination", "Prescription"
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HospitalCenterName { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;

    // Classe CSS pour l'affichage
    public string EventClass => EventType switch
    {
        "Diagnosis" => "diagnosis-event",
        "CareEpisode" => "care-event",
        "Examination" => "exam-event",
        "Prescription" => "prescription-event",
        _ => "general-event"
    };

    // Icône pour l'affichage
    public string EventIcon => EventType switch
    {
        "Diagnosis" => "fa-stethoscope",
        "CareEpisode" => "fa-procedures",
        "Examination" => "fa-microscope",
        "Prescription" => "fa-prescription",
        _ => "fa-calendar"
    };
}