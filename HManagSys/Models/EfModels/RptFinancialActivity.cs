using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class RptFinancialActivity : IEntity
{
    public int Id { get; set; }

    public int HospitalCenterId { get; set; }

    public string HospitalCenterName { get; set; } = null!;

    public DateOnly ReportDate { get; set; }

    public decimal TotalSales { get; set; }

    public decimal TotalCareRevenue { get; set; }

    public decimal TotalExaminationRevenue { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal TotalCashPayments { get; set; }

    public decimal TotalMobilePayments { get; set; }

    public int TransactionCount { get; set; }

    public int PatientCount { get; set; }

    public DateTime ReportGeneratedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
}
