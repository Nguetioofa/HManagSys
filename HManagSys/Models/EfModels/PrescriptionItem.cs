using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class PrescriptionItem : IEntity
{
    public int Id { get; set; }

    public int PrescriptionId { get; set; }

    public int ProductId { get; set; }

    public decimal Quantity { get; set; }

    public string? Dosage { get; set; }

    public string? Frequency { get; set; }

    public string? Duration { get; set; }

    public string? Instructions { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Prescription Prescription { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
