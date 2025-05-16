using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class CashHandover : IEntity
{
    public int Id { get; set; }

    public int HospitalCenterId { get; set; }

    public int FinancierId { get; set; }

    public DateTime HandoverDate { get; set; }

    public decimal TotalCashAmount { get; set; }

    public decimal HandoverAmount { get; set; }

    public decimal RemainingCashAmount { get; set; }

    public int HandedOverBy { get; set; }

    public string? Notes { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Financier Financier { get; set; } = null!;

    public virtual User HandedOverByNavigation { get; set; } = null!;

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;
}
