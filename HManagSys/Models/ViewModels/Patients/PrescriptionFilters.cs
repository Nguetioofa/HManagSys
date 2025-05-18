using HManagSys.Models.ViewModels.Stock;
using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Patients;


/// <summary>
/// Filtres pour les prescriptions
/// </summary>
public class PrescriptionFilters
{
    public string? SearchTerm { get; set; }
    public int? PatientId { get; set; }
    public int? HospitalCenterId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? PrescribedBy { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Modèle pour la création d'une prescription
/// </summary>
public class CreatePrescriptionViewModel
{
    [Required(ErrorMessage = "Le patient est obligatoire")]
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public int? DiagnosisId { get; set; }
    public string? DiagnosisName { get; set; }

    public int? CareEpisodeId { get; set; }
    public string? CareEpisodeName { get; set; }

    [Required(ErrorMessage = "La date de prescription est obligatoire")]
    public DateTime PrescriptionDate { get; set; } = DateTime.Now;

    public string? Instructions { get; set; }

    public int HospitalCenterId { get; set; }

    // Items de la prescription
    public List<PrescriptionItemFormViewModel> Items { get; set; } = new();

    // Listes pour les dropdowns
    public List<SelectOption> DiagnosisOptions { get; set; } = new();
    public List<SelectOption> CareEpisodeOptions { get; set; } = new();
    public List<ProductViewModel> AvailableProducts { get; set; } = new();
}

/// <summary>
/// Item d'une prescription dans le formulaire
/// </summary>
public class PrescriptionItemFormViewModel
{
    [Required]
    public int ProductId { get; set; }
    public string? ProductName { get; set; }

    [Required]
    [Range(0.01, 1000, ErrorMessage = "La quantité doit être entre 0.01 et 1000")]
    public decimal Quantity { get; set; }

    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
}