using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class Property
{
    public int Id { get; set; }

    public int LandlordId { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Building> Buildings { get; set; } = new List<Building>();

    public virtual User Landlord { get; set; } = null!;
}
