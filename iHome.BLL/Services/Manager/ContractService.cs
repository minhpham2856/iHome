using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Manager contract lifecycle: list, create, update, delete, assign/remove tenants
	public class ContractService
	{
		// Mutating contract operations
		private readonly ManagerOperationsRepository _operationsRepository = new();
		// Read-only room/contract lookups
		private readonly ManagerReadRepository _readRepository = new();

		// Grid rows for contracts in manager-assigned buildings
		public List<ContractDto> GetContracts(int managerId, int? buildingId = null)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// Load contracts scoped to manager, map each to list DTO
			return _operationsRepository.GetContracts(managerId, buildingId)
				.Select(ToDto)
				.ToList();
		}

		// Combo: rooms available for new contract (excludes maintenance rooms)
		public List<LookupOptionDto> GetRoomOptions(int managerId, int? buildingId = null)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			return _readRepository.GetRooms(managerId, buildingId)
				// Maintenance rooms cannot receive new contracts
				.Where(room => !string.Equals(room.Status, RoomStatus.Maintenance, StringComparison.OrdinalIgnoreCase))
				.Select(room => new LookupOptionDto
				{
					Id = room.Id,
					BuildingId = room.BuildingId,
					PropertyId = room.Building.PropertyId,
					DisplayName = $"{room.Building.Name} - Phòng {room.RoomNumber}",
					// Pre-fill monthly rent from room type base rent
					SuggestedAmount = room.RoomType.BaseRent,
					// Truyền sức chứa để dialog bắt buộc đủ khách đứng tên khi tạo HĐ
					MaxOccupancy = room.RoomType.MaxOccupancy
				})
				.ToList();
		}

		// Combo: active contracts for invoice creation picker
		public List<ContractOptionDto> GetContractOptions(int managerId, int? buildingId = null) =>
			GetContracts(managerId, buildingId)
				// Only billable active contracts appear in invoice form
				.Where(contract => contract.Status == ContractStatus.Active)
				.Select(contract => new ContractOptionDto
				{
					Id = contract.Id,
					// Rich label: id + location + main tenant name
					DisplayName = $"HĐ #{contract.Id} - {contract.BuildingName} / {contract.RoomNumber} - {contract.MainTenantName}"
				})
				.ToList();

		// Load contract form for view/edit dialog
		public ContractFormDto GetContract(int managerId, int contractId)
		{
			// Contract must exist in manager's scoped set
			var contract = _operationsRepository.GetContracts(managerId)
				.SingleOrDefault(item => item.Id == contractId)
				?? throw new UnauthorizedAccessException("Bạn không có quyền xem hợp đồng này.");
			return new ContractFormDto
			{
				Id = contract.Id,
				RoomId = contract.RoomId,
				// Resolve main tenant id from ContractTenants link table
				MainTenantId = contract.ContractTenants.FirstOrDefault(ct => ct.IsMainTenant)?.TenantId ?? 0,
				StartDate = contract.StartDate,
				EndDate = contract.EndDate,
				MonthlyRent = contract.MonthlyRent,
				DepositAmount = contract.DepositAmount,
				Status = contract.Status,
				Notes = contract.Notes
			};
		}

		// Create new rental contract with main + co-tenants
		public int CreateContract(int managerId, ContractFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// Validate dates, rent, status — isCreate=true enforces room/tenant selection
			FormValidation.ValidateContract(input, true);
			// Truyền CoTenantIds để DAL kiểm tra đủ MaxOccupancy và lưu ContractTenants
			var contract = _operationsRepository.CreateContract(
				managerId,
				ToEntity(input, managerId),
				input.MainTenantId,
				input.CoTenantIds);
			return contract.Id;
		}

		// Update contract metadata (dates, rent, status, notes)
		public void UpdateContract(int managerId, ContractFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// isCreate=false — room/tenant not re-validated on edit
			FormValidation.ValidateContract(input, false);
			_operationsRepository.UpdateContract(managerId, ToEntity(input, managerId));
		}

		// Delete contract when business rules allow (no blocking invoices, etc.)
		public void DeleteContract(int managerId, int contractId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			_operationsRepository.DeleteContract(managerId, contractId);
		}

		// Add tenant to existing contract (main or co-tenant)
		public void AssignTenant(int managerId, int contractId, int tenantId, bool isMainTenant)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// Both ids required for link table insert
			if (contractId <= 0 || tenantId <= 0)
			{
				throw new ArgumentException("Vui lòng chọn hợp đồng và khách thuê.");
			}
			_operationsRepository.AssignTenant(managerId, contractId, tenantId, isMainTenant);
		}

		// Remove tenant from contract — DAL prevents removing last main tenant
		public void RemoveTenant(int managerId, int contractId, int tenantId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			_operationsRepository.RemoveTenant(managerId, contractId, tenantId);
		}

		// Map form → Contract entity for DAL persistence
		private static Contract ToEntity(ContractFormDto input, int managerId) => new()
		{
			Id = input.Id,
			RoomId = input.RoomId,
			StartDate = input.StartDate,
			EndDate = input.EndDate,
			MonthlyRent = input.MonthlyRent,
			DepositAmount = input.DepositAmount,
			Status = input.Status,
			Notes = input.Notes,
			// Audit: which manager created the contract
			CreatedBy = managerId,
			CreatedAt = DateTime.Now
		};

		// Map Contract entity → grid/list DTO with tenant summary
		private static ContractDto ToDto(Contract contract)
		{
			// Order tenants: main first, then alphabetically by name
			var tenants = contract.ContractTenants.OrderByDescending(ct => ct.IsMainTenant).ToList();
			// Hiển thị đủ tên: người chính gắn nhãn, các khách còn lại liệt kê sau
			string tenantNames = string.Join(", ", tenants.Select(ct =>
				ct.IsMainTenant
					? $"{ct.Tenant.FullName} (chính)"
					: ct.Tenant.FullName));
			return new ContractDto
			{
				Id = contract.Id,
				RoomId = contract.RoomId,
				BuildingName = contract.Room.Building.Name,
				RoomNumber = contract.Room.RoomNumber,
				StartDate = contract.StartDate,
				EndDate = contract.EndDate,
				MonthlyRent = contract.MonthlyRent,
				DepositAmount = contract.DepositAmount,
				Status = contract.Status,
				StatusDisplay = DisplayFormatter.FormatContractStatus(contract.Status, contract.EndDate),
				MainTenantName = tenants.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant.FullName ?? "Chưa có",
				TenantNames = string.IsNullOrWhiteSpace(tenantNames) ? "Chưa có" : tenantNames,
				TenantCount = tenants.Count,
				Notes = contract.Notes
			};
		}
	}
}
