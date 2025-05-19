using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Stock;
using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Patients;

/// <summary>
/// Filtres pour les épisodes de soins
/// </summary>
public class CareEpisodeFilters
{
    public string? SearchTerm { get; set; }
    public int? PatientId { get; set; }
    public int? HospitalCenterId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? PrimaryCaregiver { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Modèle pour la création d'un épisode de soins
/// </summary>
public class CreateCareEpisodeViewModel
{
    [Required(ErrorMessage = "Le patient est obligatoire")]
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le diagnostic est obligatoire")]
    public int DiagnosisId { get; set; }
    public string DiagnosisName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le soignant principal est obligatoire")]
    public int PrimaryCaregiver { get; set; }
    public string? PrimaryCaregiverName { get; set; }

    [Required(ErrorMessage = "La date de début est obligatoire")]
    public DateTime EpisodeStartDate { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    public int HospitalCenterId { get; set; }

    // Listes pour les dropdowns
    public List<SelectOption> DiagnosisOptions { get; set; } = new();
    public List<SelectOption> CaregiverOptions { get; set; } = new();
}

/// <summary>
/// Modèle pour la création d'un service de soins
/// </summary>
public class CreateCareServiceViewModel
{
    [Required]
    public int CareEpisodeId { get; set; }
    public string? PatientName { get; set; }

    [Required(ErrorMessage = "Le type de soin est obligatoire")]
    public int CareTypeId { get; set; }

    [Required(ErrorMessage = "Le soignant est obligatoire")]
    public int AdministeredBy { get; set; }

    [Required(ErrorMessage = "La date du service est obligatoire")]
    public DateTime ServiceDate { get; set; } = DateTime.Now;

    public int? Duration { get; set; }

    public string? Notes { get; set; }

    [Required(ErrorMessage = "Le coût est obligatoire")]
    [Range(0, 1000000, ErrorMessage = "Le coût doit être entre 0 et 1,000,000")]
    public decimal Cost { get; set; }

    // Produits utilisés
    public List<CareServiceProductItemViewModel> Products { get; set; } = null;

    // Listes pour les dropdowns
    public List<SelectOption> CareTypeOptions { get; set; } = new();
    public List<SelectOption> StaffOptions { get; set; } = new();
    public List<ProductViewModel> AvailableProducts { get; set; } = new();
}

/// <summary>
/// Modèle pour un produit utilisé dans un soin
/// </summary>
public class CareServiceProductItemViewModel
{
    //[Required]
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }

    //[Required]
    //[Range(0, 1000, ErrorMessage = "La quantité doit être entre 0.01 et 1000")]
    public decimal QuantityUsed { get; set; }

    public decimal UnitCost { get; set; }
    public decimal TotalCost => QuantityUsed * UnitCost;
}

public class CareServiceProductModalsViewModel
{

    public bool success { get; set; }

    public string message { get; set; } 

    public List<CareServiceProductItemViewModel> products = new();

    public decimal totalCost => products?.Sum(p => p.TotalCost) ?? 0;

}