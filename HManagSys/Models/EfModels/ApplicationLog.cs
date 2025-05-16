using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class ApplicationLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? HospitalCenterId { get; set; }

    public string LogLevel { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? Details { get; set; }

    public string? EntityType { get; set; }

    public int? EntityId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? RequestPath { get; set; }

    public DateTime Timestamp { get; set; }
}
