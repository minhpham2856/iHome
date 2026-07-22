using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Vòng đời hợp đồng Manager: danh sách, tạo/sửa/xóa, gán/gỡ khách
	public class ContractService
	{
		private readonly ManagerOperationsRepository _operationsRepository = new();
		private readonly ManagerReadRepository _readRepository = new();

		// Lưới hợp đồng trong các tòa được phân công
		public List<ContractDto> GetContracts(int managerId, int? buildingId = null)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			return _operationsRepository.GetContracts(managerId, buildingId)
				.Select(ToDto)
				.ToList();
		}

		// Combo phòng cho HĐ mới — loại phòng Bảo trì; kèm MaxOccupancy cho chọn khách phụ
		public List<LookupOptionDto> GetRoomOptions(int managerId, int? buildingId = null)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			return _readRepository.GetRooms(managerId, buildingId)
				.Where(room => !string.Equals(room.Status, RoomStatus.Maintenance, StringComparison.OrdinalIgnoreCase))
				.Select(room => new LookupOptionDto
				{
					Id = room.Id,
					BuildingId = room.BuildingId,
					PropertyId = room.Building.PropertyId,
					DisplayName = $"{room.Building.Name} - Phòng {room.RoomNumber}",
					SuggestedAmount = room.RoomType.BaseRent,
					// Trần sức chứa (MaxOccupancy) — UI giới hạn số khách phụ khi tạo HĐ
					MaxOccupancy = room.RoomType.MaxOccupancy
				})
				.ToList();
		}

		// Combo HĐ đang hoạt động cho form lập hóa đơn
		public List<ContractOptionDto> GetContractOptions(int managerId, int? buildingId = null) =>
			GetContracts(managerId, buildingId)
				.Where(contract => contract.Status == ContractStatus.Active)
				.Select(contract => new ContractOptionDto
				{
					Id = contract.Id,
					DisplayName = $"HĐ #{contract.Id} - {contract.BuildingName} / {contract.RoomNumber} - {contract.MainTenantName}"
				})
				.ToList();

		// Form xem/sửa — lấy MainTenantId từ ContractTenants
		public ContractFormDto GetContract(int managerId, int contractId)
		{
			var contract = _operationsRepository.GetContracts(managerId)
				.SingleOrDefault(item => item.Id == contractId)
				?? throw new UnauthorizedAccessException("Bạn không có quyền xem hợp đồng này.");
			return new ContractFormDto
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

		// Tạo HĐ mới với khách chính + khách phụ
		public int CreateContract(int managerId, ContractFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			FormValidation.ValidateContract(input, true);
			var contract = _operationsRepository.CreateContract(
				managerId,
				ToEntity(input, managerId),
				input.MainTenantId,
				input.CoTenantIds);
			return contract.Id;
		}

		// Cập nhật metadata HĐ (ngày, tiền, trạng thái, ghi chú)
		public void UpdateContract(int managerId, ContractFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			FormValidation.ValidateContract(input, false);
			_operationsRepository.UpdateContract(managerId, ToEntity(input, managerId));
		}

		public void DeleteContract(int managerId, int contractId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			_operationsRepository.DeleteContract(managerId, contractId);
		}

		// Gán khách vào HĐ hiện có (chính hoặc phụ)
		public void AssignTenant(int managerId, int contractId, int tenantId, bool isMainTenant)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			if (contractId <= 0 || tenantId <= 0)
			{
				throw new ArgumentException("Vui lòng chọn hợp đồng và khách thuê.");
			}
			_operationsRepository.AssignTenant(managerId, contractId, tenantId, isMainTenant);
		}

		// Gỡ khách — DAL chặn gỡ khách chính cuối cùng
		public void RemoveTenant(int managerId, int contractId, int tenantId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			_operationsRepository.RemoveTenant(managerId, contractId, tenantId);
		}

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
			CreatedBy = managerId,
			CreatedAt = DateTime.Now
		};

		private static ContractDto ToDto(Contract contract)
		{
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
