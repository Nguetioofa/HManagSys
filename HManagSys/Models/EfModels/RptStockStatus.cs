using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class RptStockStatus : IEntity
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string ProductCategory { get; set; } = null!;

    public int HospitalCenterId { get; set; }

    public string HospitalCenterName { get; set; } = null!;

    public decimal CurrentQuantity { get; set; }

    public decimal? MinimumThreshold { get; set; }

    public decimal? MaximumThreshold { get; set; }

    public string StockStatus { get; set; } = null!;

    public DateTime? LastMovementDate { get; set; }

    public DateTime ReportGeneratedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
}
