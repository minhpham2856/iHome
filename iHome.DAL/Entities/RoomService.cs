using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class RoomService
{
    public int RoomId { get; set; }

    public int ServiceId { get; set; }

    public bool IsActive { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;

    public virtual ICollection<ServiceReading> ServiceReadings { get; set; } = new List<ServiceReading>();
}
