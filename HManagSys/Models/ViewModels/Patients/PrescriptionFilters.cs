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

    public string? DiagnosisName { get; set; }

    [Display(Name = "Épisode de soins")]
    public int? CareEpisodeId { get; set; }
    public string? CareEpisodeName { get; set; }

    [Required(ErrorMessage = "La date de prescription est obligatoire")]
    public DateTime PrescriptionDate { get; set; } = DateTime.Now;

    [Display(Name = "Instructions")]
    [DataType(DataType.MultilineText)]
    public string? Instructions { get; set; }

    public int HospitalCenterId { get; set; }

    // Items de la prescription
    [Display(Name = "Produits prescrits")]
    public List<CreatePrescriptionItemViewModel> Items { get; set; } = new();
    public string Status { get; set; } = "Pending";


    // Listes pour les dropdowns

    [Display(Name = "Diagnostic")]
    public int? DiagnosisId { get; set; }
    public List<SelectOption> DiagnosisOptions { get; set; } = new();

    public List<SelectOption> CareEpisodeOptions { get; set; } = new();
    public List<ProductViewModel> AvailableProducts { get; set; } = new();
}


/// <summary>
/// Vue détaillée d'une prescription
/// </summary>
public class PrescriptionViewModel
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? DiagnosisId { get; set; }
    public string? DiagnosisName { get; set; }
    public int? CareEpisodeId { get; set; }
    public string? CareEpisodeName { get; set; }
    public int HospitalCenterId { get; set; }
    public string HospitalCenterName { get; set; } = string.Empty;
    public int PrescribedById { get; set; }
    public string PrescribedByName { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public string? Instructions { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<PrescriptionItemViewModel> Items { get; set; } = new();

    // Propriétés calculées pour l'affichage
    public string FormattedDate => PrescriptionDate.ToString("dd/MM/yyyy HH:mm");
    public string StatusBadge => Status switch
    {
        "Pending" => "badge bg-warning",
        "Dispensed" => "badge bg-success",
        "Canceled" => "badge bg-danger",
        _ => "badge bg-secondary"
    };
    public string StatusText => Status switch
    {
        "Pending" => "En attente",
        "Dispensed" => "Dispensée",
        "Canceled" => "Annulée",
        _ => Status
    };
    public int TotalItems => Items.Count;
    public bool CanDispense => Status == "Pending";
    public bool CanEdit => Status == "Pending";
}


/// <summary>
/// Item d'une prescription dans le formulaire
/// </summary>
public class PrescriptionItemFormViewModel
{
    [Required]
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int PrescriptionId { get; set; }

    [Required]
    [Range(0.01, 1000, ErrorMessage = "La quantité doit être entre 0.01 et 1000")]
    public decimal Quantity { get; set; }

    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }

    // Propriétés calculées pour l'affichage
    public string FormattedDosage => !string.IsNullOrEmpty(Dosage) ? Dosage : "N/A";
    public string FormattedFrequency => !string.IsNullOrEmpty(Frequency) ? Frequency : "N/A";
    public string FormattedDuration => !string.IsNullOrEmpty(Duration) ? Duration : "N/A";
    public string FormattedInstructions => !string.IsNullOrEmpty(Instructions) ? Instructions : "N/A";
}


/// <summary>
/// Modèle pour la création d'un item de prescription
/// </summary>
public class CreatePrescriptionItemViewModel
{
    public int PrescriptionId { get; set; }

    [Required(ErrorMessage = "Le produit est requis")]
    [Display(Name = "Produit")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "La quantité est requise")]
    [Display(Name = "Quantité")]
    [Range(0.1, double.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
    public decimal Quantity { get; set; } = 1;

    [Display(Name = "Dosage")]
    public string? Dosage { get; set; }

    [Display(Name = "Fréquence")]
    public string? Frequency { get; set; }

    [Display(Name = "Durée")]
    public string? Duration { get; set; }

    [Display(Name = "Instructions")]
    public string? Instructions { get; set; }
}

/// <summary>
/// Modèle pour la modification d'une prescription
/// </summary>
public class EditPrescriptionViewModel
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    [Display(Name = "Diagnostic")]
    public int? DiagnosisId { get; set; }
    public List<SelectOption> DiagnosisOptions { get; set; } = new();

    [Display(Name = "Épisode de soins")]
    public int? CareEpisodeId { get; set; }
    public List<SelectOption> CareEpisodeOptions { get; set; } = new();

    public int HospitalCenterId { get; set; }

    [Required(ErrorMessage = "La date de prescription est requise")]
    [Display(Name = "Date de prescription")]
    [DataType(DataType.DateTime)]
    public DateTime PrescriptionDate { get; set; }

    [Display(Name = "Instructions")]
    [DataType(DataType.MultilineText)]
    public string? Instructions { get; set; }

    public string Status { get; set; } = "Pending";

    [Display(Name = "Produits prescrits")]
    public List<EditPrescriptionItemViewModel> Items { get; set; } = new();

    // Produits disponibles pour la sélection
    public List<ProductViewModel> AvailableProducts { get; set; } = new();
}

/// <summary>
/// Modèle pour la modification d'un item de prescription
/// </summary>
public class EditPrescriptionItemViewModel
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }

    [Required(ErrorMessage = "Le produit est requis")]
    [Display(Name = "Produit")]
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La quantité est requise")]
    [Display(Name = "Quantité")]
    [Range(0.1, double.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
    public decimal Quantity { get; set; }

    [Display(Name = "Dosage")]
    public string? Dosage { get; set; }

    [Display(Name = "Fréquence")]
    public string? Frequency { get; set; }

    [Display(Name = "Durée")]
    public string? Duration { get; set; }

    [Display(Name = "Instructions")]
    public string? Instructions { get; set; }
}

/// <summary>
/// Modèle de présentation simple d'un produit
/// Pour sélection dans les listes déroulantes
/// </summary>
public class ProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public decimal? AvailableQuantity { get; set; }

    // Propriétés calculées pour l'affichage
    public string DisplayName => $"{Name} ({UnitOfMeasure})";
    public string FormattedPrice => $"{SellingPrice:N0} FCFA";
    public string StockStatus => AvailableQuantity.HasValue
        ? AvailableQuantity.Value > 0 ? "En stock" : "Rupture de stock"
        : "Stock inconnu";
}