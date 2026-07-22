using iHome.BLL.Common;
using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Enums;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace iHome.BLL.Services
{
	// Landlord view/edit tenants linked to contracts in their properties — add/remove tenants handled by Manager
	public class LandlordTenantService
	{
		private readonly TenantRepository _tenants;
		private readonly PropertyRepository _properties;
		private readonly BuildingRepository _buildings;
		private readonly UserRepository _users;
		private readonly AuditLogRepository _audits;

		public LandlordTenantService() : this(new IHomeDbContext()) { }

		public LandlordTenantService(IHomeDbContext context)
		{
			// Tenant data access against shared DbContext
			_tenants = new TenantRepository(context);
			// Property filter options
			_properties = new PropertyRepository(context);
			// Building filter options
			_buildings = new BuildingRepository(context);
			// Landlord role guard
			_users = new UserRepository(context);
			// Audit trail for update operations
			_audits = new AuditLogRepository(context);
		}

		// Property filter combo
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

		// Building filter combo by property
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

		// Grid of all tenants who ever rented within landlord scope
		public List<TenantDto> GetByLandlord(int landlordId)
		{
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Load scoped tenants and map to list DTOs
			return _tenants.GetForLandlord(landlordId).Select(MapList).ToList();
		}

		// Edit form — EnsureAccessible guard runs first
		public TenantFormDto? GetForm(int landlordId, int tenantId)
		{
			// Verify tenant is reachable via landlord's contracts
			EnsureAccessible(landlordId, tenantId);
			// Load tenant entity by primary key
			var tenant = _tenants.GetById(tenantId);
			// Return null when tenant row missing after access check
			if (tenant == null) return null;
			// Project entity fields into form DTO
			return new TenantFormDto
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

		// Contract history for tenant within landlord scope
		public List<TenantContractDto> GetContracts(int landlordId, int tenantId)
		{
			// Verify tenant is reachable via landlord's contracts
			EnsureAccessible(landlordId, tenantId);
			// Load contract links and map to history DTOs
			return _tenants.GetContractLinks(landlordId, tenantId)
				.Select(ct => new TenantContractDto
				{
					ContractId = ct.ContractId,
					PropertyName = ct.Contract.Room.Building.Property?.Name ?? string.Empty,
					BuildingName = ct.Contract.Room.Building.Name,
					RoomNumber = ct.Contract.Room.RoomNumber,
					IsMainTenant = ct.IsMainTenant,
					StartDate = ct.Contract.StartDate,
					EndDate = ct.Contract.EndDate,
					Status = ct.Contract.Status,
					StatusDisplay = ContractStatus.FormatWithEndDate(ct.Contract.Status, ct.Contract.EndDate),
					MonthlyRent = ct.Contract.MonthlyRent
				})
				.ToList();
		}

		// Update tenant personal info — guard unique national ID (CCCD)
		public void Update(int landlordId, TenantFormDto form)
		{
			// Verify tenant is reachable via landlord's contracts
			EnsureAccessible(landlordId, form.Id);
			// Validate and normalize form fields
			Validate(form);
			// Reject duplicate national ID on another tenant
			if (_tenants.IdCardExists(form.IdCardNumber, form.Id))
			{
				throw new InvalidOperationException("CCCD đã tồn tại trong hệ thống.");
			}

			// Load existing tenant for audit snapshot
			var existing = _tenants.GetById(form.Id)
				?? throw new InvalidOperationException("Không tìm thấy khách thuê.");
			// Snapshot values before edit for audit diff
			var before = new Dictionary<string, string?>
			{
				[AuditField.FullName] = existing.FullName,
				[AuditField.IdCard] = existing.IdCardNumber,
				[AuditField.Phone] = existing.PhoneNumber
			};

			// Map form to entity with formatting helpers
			var entity = ToEntity(form);
			// UPDATE; throw if repository reports failure
			if (!_tenants.Update(entity))
			{
				throw new InvalidOperationException("Không thể cập nhật khách thuê.");
			}

			// Snapshot values after edit for audit diff
			var after = new Dictionary<string, string?>
			{
				[AuditField.FullName] = entity.FullName,
				[AuditField.IdCard] = entity.IdCardNumber,
				[AuditField.Phone] = entity.PhoneNumber
			};
			// Build compact old/new diff strings
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			// Write update audit entry
			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Update,
				TableName = AuditObject.Tenants,
				RecordId = form.Id.ToString(),
				Detail = entity.FullName,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}

		// Guard caller has Landlord role
		private void EnsureLandlord(int landlordId)
		{
			// Load user by primary key
			var user = _users.GetById(landlordId);
			// Reject missing user or non-landlord role
			if (user == null || user.Role != UserRole.Landlord)
			{
				throw new UnauthorizedAccessException("Chỉ chủ nhà mới quản lý khách thuê.");
			}
		}

		// Tenant must have at least one contract link to landlord's property
		private void EnsureAccessible(int landlordId, int tenantId)
		{
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Reject tenant outside landlord contract scope
			if (!_tenants.IsAccessible(landlordId, tenantId))
			{
				throw new UnauthorizedAccessException("Khách thuê không thuộc hệ thống nhà trọ của bạn.");
			}
		}

		// Validate and normalize trimmed form fields
		private static void Validate(TenantFormDto form)
		{
			// Trim required text fields
			form.FullName = form.FullName.Trim();
			form.IdCardNumber = form.IdCardNumber.Trim();
			form.PhoneNumber = form.PhoneNumber.Trim();
			// Normalize optional email and address
			form.Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim();
			form.PermanentAddress = string.IsNullOrWhiteSpace(form.PermanentAddress)
				? null
				: form.PermanentAddress.Trim();

			// Reject invalid full name length
			if (form.FullName.Length < 2 || form.FullName.Length > 100)
			{
				throw new ArgumentException("Họ tên phải có từ 2 đến 100 ký tự.");
			}
			// Reject missing or future date of birth
			if (form.DateOfBirth == default || form.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
			{
				throw new ArgumentException("Ngày sinh không hợp lệ.");
			}
			// Reject invalid national ID format
			if (form.IdCardNumber.Length < 9 || form.IdCardNumber.Length > 20 ||
				!form.IdCardNumber.All(char.IsDigit))
			{
				throw new ArgumentException("CCCD phải gồm từ 9 đến 20 chữ số.");
			}
			// Validate phone digits (optional leading + stripped)
			string phoneDigits = form.PhoneNumber.TrimStart('+');
			if (phoneDigits.Length < 9 || phoneDigits.Length > 15 || !phoneDigits.All(char.IsDigit))
			{
				throw new ArgumentException("Số điện thoại không hợp lệ.");
			}
			// Validate email format when provided
			if (form.Email != null)
			{
				try
				{
					// MailAddress parser throws on invalid format
					_ = new MailAddress(form.Email);
				}
				catch (FormatException)
				{
					throw new ArgumentException("Email không hợp lệ.");
				}
			}
		}

		// Map form DTO to Tenant entity with formatting helpers
		private static Tenant ToEntity(TenantFormDto form)
		{
			// Project form fields into persistence entity
			return new Tenant
			{
				Id = form.Id,
				FullName = InputFormatter.FormatFullName(form.FullName),
				DateOfBirth = form.DateOfBirth,
				IdCardNumber = form.IdCardNumber,
				PhoneNumber = form.PhoneNumber,
				Email = form.Email,
				PermanentAddress = form.PermanentAddress
			};
		}

		// List DTO with current room info from most recent Active contract
		private static TenantDto MapList(Tenant t)
		{
			// Find best active contract link — main tenant preferred, then latest start date
			var active = t.ContractTenants?
				.Where(ct =>
					ct.Contract != null &&
					string.Equals(ct.Contract.Status, ContractStatus.Active, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(ct => ct.IsMainTenant)
				.ThenByDescending(ct => ct.Contract!.StartDate)
				.FirstOrDefault();

			// Build base list DTO from tenant scalars
			var dto = new TenantDto
			{
				Id = t.Id,
				FullName = t.FullName,
				DateOfBirth = t.DateOfBirth,
				IdCardNumber = t.IdCardNumber,
				PhoneNumber = t.PhoneNumber,
				Email = t.Email,
				PermanentAddress = t.PermanentAddress,
				ContractCount = t.ContractTenants?.Count ?? 0
			};

			// No active contract with room navigation — return base DTO
			if (active?.Contract?.Room?.Building == null)
			{
				return dto;
			}

			// Enrich DTO with current placement from active contract
			dto.CurrentPropertyId = active.Contract.Room.Building.PropertyId;
			dto.CurrentPropertyName = active.Contract.Room.Building.Property?.Name ?? "-";
			dto.CurrentBuildingId = active.Contract.Room.BuildingId;
			dto.CurrentBuildingName = active.Contract.Room.Building.Name;
			dto.CurrentRoomNumber = active.Contract.Room.RoomNumber;
			dto.CurrentRoleDisplay = active.IsMainTenant ? "Người thuê chính" : "Người ở cùng";
			dto.CurrentStatusDisplay = ContractStatus.FormatWithEndDate(active.Contract.Status, active.Contract.EndDate);
			// Return enriched list DTO
			return dto;
		}
	}
}
