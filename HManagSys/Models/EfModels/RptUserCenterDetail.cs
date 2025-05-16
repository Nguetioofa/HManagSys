using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class RptUserCenterDetail : IEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public bool UserIsActive { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public int? AssignmentId { get; set; }

    public string? RoleType { get; set; }

    public bool? AssignmentIsActive { get; set; }

    public int? HospitalCenterId { get; set; }

    public string? HospitalCenterName { get; set; }

    public DateTime? AssignmentStartDate { get; set; }

    public DateTime? AssignmentEndDate { get; set; }

    public DateTime ReportGeneratedAt { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
}
