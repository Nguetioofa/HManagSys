using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class UserLastSelectedCenter
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int LastSelectedHospitalCenterId { get; set; }

    public DateTime LastSelectionDate { get; set; }

    public virtual HospitalCenter LastSelectedHospitalCenter { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
