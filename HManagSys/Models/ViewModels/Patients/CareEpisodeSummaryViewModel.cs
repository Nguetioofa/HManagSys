using HManagSys.Models;
using HManagSys.Models.ViewModels.Patients;

/// <summary>
/// ViewModel pour les épisodes de soins
/// </summary>
public class CareEpisodeViewModel
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DiagnosisId { get; set; }
    public string DiagnosisName { get; set; } = string.Empty;
    public int HospitalCenterId { get; set; }
    public string HospitalCenterName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int PrimaryCaregiverId { get; set; }
    public string PrimaryCaregiverName { get; set; } = string.Empty;
    public DateTime EpisodeStartDate { get; set; }
    public DateTime? EpisodeEndDate { get; set; }
    public string Status { get; set; } = string.Empty; // "Active", "Completed", "Interrupted"
    public string? InterruptionReason { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingBalance { get; set; }

    // Relations
    public List<CareServiceViewModel>? CareServices { get; set; }

    // Propriétés calculées
    public int DurationDays => EpisodeEndDate.HasValue
        ? (int)(EpisodeEndDate.Value - EpisodeStartDate).TotalDays
        : (int)(DateTime.Now - EpisodeStartDate).TotalDays;

    public string StatusClass => Status switch
    {
        "Active" => "text-success",
        "Completed" => "text-primary",
        "Interrupted" => "text-warning",
        _ => "text-secondary"
    };

    public bool IsComplete => Status == "Completed";
}


/// <summary>
/// ViewModel pour les items de prescription
/// </summary>
public class PrescriptionItemViewModel
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }

    public string FullInstructions => string.Join(" - ",
        new[] { Dosage, Frequency, Duration, Instructions }
        .Where(s => !string.IsNullOrEmpty(s)));

    public string FormattedDosage => !string.IsNullOrEmpty(Dosage) ? Dosage : "N/A";
    public string FormattedFrequency => !string.IsNullOrEmpty(Frequency) ? Frequency : "N/A";
    public string FormattedDuration => !string.IsNullOrEmpty(Duration) ? Duration : "N/A";
    public string FormattedInstructions => !string.IsNullOrEmpty(Instructions) ? Instructions : "N/A";
}

/// <summary>
/// ViewModel pour les examens médicaux
/// </summary>
public class ExaminationViewModel
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int ExaminationTypeId { get; set; }
    public string ExaminationTypeName { get; set; } = string.Empty;
    public int? CareEpisodeId { get; set; }
    public int HospitalCenterId { get; set; }
    public string HospitalCenterName { get; set; } = string.Empty;
    public int RequestedById { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public int? PerformedById { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? PerformedDate { get; set; }
    public string Status { get; set; } = string.Empty; // "Requested", "Scheduled", "Completed", "Cancelled"
    public decimal FinalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }

    // Résultat de l'examen
    public ExaminationResultViewModel? Result { get; set; }

    // Propriétés calculées
    public bool IsCompleted => Status == "Completed";
    public bool HasResult => Result != null;

    public string StatusClass => Status switch
    {
        "Completed" => "text-success",
        "Scheduled" => "text-primary",
        "Requested" => "text-warning",
        "Cancelled" => "text-danger",
        _ => "text-secondary"
    };
}

/// <summary>
/// ViewModel pour les résultats d'examen
/// </summary>
public class ExaminationResultViewModel
{
    public int Id { get; set; }
    public int ExaminationId { get; set; }
    public string? ResultData { get; set; }
    public string? ResultNotes { get; set; }
    public string? AttachmentPath { get; set; }
    public int ReportedById { get; set; }
    public string ReportedByName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }

    public bool HasAttachment => !string.IsNullOrEmpty(AttachmentPath);
}

/// <summary>
/// ViewModel pour les services de soins
/// </summary>
public class CareServiceViewModel
{
    public int Id { get; set; }
    public int CareEpisodeId { get; set; }
    public int CareTypeId { get; set; }
    public string CareTypeName { get; set; } = string.Empty;
    public int AdministeredById { get; set; }
    public string AdministeredByName { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public int? Duration { get; set; }
    public string? Notes { get; set; }
    public decimal Cost { get; set; }

    // Produits utilisés lors du soin
    public List<CareServiceProductViewModel>? UsedProducts { get; set; }

    // Propriétés calculées
    public int ProductCount => UsedProducts?.Count ?? 0;
    public decimal ProductCostTotal => UsedProducts?.Sum(p => p.TotalCost) ?? 0;
}

/// <summary>
/// ViewModel pour les produits utilisés dans les soins
/// </summary>
public class CareServiceProductViewModel
{
    public int Id { get; set; }
    public int CareServiceId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantityUsed { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// ViewModel générique paginé avec filtres
/// </summary>
/// <typeparam name="T">Type des éléments de la liste</typeparam>
/// <typeparam name="F">Type des filtres</typeparam>
public class PagedViewModel<T, F> where T : class where F : class
{
    public List<T> Items { get; set; } = new List<T>();
    public F Filters { get; set; } = null!;
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
    public PatientStatisticsViewModel? ExtraData { get; set; }

    public bool HasItems => Items.Any();
    public string ResultSummary => $"{Pagination.TotalCount} résultat{(Pagination.TotalCount > 1 ? "s" : "")} trouvé{(Pagination.TotalCount > 1 ? "s" : "")}";
}
