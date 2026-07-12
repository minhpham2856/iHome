using iHome.BLL.DTOs;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerTenantService
	{
		private readonly ManagerReadRepository _readRepository = new();
		private readonly ManagerOperationsRepository _operationsRepository = new();

		public List<ManagerTenantDto> GetTenants(int managerId, int? propertyId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);

			var rows = _readRepository.GetContractTenants(managerId, propertyId)
				.Select(ct => new ManagerTenantDto
				{
					TenantId = ct.TenantId,
					FullName = ct.Tenant.FullName,
					DateOfBirth = ct.Tenant.DateOfBirth,
					IdCardNumber = ct.Tenant.IdCardNumber,
					PhoneNumber = ct.Tenant.PhoneNumber,
					Email = ct.Tenant.Email,
					PermanentAddress = ct.Tenant.PermanentAddress,
					BuildingId = ct.Contract.Room.BuildingId,
					BuildingName = ct.Contract.Room.Building.Name,
					RoomId = ct.Contract.RoomId,
					RoomNumber = ct.Contract.Room.RoomNumber,
					ContractId = ct.ContractId,
					IsMainTenant = ct.IsMainTenant,
					TenantRoleDisplay = ct.IsMainTenant ? "Người thuê chính" : "Người ở cùng",
					StartDate = ct.Contract.StartDate,
					EndDate = ct.Contract.EndDate,
					ContractStatus = ct.Contract.Status,
					ContractStatusDisplay = ManagerDisplayFormatter.FormatContractStatus(ct.Contract.Status)
				})
				.ToList();

			if (!propertyId.HasValue)
			{
				var linkedIds = rows.Select(row => row.TenantId).ToHashSet();
				rows.AddRange(_operationsRepository.GetUnassignedManagerTenants(managerId)
					.Where(tenant => !linkedIds.Contains(tenant.Id))
					.Select(tenant => new ManagerTenantDto
					{
						TenantId = tenant.Id,
						FullName = tenant.FullName,
						DateOfBirth = tenant.DateOfBirth,
						IdCardNumber = tenant.IdCardNumber,
						PhoneNumber = tenant.PhoneNumber,
						Email = tenant.Email,
						PermanentAddress = tenant.PermanentAddress,
						BuildingName = "Chưa gán",
						RoomNumber = "—",
						TenantRoleDisplay = "Chưa gán",
						ContractStatusDisplay = "Chưa có hợp đồng"
					}));
			}

			return rows;
		}

		public List<ManagerLookupOptionDto> GetTenantOptions(int managerId)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			return _operationsRepository.GetManageableTenants(managerId)
				.Select(tenant => new ManagerLookupOptionDto
				{
					Id = tenant.Id,
					DisplayName = $"{tenant.FullName} - {tenant.IdCardNumber}"
				})
				.ToList();
		}

		public ManagerTenantFormDto GetTenant(int managerId, int tenantId)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			var tenant = _operationsRepository.GetManageableTenants(managerId)
				.SingleOrDefault(item => item.Id == tenantId)
				?? throw new UnauthorizedAccessException("Bạn không có quyền xem khách thuê này.");
			return ToForm(tenant);
		}

		public int CreateTenant(int managerId, ManagerTenantFormDto input)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			ManagerValidation.ValidateTenant(input);
			if (_operationsRepository.IdCardExists(input.IdCardNumber))
			{
				throw new InvalidOperationException("CCCD đã tồn tại trong hệ thống.");
			}

			var tenant = _operationsRepository.CreateTenant(managerId, ToEntity(input));
			return tenant.Id;
		}

		public void UpdateTenant(int managerId, ManagerTenantFormDto input)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			ManagerValidation.ValidateTenant(input);
			if (_operationsRepository.IdCardExists(input.IdCardNumber, input.Id))
			{
				throw new InvalidOperationException("CCCD đã tồn tại trong hệ thống.");
			}
			_operationsRepository.UpdateTenant(managerId, ToEntity(input));
		}

		public void DeleteTenant(int managerId, int tenantId)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			_operationsRepository.DeleteTenant(managerId, tenantId);
		}

		private static Tenant ToEntity(ManagerTenantFormDto input) => new()
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

		private static ManagerTenantFormDto ToForm(Tenant tenant) => new()
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
