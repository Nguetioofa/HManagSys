using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class CareEpisode : IEntity
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int DiagnosisId { get; set; }

    public int HospitalCenterId { get; set; }

    public int PrimaryCaregiver { get; set; }

    public DateTime EpisodeStartDate { get; set; }

    public DateTime? EpisodeEndDate { get; set; }

    public string Status { get; set; } = null!;

    public string? InterruptionReason { get; set; }

    public decimal TotalCost { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal RemainingBalance { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<CareService> CareServices { get; set; } = new List<CareService>();

    public virtual Diagnosis Diagnosis { get; set; } = null!;

    public virtual ICollection<Examination> Examinations { get; set; } = new List<Examination>();

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual User PrimaryCaregiverNavigation { get; set; } = null!;
}
