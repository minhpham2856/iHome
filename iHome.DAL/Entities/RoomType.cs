using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class RoomType
{
    public int Id { get; set; }

    public int BuildingId { get; set; }

    public string TypeName { get; set; } = null!;

    public int MaxOccupancy { get; set; }

    public string? Description { get; set; }

    public virtual Building Building { get; set; } = null!;

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
