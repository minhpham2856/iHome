using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class ServiceReading
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public int ServiceId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal? PreviousReading { get; set; }

    public decimal CurrentReading { get; set; }

    public decimal? Consumption { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual RoomService RoomService { get; set; } = null!;
}
