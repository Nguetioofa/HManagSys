using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class Financier : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int HospitalCenterId { get; set; }

    public string? ContactInfo { get; set; }

    public bool IsActive { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<CashHandover> CashHandovers { get; set; } = new List<CashHandover>();

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;
}
