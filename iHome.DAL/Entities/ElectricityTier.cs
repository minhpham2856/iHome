using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class ElectricityTier
{
    public int Id { get; set; }

    public int BuildingId { get; set; }

    public int MinUnit { get; set; }

    public int? MaxUnit { get; set; }

    public decimal PricePerUnit { get; set; }

    public virtual Building Building { get; set; } = null!;
}
