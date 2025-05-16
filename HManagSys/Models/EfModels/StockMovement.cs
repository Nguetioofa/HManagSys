using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class StockMovement : IEntity
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int HospitalCenterId { get; set; }

    public string MovementType { get; set; } = null!;

    public decimal Quantity { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public string? Notes { get; set; }

    public DateTime MovementDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
