using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class ExaminationResult : IEntity
{
    public int Id { get; set; }

    public int ExaminationId { get; set; }

    public string? ResultData { get; set; }

    public string? ResultNotes { get; set; }

    public string? AttachmentPath { get; set; }

    public int ReportedBy { get; set; }

    public DateTime ReportDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Examination Examination { get; set; } = null!;

    public virtual User ReportedByNavigation { get; set; } = null!;
}
