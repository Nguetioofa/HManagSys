using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class Prescription : IEntity
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int? DiagnosisId { get; set; }

    public int? CareEpisodeId { get; set; }

    public int HospitalCenterId { get; set; }

    public int PrescribedBy { get; set; }

    public DateTime PrescriptionDate { get; set; }

    public string? Instructions { get; set; }

    public string Status { get; set; } = null!;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual CareEpisode? CareEpisode { get; set; }

    public virtual Diagnosis? Diagnosis { get; set; }

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual User PrescribedByNavigation { get; set; } = null!;

    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
