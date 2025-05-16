using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class UserCenterAssignment : IEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int HospitalCenterId { get; set; }

    public string RoleType { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime AssignmentStartDate { get; set; }

    public DateTime? AssignmentEndDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual HospitalCenter HospitalCenter { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
