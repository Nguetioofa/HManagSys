using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class Diagnosis : IEntity
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int HospitalCenterId { get; set; }

    public int DiagnosedBy { get; set; }

    public string? DiagnosisCode { get; set; }

    public string DiagnosisName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Severity { get; set; }

    public DateTime DiagnosisDate { get; set; }

    public bool IsActive { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<CareEpisode> CareEpisodes { get; set; } = new List<CareEpisode>();

    public virtual User DiagnosedByNavigation { get; set; } = null!;

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
