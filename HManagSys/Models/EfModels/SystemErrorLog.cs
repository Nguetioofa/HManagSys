using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class SystemErrorLog
{
    public int Id { get; set; }

    public Guid ErrorId { get; set; }

    public int? UserId { get; set; }

    public int? HospitalCenterId { get; set; }

    public string Severity { get; set; } = null!;

    public string Source { get; set; } = null!;

    public string ErrorType { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? StackTrace { get; set; }

    public string? InnerException { get; set; }

    public string? RequestData { get; set; }

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }

    public string? RequestPath { get; set; }

    public bool IsResolved { get; set; }

    public int? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? ResolutionNotes { get; set; }

    public DateTime Timestamp { get; set; }
}
