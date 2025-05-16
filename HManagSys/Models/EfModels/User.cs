using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class User : IEntity
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public bool MustChangePassword { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CareEpisode> CareEpisodes { get; set; } = new List<CareEpisode>();

    public virtual ICollection<CareService> CareServices { get; set; } = new List<CareService>();

    public virtual ICollection<CashHandover> CashHandovers { get; set; } = new List<CashHandover>();

    public virtual ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();

    public virtual ICollection<Examination> ExaminationPerformedByNavigations { get; set; } = new List<Examination>();

    public virtual ICollection<Examination> ExaminationRequestedByNavigations { get; set; } = new List<Examination>();

    public virtual ICollection<ExaminationResult> ExaminationResults { get; set; } = new List<ExaminationResult>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public virtual ICollection<StockTransfer> StockTransfers { get; set; } = new List<StockTransfer>();

    public virtual ICollection<UserCenterAssignment> UserCenterAssignments { get; set; } = new List<UserCenterAssignment>();

    public virtual UserLastSelectedCenter? UserLastSelectedCenter { get; set; }

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
