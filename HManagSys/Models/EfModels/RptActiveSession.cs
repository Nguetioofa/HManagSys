using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class RptActiveSession : IEntity
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string CurrentHospitalCenter { get; set; } = null!;

    public DateTime LoginTime { get; set; }

    public string? IpAddress { get; set; }

    public int HoursConnected { get; set; }

    public DateTime ReportGeneratedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
}
