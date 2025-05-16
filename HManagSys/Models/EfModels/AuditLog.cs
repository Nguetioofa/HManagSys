using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class AuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string ActionType { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public int? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? Description { get; set; }

    public string? IpAddress { get; set; }

    public int? HospitalCenterId { get; set; }

    public DateTime ActionDate { get; set; }

    public virtual HospitalCenter? HospitalCenter { get; set; }

    public virtual User? User { get; set; }
}
