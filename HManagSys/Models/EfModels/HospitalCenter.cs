using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class HospitalCenter : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CareEpisode> CareEpisodes { get; set; } = new List<CareEpisode>();

    public virtual ICollection<CashHandover> CashHandovers { get; set; } = new List<CashHandover>();

    public virtual ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();

    public virtual ICollection<Examination> Examinations { get; set; } = new List<Examination>();

    public virtual ICollection<Financier> Financiers { get; set; } = new List<Financier>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public virtual ICollection<StockInventory> StockInventories { get; set; } = new List<StockInventory>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<StockTransfer> StockTransferFromHospitalCenters { get; set; } = new List<StockTransfer>();

    public virtual ICollection<StockTransfer> StockTransferToHospitalCenters { get; set; } = new List<StockTransfer>();

    public virtual ICollection<UserCenterAssignment> UserCenterAssignments { get; set; } = new List<UserCenterAssignment>();

    public virtual ICollection<UserLastSelectedCenter> UserLastSelectedCenters { get; set; } = new List<UserLastSelectedCenter>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
