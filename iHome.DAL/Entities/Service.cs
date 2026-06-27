using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class Service
{
    public int Id { get; set; }

    public int BuildingId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public string CalculationMethod { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Building Building { get; set; } = null!;

    public virtual ICollection<RoomService> RoomServices { get; set; } = new List<RoomService>();
}
