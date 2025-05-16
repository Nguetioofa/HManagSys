using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class UserSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CurrentHospitalCenterId { get; set; }

    public string SessionToken { get; set; } = null!;

    public DateTime LoginTime { get; set; }

    public DateTime? LogoutTime { get; set; }

    public string? IpAddress { get; set; }

    public bool IsActive { get; set; }

    public virtual HospitalCenter CurrentHospitalCenter { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
