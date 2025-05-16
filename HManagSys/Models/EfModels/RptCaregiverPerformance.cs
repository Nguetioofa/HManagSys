using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class RptCaregiverPerformance : IEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string CaregiverName { get; set; } = null!;

    public int HospitalCenterId { get; set; }

    public string HospitalCenterName { get; set; } = null!;

    public DateOnly ReportDate { get; set; }

    public int PatientsServed { get; set; }

    public int CareServicesProvided { get; set; }

    public int ExaminationsRequested { get; set; }

    public int PrescriptionsIssued { get; set; }

    public int SalesMade { get; set; }

    public decimal TotalRevenueGenerated { get; set; }

    public DateTime ReportGeneratedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
}
