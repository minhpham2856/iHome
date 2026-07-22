using iHome.BLL.Common;
using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Enums;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services
{
	// Landlord view/edit contracts in their system — create/delete/assign tenants handled by Manager
	public class LandlordContractService
	{
		private readonly ContractRepository _contracts;
		private readonly PropertyRepository _properties;
		private readonly BuildingRepository _buildings;
		private readonly UserRepository _users;
		private readonly AuditLogRepository _audits;

		public LandlordContractService() : this(new IHomeDbContext()) { }

		public LandlordContractService(IHomeDbContext context)
		{
			_contracts = new ContractRepository(context);
			_properties = new PropertyRepository(context);
			_buildings = new BuildingRepository(context);
			_users = new UserRepository(context);
			_audits = new AuditLogRepository(context);
		}

		// Property filter combo — active properties only
		public List<PropertyFilterOptionDto> GetPropertyOptions(int landlordId)
		{
			EnsureLandlord(landlordId);
			var options = new List<PropertyFilterOptionDto>
			{
				new() { Id = 0, Name = "Tất cả nhà trọ" }
			};
			options.AddRange(_properties.GetByLandlord(landlordId)
				.Where(p => p.IsActive)
				.OrderBy(p => p.Name)
				.Select(p => new PropertyFilterOptionDto { Id = p.Id, Name = p.Name }));
			return options;
		}

		// Building filter combo by property — Id=0 means all buildings
		public List<BuildingFilterOptionDto> GetBuildingOptions(int landlordId, int propertyId)
		{
			EnsureLandlord(landlordId);
			var options = new List<BuildingFilterOptionDto>
			{
				new() { Id = 0, PropertyId = propertyId, Name = "Tất cả tòa" }
			};
			if (propertyId <= 0) return options;

			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}

			options.AddRange(_buildings.GetByProperty(propertyId)
				.Where(b => b.IsActive)
				.OrderBy(b => b.Name)
				.Select(b => new BuildingFilterOptionDto
				{
					Id = b.Id,
					PropertyId = b.PropertyId,
					Name = b.Name,
					NumberOfFloors = b.NumberOfFloors
				}));
			return options;
		}

		// Contract status combo for filter/form
		public List<ContractStatusOptionDto> GetStatusOptions()
		{
			return new()
			{
				new() { Value = ContractStatus.Active, Name = ContractStatus.Active },
				new() { Value = ContractStatus.Expired, Name = ContractStatus.Expired },
				new() { Value = ContractStatus.Terminated, Name = ContractStatus.Terminated }
			};
		}

		// Contract grid — optional filter by property/building
		public List<ContractDto> GetByLandlord(int landlordId, int? propertyId = null, int? buildingId = null)
		{
			EnsureLandlord(landlordId);
			return _contracts.GetForLandlord(landlordId, propertyId, buildingId)
				.Select(MapList)
				.ToList();
		}

		// View/edit form — resolve main tenant from ContractTenants
		public ContractFormDto? GetForm(int landlordId, int contractId)
		{
			var contract = RequireOwned(landlordId, contractId);
			var main = contract.ContractTenants?
				.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant
				?? contract.ContractTenants?.FirstOrDefault()?.Tenant;

			return new ContractFormDto
			{
				Id = contract.Id,
				PropertyName = contract.Room?.Building?.Property?.Name ?? string.Empty,
				BuildingName = contract.Room?.Building?.Name ?? string.Empty,
				RoomNumber = contract.Room?.RoomNumber ?? string.Empty,
				MainTenantName = main?.FullName ?? "-",
				StartDate = contract.StartDate,
				EndDate = contract.EndDate,
				MonthlyRent = contract.MonthlyRent,
				DepositAmount = contract.DepositAmount,
				Status = contract.Status,
				Notes = contract.Notes
			};
		}

		// Tenant list on contract — main tenant sorted first
		public List<ContractTenantMemberDto> GetTenants(int landlordId, int contractId)
		{
			var contract = RequireOwned(landlordId, contractId);
			return (contract.ContractTenants ?? Enumerable.Empty<ContractTenant>())
				.OrderByDescending(ct => ct.IsMainTenant)
				.ThenBy(ct => ct.Tenant?.FullName)
				.Select(ct => new ContractTenantMemberDto
				{
					TenantId = ct.TenantId,
					FullName = ct.Tenant?.FullName ?? string.Empty,
					PhoneNumber = ct.Tenant?.PhoneNumber ?? string.Empty,
					IdCardNumber = ct.Tenant?.IdCardNumber ?? string.Empty,
					IsMainTenant = ct.IsMainTenant
				})
				.ToList();
		}

		// Landlord updates contract metadata only (dates, amounts, status, notes)
		public void Update(int landlordId, ContractFormDto form)
		{
			RequireOwned(landlordId, form.Id);
			Validate(form);

			var existing = _contracts.GetByIdForLandlord(landlordId, form.Id)
				?? throw new InvalidOperationException("Không tìm thấy hợp đồng.");
			string? notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();
			var before = new Dictionary<string, string?>
			{
				[AuditField.StartDate] = existing.StartDate.ToString("dd/MM/yyyy"),
				[AuditField.EndDate] = existing.EndDate.ToString("dd/MM/yyyy"),
				[AuditField.MonthlyRent] = existing.MonthlyRent.ToString("N0"),
				[AuditField.Deposit] = existing.DepositAmount.ToString("N0"),
				[AuditField.Status] = ContractStatus.FormatWithEndDate(existing.Status, existing.EndDate),
				[AuditField.Notes] = existing.Notes
			};
			var after = new Dictionary<string, string?>
			{
				[AuditField.StartDate] = form.StartDate.ToString("dd/MM/yyyy"),
				[AuditField.EndDate] = form.EndDate.ToString("dd/MM/yyyy"),
				[AuditField.MonthlyRent] = form.MonthlyRent.ToString("N0"),
				[AuditField.Deposit] = form.DepositAmount.ToString("N0"),
				[AuditField.Status] = ContractStatus.FormatWithEndDate(form.Status, form.EndDate),
				[AuditField.Notes] = notes
			};
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			_contracts.Update(new Contract
			{
				Id = form.Id,
				StartDate = form.StartDate,
				EndDate = form.EndDate,
				MonthlyRent = form.MonthlyRent,
				DepositAmount = form.DepositAmount,
				Status = form.Status,
				Notes = notes
			});

			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Update,
				TableName = AuditObject.Contracts,
				RecordId = form.Id.ToString(),
				Detail = existing.Room?.RoomNumber ?? form.Id.ToString(),
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}

		// Load contract and verify it belongs to landlord via Room→Building→Property chain
		private Contract RequireOwned(int landlordId, int contractId)
		{
			EnsureLandlord(landlordId);
			var contract = _contracts.GetByIdForLandlord(landlordId, contractId);
			if (contract == null)
			{
				throw new UnauthorizedAccessException("Hợp đồng không thuộc hệ thống nhà trọ của bạn.");
			}
			return contract;
		}

		// Guard caller has Landlord role
		private void EnsureLandlord(int landlordId)
		{
			var user = _users.GetById(landlordId);
			if (user == null || user.Role != UserRole.Landlord)
			{
				throw new UnauthorizedAccessException("Chỉ chủ nhà mới quản lý hợp đồng.");
			}
		}

		// Validate contract form fields
		private static void Validate(ContractFormDto form)
		{
			if (form.EndDate < form.StartDate)
			{
				throw new ArgumentException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
			}
			if (form.MonthlyRent < 0)
			{
				throw new ArgumentException("Tiền thuê không được âm.");
			}
			if (form.DepositAmount < 0)
			{
				throw new ArgumentException("Tiền cọc không được âm.");
			}
			if (form.Status is not (ContractStatus.Active or ContractStatus.Expired or ContractStatus.Terminated))
			{
				throw new ArgumentException("Trạng thái hợp đồng không hợp lệ.");
			}
		}

		// Map Contract entity to grid list DTO
		private static ContractDto MapList(Contract c)
		{
			var main = c.ContractTenants?
				.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant
				?? c.ContractTenants?.FirstOrDefault()?.Tenant;

			return new ContractDto
			{
				Id = c.Id,
				PropertyId = c.Room?.Building?.PropertyId ?? 0,
				PropertyName = c.Room?.Building?.Property?.Name ?? string.Empty,
				BuildingId = c.Room?.BuildingId ?? 0,
				BuildingName = c.Room?.Building?.Name ?? string.Empty,
				RoomId = c.RoomId,
				RoomNumber = c.Room?.RoomNumber ?? string.Empty,
				MainTenantName = main?.FullName ?? "-",
				TenantCount = c.ContractTenants?.Count ?? 0,
				StartDate = c.StartDate,
				EndDate = c.EndDate,
				MonthlyRent = c.MonthlyRent,
				DepositAmount = c.DepositAmount,
				Status = c.Status,
				StatusDisplay = ContractStatus.FormatWithEndDate(c.Status, c.EndDate),
				Notes = c.Notes
			};
		}
	}
}
