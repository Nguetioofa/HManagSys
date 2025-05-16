using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class CareService : IEntity
{
    public int Id { get; set; }

    public int CareEpisodeId { get; set; }

    public int CareTypeId { get; set; }

    public int AdministeredBy { get; set; }

    public DateTime ServiceDate { get; set; }

    public int? Duration { get; set; }

    public string? Notes { get; set; }

    public decimal Cost { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual User AdministeredByNavigation { get; set; } = null!;

    public virtual CareEpisode CareEpisode { get; set; } = null!;

    public virtual ICollection<CareServiceProduct> CareServiceProducts { get; set; } = new List<CareServiceProduct>();

    public virtual CareType CareType { get; set; } = null!;
}
