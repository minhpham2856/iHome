using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
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

	public class RoomDto
	{
		public int Id { get; set; }
		public int BuildingId { get; set; }
		public int RoomTypeId { get; set; }
		public string RoomTypeName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public int Floor { get; set; }
		public decimal? Area { get; set; }
		public decimal BaseRent { get; set; }
		public string Status { get; set; } = string.Empty;
		public string StatusDisplay { get; set; } = string.Empty;
		public int CurrentOccupancy { get; set; }
		public int MaxOccupancy { get; set; }
		public string? Notes { get; set; }
	}

	public class RoomFormDto
	{
		public int Id { get; set; }
		public int BuildingId { get; set; }
		public int RoomTypeId { get; set; }
		public string RoomNumber { get; set; } = string.Empty;
		public int Floor { get; set; } = 1;
		public string Status { get; set; } = "Empty";
		public string? Notes { get; set; }
	}

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

	public class RoomTypeOptionDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public decimal BaseRent { get; set; }
		public decimal? Area { get; set; }
	}

	public class BuildingFilterOptionDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string Name { get; set; } = string.Empty;
		public int NumberOfFloors { get; set; }
	}

	public class PropertyFilterOptionDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}
}
