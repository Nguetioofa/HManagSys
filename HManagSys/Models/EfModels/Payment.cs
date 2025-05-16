using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class Payment : IEntity
{
    public int Id { get; set; }

    public string ReferenceType { get; set; } = null!;

    public int ReferenceId { get; set; }

    public int? PatientId { get; set; }

    public int HospitalCenterId { get; set; }

    public int PaymentMethodId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public int ReceivedBy { get; set; }

    public string? TransactionReference { get; set; }

    public string? Notes { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;

    public virtual Patient? Patient { get; set; }

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual User ReceivedByNavigation { get; set; } = null!;
}
