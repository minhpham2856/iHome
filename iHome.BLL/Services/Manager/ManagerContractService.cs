using iHome.BLL.DTOs;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerContractService
	{
		private readonly ManagerOperationsRepository _operationsRepository = new();
		private readonly ManagerReadRepository _readRepository = new();

		public List<ManagerContractDto> GetContracts(int managerId, int? buildingId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			return _operationsRepository.GetContracts(managerId, buildingId)
				.Select(ToDto)
				.ToList();
		}

		public List<ManagerLookupOptionDto> GetRoomOptions(int managerId, int? buildingId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			return _readRepository.GetRooms(managerId, buildingId)
				.Where(room => !string.Equals(room.Status, "Maintenance", StringComparison.OrdinalIgnoreCase))
				.Select(room => new ManagerLookupOptionDto
				{
					Id = room.Id,
					BuildingId = room.BuildingId,
					PropertyId = room.Building.PropertyId,
					DisplayName = $"{room.Building.Name} - Phòng {room.RoomNumber}",
					SuggestedAmount = room.RoomType.BaseRent,
					// Truyền sức chứa để dialog bắt buộc đủ khách đứng tên khi tạo HĐ
					MaxOccupancy = room.RoomType.MaxOccupancy
				})
				.ToList();
		}

		public List<ManagerContractOptionDto> GetContractOptions(int managerId, int? buildingId = null) =>
			GetContracts(managerId, buildingId)
				.Where(contract => contract.Status == "Active")
				.Select(contract => new ManagerContractOptionDto
				{
					Id = contract.Id,
					DisplayName = $"HĐ #{contract.Id} - {contract.BuildingName} / {contract.RoomNumber} - {contract.MainTenantName}"
				})
				.ToList();

		public ManagerContractFormDto GetContract(int managerId, int contractId)
		{
			var contract = _operationsRepository.GetContracts(managerId)
				.SingleOrDefault(item => item.Id == contractId)
				?? throw new UnauthorizedAccessException("Bạn không có quyền xem hợp đồng này.");
			return new ManagerContractFormDto
			{
				Id = contract.Id,
				RoomId = contract.RoomId,
				MainTenantId = contract.ContractTenants.FirstOrDefault(ct => ct.IsMainTenant)?.TenantId ?? 0,
				StartDate = contract.StartDate,
				EndDate = contract.EndDate,
				MonthlyRent = contract.MonthlyRent,
				DepositAmount = contract.DepositAmount,
				Status = contract.Status,
				Notes = contract.Notes
			};
		}

		public int CreateContract(int managerId, ManagerContractFormDto input)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			ManagerValidation.ValidateContract(input, true);
			// Truyền CoTenantIds để DAL kiểm tra đủ MaxOccupancy và lưu ContractTenants
			var contract = _operationsRepository.CreateContract(
				managerId,
				ToEntity(input, managerId),
				input.MainTenantId,
				input.CoTenantIds);
			return contract.Id;
		}

		public void UpdateContract(int managerId, ManagerContractFormDto input)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			ManagerValidation.ValidateContract(input, false);
			_operationsRepository.UpdateContract(managerId, ToEntity(input, managerId));
		}

		public void DeleteContract(int managerId, int contractId)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			_operationsRepository.DeleteContract(managerId, contractId);
		}

		public void AssignTenant(int managerId, int contractId, int tenantId, bool isMainTenant)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			if (contractId <= 0 || tenantId <= 0)
			{
				throw new ArgumentException("Vui lòng chọn hợp đồng và khách thuê.");
			}
			_operationsRepository.AssignTenant(managerId, contractId, tenantId, isMainTenant);
		}

		public void RemoveTenant(int managerId, int contractId, int tenantId)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			_operationsRepository.RemoveTenant(managerId, contractId, tenantId);
		}

		private static Contract ToEntity(ManagerContractFormDto input, int managerId) => new()
		{
			Id = input.Id,
			RoomId = input.RoomId,
			StartDate = input.StartDate,
			EndDate = input.EndDate,
			MonthlyRent = input.MonthlyRent,
			DepositAmount = input.DepositAmount,
			Status = input.Status,
			Notes = input.Notes,
			CreatedBy = managerId,
			CreatedAt = DateTime.Now
		};

		private static ManagerContractDto ToDto(Contract contract)
		{
			var tenants = contract.ContractTenants.OrderByDescending(ct => ct.IsMainTenant).ToList();
			// Hiển thị đủ tên: người chính gắn nhãn, các khách còn lại liệt kê sau
			string tenantNames = string.Join(", ", tenants.Select(ct =>
				ct.IsMainTenant
					? $"{ct.Tenant.FullName} (chính)"
					: ct.Tenant.FullName));
			return new ManagerContractDto
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
				StatusDisplay = ManagerDisplayFormatter.FormatContractStatus(contract.Status, contract.EndDate),
				MainTenantName = tenants.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant.FullName ?? "Chưa có",
				TenantNames = string.IsNullOrWhiteSpace(tenantNames) ? "Chưa có" : tenantNames,
				TenantCount = tenants.Count,
				Notes = contract.Notes
			};
		}
	}
}
