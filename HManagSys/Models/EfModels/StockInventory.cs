using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class StockInventory : IEntity
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int HospitalCenterId { get; set; }

    public decimal CurrentQuantity { get; set; }

    public decimal? MinimumThreshold { get; set; }

    public decimal? MaximumThreshold { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
