using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class Sale : IEntity
{
    public int Id { get; set; }

    public string SaleNumber { get; set; } = null!;

    public int? PatientId { get; set; }

    public int HospitalCenterId { get; set; }

    public int SoldBy { get; set; }

    public DateTime SaleDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string? Notes { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;

    public virtual Patient? Patient { get; set; }

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public virtual User SoldByNavigation { get; set; } = null!;
}
