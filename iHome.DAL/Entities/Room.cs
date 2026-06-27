using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class Room
{
    public int Id { get; set; }

    public int BuildingId { get; set; }

    public int RoomTypeId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int Floor { get; set; }

    public decimal? Area { get; set; }

    public decimal BaseRent { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Building Building { get; set; } = null!;

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<RoomService> RoomServices { get; set; } = new List<RoomService>();

    public virtual RoomType RoomType { get; set; } = null!;
}
