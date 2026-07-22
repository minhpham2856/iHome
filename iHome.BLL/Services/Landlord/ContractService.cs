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
			// Contract data access against shared DbContext
			_contracts = new ContractRepository(context);
			// Property filter options
			_properties = new PropertyRepository(context);
			// Building filter options
			_buildings = new BuildingRepository(context);
			// Landlord role guard
			_users = new UserRepository(context);
			// Audit trail for update operations
			_audits = new AuditLogRepository(context);
		}

		// Property filter combo — active properties only
		public List<PropertyFilterOptionDto> GetPropertyOptions(int landlordId)
		{
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Start with "all properties" placeholder
			var options = new List<PropertyFilterOptionDto>
			{
				new() { Id = 0, Name = "Tất cả nhà trọ" }
			};
			// Append active landlord properties sorted by name
			options.AddRange(_properties.GetByLandlord(landlordId)
				.Where(p => p.IsActive)
				.OrderBy(p => p.Name)
				.Select(p => new PropertyFilterOptionDto { Id = p.Id, Name = p.Name }));
			// Return filter combo options
			return options;
		}

		// Building filter combo by property — Id=0 means all buildings
		public List<BuildingFilterOptionDto> GetBuildingOptions(int landlordId, int propertyId)
		{
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Start with "all buildings" placeholder
			var options = new List<BuildingFilterOptionDto>
			{
				new() { Id = 0, PropertyId = propertyId, Name = "Tất cả tòa" }
			};
			// No specific property selected — return placeholder only
			if (propertyId <= 0) return options;

			// Load property and verify landlord ownership
			var property = _properties.GetById(propertyId);
			// Reject missing or foreign-owned properties
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}

			// Append active buildings for property sorted by name
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
			// Return filter combo options
			return options;
		}

		// Contract status combo for filter/form
		public List<ContractStatusOptionDto> GetStatusOptions()
		{
			// Return fixed list of allowed contract status values
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
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Load scoped contracts and map to list DTOs
			return _contracts.GetForLandlord(landlordId, propertyId, buildingId)
				.Select(MapList)
				.ToList();
		}

		// View/edit form — resolve main tenant from ContractTenants
		public ContractFormDto? GetForm(int landlordId, int contractId)
		{
			// Load contract and verify landlord ownership
			var contract = RequireOwned(landlordId, contractId);
			// Prefer main tenant; fall back to first linked tenant
			var main = contract.ContractTenants?
				.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant
				?? contract.ContractTenants?.FirstOrDefault()?.Tenant;

			// Project contract metadata into form DTO
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
			// Load contract and verify landlord ownership
			var contract = RequireOwned(landlordId, contractId);
			// Map contract tenant links to member DTOs
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
			// Verify contract belongs to landlord
			RequireOwned(landlordId, form.Id);
			// Validate form fields
			Validate(form);

			// Reload contract with landlord scope for audit snapshot
			var existing = _contracts.GetByIdForLandlord(landlordId, form.Id)
				?? throw new InvalidOperationException("Không tìm thấy hợp đồng.");
			// Normalize optional notes field
			string? notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();
			// Snapshot values before edit for audit diff
			var before = new Dictionary<string, string?>
			{
				[AuditField.StartDate] = existing.StartDate.ToString("dd/MM/yyyy"),
				[AuditField.EndDate] = existing.EndDate.ToString("dd/MM/yyyy"),
				[AuditField.MonthlyRent] = existing.MonthlyRent.ToString("N0"),
				[AuditField.Deposit] = existing.DepositAmount.ToString("N0"),
				[AuditField.Status] = ContractStatus.FormatWithEndDate(existing.Status, existing.EndDate),
				[AuditField.Notes] = existing.Notes
			};
			// Snapshot values after edit for audit diff
			var after = new Dictionary<string, string?>
			{
				[AuditField.StartDate] = form.StartDate.ToString("dd/MM/yyyy"),
				[AuditField.EndDate] = form.EndDate.ToString("dd/MM/yyyy"),
				[AuditField.MonthlyRent] = form.MonthlyRent.ToString("N0"),
				[AuditField.Deposit] = form.DepositAmount.ToString("N0"),
				[AuditField.Status] = ContractStatus.FormatWithEndDate(form.Status, form.EndDate),
				[AuditField.Notes] = notes
			};
			// Build compact old/new diff strings
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			// Persist updated contract row
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

			// Write update audit entry
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
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Load contract scoped to landlord
			var contract = _contracts.GetByIdForLandlord(landlordId, contractId);
			// Reject contract outside landlord scope
			if (contract == null)
			{
				throw new UnauthorizedAccessException("Hợp đồng không thuộc hệ thống nhà trọ của bạn.");
			}
			// Return verified entity to caller
			return contract;
		}

		// Guard caller has Landlord role
		private void EnsureLandlord(int landlordId)
		{
			// Load user by primary key
			var user = _users.GetById(landlordId);
			// Reject missing user or non-landlord role
			if (user == null || user.Role != UserRole.Landlord)
			{
				throw new UnauthorizedAccessException("Chỉ chủ nhà mới quản lý hợp đồng.");
			}
		}

		// Validate contract form fields
		private static void Validate(ContractFormDto form)
		{
			// Reject end date before start date
			if (form.EndDate < form.StartDate)
			{
				throw new ArgumentException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
			}
			// Reject negative monthly rent
			if (form.MonthlyRent < 0)
			{
				throw new ArgumentException("Tiền thuê không được âm.");
			}
			// Reject negative deposit
			if (form.DepositAmount < 0)
			{
				throw new ArgumentException("Tiền cọc không được âm.");
			}
			// Reject unknown contract status
			if (form.Status is not (ContractStatus.Active or ContractStatus.Expired or ContractStatus.Terminated))
			{
				throw new ArgumentException("Trạng thái hợp đồng không hợp lệ.");
			}
		}

		// Map Contract entity to grid list DTO
		private static ContractDto MapList(Contract c)
		{
			// Prefer main tenant; fall back to first linked tenant
			var main = c.ContractTenants?
				.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant
				?? c.ContractTenants?.FirstOrDefault()?.Tenant;

			// Project contract and room metadata into list DTO
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
