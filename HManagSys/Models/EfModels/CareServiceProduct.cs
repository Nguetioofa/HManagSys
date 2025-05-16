using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class CareServiceProduct : IEntity
{
    public int Id { get; set; }

    public int CareServiceId { get; set; }

    public int ProductId { get; set; }

    public decimal QuantityUsed { get; set; }

    public decimal UnitCost { get; set; }

    public decimal TotalCost { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual CareService CareService { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
