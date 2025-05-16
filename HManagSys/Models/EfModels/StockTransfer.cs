using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class StockTransfer : IEntity
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int FromHospitalCenterId { get; set; }

    public int ToHospitalCenterId { get; set; }

    public decimal Quantity { get; set; }

    public string? TransferReason { get; set; }

    public string Status { get; set; } = null!;

    public DateTime RequestDate { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public int? ApprovedBy { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual HospitalCenter FromHospitalCenter { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual HospitalCenter ToHospitalCenter { get; set; } = null!;
}
