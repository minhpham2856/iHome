using iHome.BLL.DTOs.Manager;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// CRUD tenants in Manager scope — includes unassigned tenants created by this manager
	public class TenantService
	{
		// Read-only queries for contract-linked tenant rows
		private readonly ManagerReadRepository _readRepository = new();
		// Create/update/delete tenant operations
		private readonly ManagerOperationsRepository _operationsRepository = new();

		// Grid: tenants on active contracts + unassigned tenants when not filtering by building
		public List<TenantDto> GetTenants(int managerId, int? buildingId = null)
		{
			// Reject invalid manager session id
			ServiceGuard.EnsureValidManagerId(managerId);
			// Map ContractTenant rows to flat TenantDto for data grid
			var rows = _readRepository.GetContractTenants(managerId, buildingId)
				.Select(ct => new TenantDto
				{
					// Tenant primary key
					TenantId = ct.TenantId,
					// Personal info from Tenant entity
					FullName = ct.Tenant.FullName,
					DateOfBirth = ct.Tenant.DateOfBirth,
					IdCardNumber = ct.Tenant.IdCardNumber,
					PhoneNumber = ct.Tenant.PhoneNumber,
					Email = ct.Tenant.Email,
					PermanentAddress = ct.Tenant.PermanentAddress,
					// Location from contract's room/building
					BuildingId = ct.Contract.Room.BuildingId,
					BuildingName = ct.Contract.Room.Building.Name,
					RoomId = ct.Contract.RoomId,
					RoomNumber = ct.Contract.Room.RoomNumber,
					ContractId = ct.ContractId,
					// Main vs co-tenant role on this contract
					IsMainTenant = ct.IsMainTenant,
					TenantRoleDisplay = ct.IsMainTenant ? "Người thuê chính" : "Người ở cùng",
					// Contract term dates
					StartDate = ct.Contract.StartDate,
					EndDate = ct.Contract.EndDate,
					// Raw DB contract status
					ContractStatus = ct.Contract.Status,
					// Display status with ExpiringSoon/Expired derivation
					ContractStatusDisplay = DisplayFormatter.FormatContractStatus(
						ct.Contract.Status,
						ct.Contract.EndDate)
				})
				.ToList();

			// Khách do manager tạo nhưng chưa gắn HĐ — chỉ hiện khi không lọc theo tòa
			if (!buildingId.HasValue)
			{
				// Track tenant ids already shown via contract links — avoid duplicates
				HashSet<int> linkedIds = rows.Select(row => row.TenantId).ToHashSet();
				// Append unassigned tenants created by this manager (not yet on any contract)
				rows.AddRange(_operationsRepository.GetUnassignedManagerTenants(managerId)
					.Where(tenant => !linkedIds.Contains(tenant.Id))
					.Select(tenant => new TenantDto
					{
						TenantId = tenant.Id,
						FullName = tenant.FullName,
						DateOfBirth = tenant.DateOfBirth,
						IdCardNumber = tenant.IdCardNumber,
						PhoneNumber = tenant.PhoneNumber,
						Email = tenant.Email,
						PermanentAddress = tenant.PermanentAddress,
						// Placeholder location — no contract yet
						BuildingName = "Chưa gán",
						RoomNumber = "-",
						TenantRoleDisplay = "Chưa gán",
						ContractStatusDisplay = "Chưa có hợp đồng"
					}));
			}

			// Combined list: contracted + unassigned tenants
			return rows;
		}

		// Combo: all tenants this manager may put on a new contract
		public List<LookupOptionDto> GetTenantOptions(int managerId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			return _operationsRepository.GetManageableTenants(managerId)
				.Select(tenant => new LookupOptionDto
				{
					Id = tenant.Id,
					// Disambiguate by full name + CCCD in picker
					DisplayName = $"{tenant.FullName} - {tenant.IdCardNumber}"
				})
				.ToList();
		}

		// Load single tenant form — throws if tenant outside manager scope
		public TenantFormDto GetTenant(int managerId, int tenantId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// Single tenant must appear in manageable set for this manager
			var tenant = _operationsRepository.GetManageableTenants(managerId)
				.SingleOrDefault(item => item.Id == tenantId)
				?? throw new UnauthorizedAccessException("Bạn không có quyền xem khách thuê này.");
			return ToForm(tenant);
		}

		// Insert new tenant record owned by this manager
		public int CreateTenant(int managerId, TenantFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// Normalize + validate form fields (throws ArgumentException on failure)
			FormValidation.ValidateTenant(input);
			// CCCD must be globally unique across all tenants
			if (_operationsRepository.IdCardExists(input.IdCardNumber))
			{
				throw new InvalidOperationException("CCCD đã tồn tại trong hệ thống.");
			}
			// Persist entity — repository stamps CreatedBy manager linkage
			var tenant = _operationsRepository.CreateTenant(managerId, ToEntity(input));
			return tenant.Id;
		}

		// Update existing tenant personal info
		public void UpdateTenant(int managerId, TenantFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			FormValidation.ValidateTenant(input);
			// CCCD unique check excludes current tenant id on update
			if (_operationsRepository.IdCardExists(input.IdCardNumber, input.Id))
			{
				throw new InvalidOperationException("CCCD đã tồn tại trong hệ thống.");
			}
			_operationsRepository.UpdateTenant(managerId, ToEntity(input));
		}

		// Delete tenant when no contract references block removal
		public void DeleteTenant(int managerId, int tenantId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// DAL enforces: no active contracts, manager ownership
			_operationsRepository.DeleteTenant(managerId, tenantId);
		}

		// Map form DTO → Tenant entity for insert/update
		private static Tenant ToEntity(TenantFormDto input) => new()
		{
			Id = input.Id,
			FullName = input.FullName,
			DateOfBirth = input.DateOfBirth,
			IdCardNumber = input.IdCardNumber,
			PhoneNumber = input.PhoneNumber,
			Email = input.Email,
			PermanentAddress = input.PermanentAddress,
			CreatedAt = DateTime.Now
		};

		// Map Tenant entity → form DTO for edit dialog
		private static TenantFormDto ToForm(Tenant tenant) => new()
		{
			Id = tenant.Id,
			FullName = tenant.FullName,
			DateOfBirth = tenant.DateOfBirth,
			IdCardNumber = tenant.IdCardNumber,
			PhoneNumber = tenant.PhoneNumber,
			Email = tenant.Email,
			PermanentAddress = tenant.PermanentAddress
		};
	}
}
