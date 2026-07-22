using iHome.BLL.Enums;
using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
	// Room type definition row for the landlord room-type management grid.
	public class RoomTypeDto
	{
		// Room type primary key.
		public int Id { get; set; }
		// Property this room type belongs to.
		public int PropertyId { get; set; }
		// Category name (e.g. single, double) shown in lists.
		public string TypeName { get; set; } = string.Empty;
		// Maximum tenants allowed per room of this type.
		public int MaxOccupancy { get; set; }
		// Typical floor area in square meters; null when not specified.
		public decimal? Area { get; set; }
		// Default monthly rent for rooms of this type.
		public decimal BaseRent { get; set; }
		// Optional description of amenities or layout.
		public string? Description { get; set; }
		// Number of physical rooms using this type (denormalized for the grid).
		public int RoomCount { get; set; }
	}

	// Editable room type fields for create/edit room-type dialogs.
	public class RoomTypeFormDto
	{
		// Room type primary key; zero when creating.
		public int Id { get; set; }
		// Property the room type will belong to.
		public int PropertyId { get; set; }
		// Type name from the form.
		public string TypeName { get; set; } = string.Empty;
		// Max occupancy from the form; defaults to one.
		public int MaxOccupancy { get; set; } = 1;
		// Optional area from the form.
		public decimal? Area { get; set; }
		// Base rent from the form.
		public decimal BaseRent { get; set; }
		// Optional description from the form.
		public string? Description { get; set; }
	}

	// Room row for the landlord room list grid within a building.
	public class RoomDto
	{
		// Room primary key.
		public int Id { get; set; }
		// Building containing this room.
		public int BuildingId { get; set; }
		// Room type Id defining capacity and default rent.
		public int RoomTypeId { get; set; }
		// Room type name for display.
		public string RoomTypeName { get; set; } = string.Empty;
		// Room number or label (e.g. "101", "A2").
		public string RoomNumber { get; set; } = string.Empty;
		// Floor level within the building.
		public int Floor { get; set; }
		// Actual room area; may override the type default.
		public decimal? Area { get; set; }
		// Monthly base rent for this room.
		public decimal BaseRent { get; set; }
		// Raw status code (Empty, Occupied, Maintenance, …).
		public string Status { get; set; } = string.Empty;
		// Vietnamese status label for grid binding.
		public string StatusDisplay { get; set; } = string.Empty;
		// Current number of tenants in the room.
		public int CurrentOccupancy { get; set; }
		// Maximum allowed occupants from the room type.
		public int MaxOccupancy { get; set; }
		// Optional internal notes about the room.
		public string? Notes { get; set; }
	}

	// Editable room fields for create/edit room dialogs.
	public class RoomFormDto
	{
		// Room primary key; zero when creating.
		public int Id { get; set; }
		// Building the room belongs to.
		public int BuildingId { get; set; }
		// Selected room type on the form.
		public int RoomTypeId { get; set; }
		// Room number entered on the form.
		public string RoomNumber { get; set; } = string.Empty;
		// Floor level; defaults to ground/first floor.
		public int Floor { get; set; } = 1;
		// Status selected on the form; defaults to vacant.
		public string Status { get; set; } = RoomStatus.Empty;
		// Optional notes from the form.
		public string? Notes { get; set; }
	}

	// Expanded room view with location context and current tenant list.
	public class RoomDetailDto
	{
		// Room primary key.
		public int Id { get; set; }
		// Building name for the detail header.
		public string BuildingName { get; set; } = string.Empty;
		// Property name for the detail header.
		public string PropertyName { get; set; } = string.Empty;
		// Room number.
		public string RoomNumber { get; set; } = string.Empty;
		// Room type name.
		public string RoomTypeName { get; set; } = string.Empty;
		// Floor level.
		public int Floor { get; set; }
		// Room area in square meters.
		public decimal? Area { get; set; }
		// Monthly base rent.
		public decimal BaseRent { get; set; }
		// Raw status code.
		public string Status { get; set; } = string.Empty;
		// Vietnamese status label.
		public string StatusDisplay { get; set; } = string.Empty;
		// Maximum occupants allowed.
		public int MaxOccupancy { get; set; }
		// Internal notes.
		public string? Notes { get; set; }
		// Tenants currently linked to an active contract for this room.
		public List<RoomTenantDto> Tenants { get; set; } = new();
	}

	// Tenant currently occupying a room, shown on the room detail panel.
	public class RoomTenantDto
	{
		// Tenant primary key.
		public int TenantId { get; set; }
		// Tenant full name.
		public string FullName { get; set; } = string.Empty;
		// Contact phone number.
		public string PhoneNumber { get; set; } = string.Empty;
		// National ID / CCCD number.
		public string IdCardNumber { get; set; } = string.Empty;
		// Optional email address.
		public string? Email { get; set; }
		// True when this tenant is the primary leaseholder.
		public bool IsMainTenant { get; set; }
		// Vietnamese role label (main tenant vs member) for the tenant list.
		public string RoleDisplay => IsMainTenant ? "Người thuê chính" : "Thành viên";
	}

	// Room type entry for room create/edit type dropdown.
	public class RoomTypeOptionDto
	{
		// Room type primary key.
		public int Id { get; set; }
		// Type name shown in the ComboBox.
		public string Name { get; set; } = string.Empty;
		// Default base rent inherited when this type is selected.
		public decimal BaseRent { get; set; }
		// Default area inherited when this type is selected.
		public decimal? Area { get; set; }
	}

	// Building entry for room list filter dropdowns.
	public class BuildingFilterOptionDto
	{
		// Building primary key.
		public int Id { get; set; }
		// Parent property for cascading filters.
		public int PropertyId { get; set; }
		// Building name shown in the filter ComboBox.
		public string Name { get; set; } = string.Empty;
		// Floor count used to validate room floor input.
		public int NumberOfFloors { get; set; }
	}

	// Property entry for room/building filter dropdowns.
	public class PropertyFilterOptionDto
	{
		// Property primary key.
		public int Id { get; set; }
		// Property name shown in the filter ComboBox.
		public string Name { get; set; } = string.Empty;
	}
}
