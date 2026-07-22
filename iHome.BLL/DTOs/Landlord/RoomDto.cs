using iHome.BLL.Enums;
using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
	// Room type definition row for the landlord room-type management grid.
	public class RoomTypeDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string TypeName { get; set; } = string.Empty;
		public int MaxOccupancy { get; set; }
		public decimal? Area { get; set; }
		public decimal BaseRent { get; set; }
		public string? Description { get; set; }
		public int RoomCount { get; set; }
	}

	// Editable room type fields for create/edit room-type dialogs.
	public class RoomTypeFormDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string TypeName { get; set; } = string.Empty;
		public int MaxOccupancy { get; set; } = 1;
		public decimal? Area { get; set; }
		public decimal BaseRent { get; set; }
		public string? Description { get; set; }
	}

	// Room row for the landlord room list grid within a building.
	public class RoomDto
	{
		public int Id { get; set; }
		public int BuildingId { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public int RoomTypeId { get; set; }
		public string RoomTypeName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public int Floor { get; set; }
		public string Status { get; set; } = string.Empty;
		public string StatusDisplay { get; set; } = string.Empty;
		public int CurrentOccupancy { get; set; }
		public int MaxOccupancy { get; set; }
		public string? Notes { get; set; }
	}

	// Editable room fields for create/edit room dialogs.
	public class RoomFormDto
	{
		public int Id { get; set; }
		public int BuildingId { get; set; }
		public int RoomTypeId { get; set; }
		public string RoomNumber { get; set; } = string.Empty;
		public int Floor { get; set; } = 1;
		public string Status { get; set; } = RoomStatus.Empty;
		public string? Notes { get; set; }
	}

	// Expanded room view with location context and current tenant list.
	public class RoomDetailDto
	{
		public int Id { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public string PropertyName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public string RoomTypeName { get; set; } = string.Empty;
		public int Floor { get; set; }
		public decimal? Area { get; set; }
		public decimal BaseRent { get; set; }
		public string Status { get; set; } = string.Empty;
		public string StatusDisplay { get; set; } = string.Empty;
		public int MaxOccupancy { get; set; }
		public string? Notes { get; set; }
		public List<RoomTenantDto> Tenants { get; set; } = new();
	}

	// Tenant currently occupying a room, shown on the room detail panel.
	public class RoomTenantDto
	{
		public int TenantId { get; set; }
		public string FullName { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string IdCardNumber { get; set; } = string.Empty;
		public string? Email { get; set; }
		public bool IsMainTenant { get; set; }
		public string RoleDisplay => IsMainTenant ? "Người thuê chính" : "Thành viên";
	}

	// Room type entry for room create/edit type dropdown.
	public class RoomTypeOptionDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public decimal BaseRent { get; set; }
		public decimal? Area { get; set; }
	}

	// Building entry for room list filter dropdowns.
	public class BuildingFilterOptionDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string Name { get; set; } = string.Empty;
		public int NumberOfFloors { get; set; }
	}

	// Property entry for room/building filter dropdowns.
	public class PropertyFilterOptionDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}
}
