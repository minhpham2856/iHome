using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class ServiceReading
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public int ServiceId { get; set; }

    public DateTime ReadingDate { get; set; }

    public decimal CurrentReading { get; set; }

    public virtual RoomService RoomService { get; set; } = null!;
}
