using HManagSys.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace HManagSys.Models.EfModels;

public partial class ProductCategory : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
