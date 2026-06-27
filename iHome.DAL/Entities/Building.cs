using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class Building
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public int? ManagerId { get; set; }

    public string Name { get; set; } = null!;

    public int NumberOfFloors { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<ElectricityTier> ElectricityTiers { get; set; } = new List<ElectricityTier>();

    public virtual User? Manager { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
