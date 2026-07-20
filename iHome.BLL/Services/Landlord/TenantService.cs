using iHome.BLL.Common;
using iHome.BLL.DTOs.Landlord;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace iHome.BLL.Services
{
	public class LandlordTenantService
	{
		private const string StatusActive = "Active";

		private readonly TenantRepository _tenants;
		private readonly UserRepository _users;
		private readonly AuditLogRepository _audits;

		public LandlordTenantService() : this(new IHomeDbContext())
		{
		}

		public LandlordTenantService(IHomeDbContext context)
		{
			_tenants = new TenantRepository(context);
			_users = new UserRepository(context);
			_audits = new AuditLogRepository(context);
		}

		public List<TenantDto> GetByLandlord(int landlordId)
		{
			EnsureLandlord(landlordId);
			return _tenants.GetForLandlord(landlordId).Select(MapList).ToList();
		}

		public TenantFormDto? GetForm(int landlordId, int tenantId)
		{
			EnsureAccessible(landlordId, tenantId);
			var tenant = _tenants.GetById(tenantId);
			if (tenant == null) return null;
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

		public List<TenantContractDto> GetContracts(int landlordId, int tenantId)
		{
			EnsureAccessible(landlordId, tenantId);
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
					StatusDisplay = FormatContractStatus(ct.Contract.Status, ct.Contract.EndDate),
					MonthlyRent = ct.Contract.MonthlyRent
				})
				.ToList();
		}

		public int Create(int landlordId, TenantFormDto form)
		{
			EnsureLandlord(landlordId);
			Validate(form);
			if (_tenants.IdCardExists(form.IdCardNumber))
			{
				throw new InvalidOperationException("CCCD đã tồn tại trong hệ thống.");
			}

			var entity = ToEntity(form);
			entity.CreatedAt = DateTime.Now;
			if (!_tenants.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm khách thuê.");
			}

			_audits.Add(
				landlordId,
				"Create",
				"Tenants",
				entity.Id.ToString(),
				null,
				entity.FullName);
			return entity.Id;
		}

		public void Update(int landlordId, TenantFormDto form)
		{
			EnsureAccessible(landlordId, form.Id);
			Validate(form);
			if (_tenants.IdCardExists(form.IdCardNumber, form.Id))
			{
				throw new InvalidOperationException("CCCD đã tồn tại trong hệ thống.");
			}

			var existing = _tenants.GetById(form.Id)
				?? throw new InvalidOperationException("Không tìm thấy khách thuê.");
			string oldValue = $"{existing.FullName}|{existing.IdCardNumber}|{existing.PhoneNumber}";

			var entity = ToEntity(form);
			if (!_tenants.Update(entity))
			{
				throw new InvalidOperationException("Không thể cập nhật khách thuê.");
			}

			_audits.Add(
				landlordId,
				"Update",
				"Tenants",
				form.Id.ToString(),
				oldValue,
				$"{entity.FullName}|{entity.IdCardNumber}|{entity.PhoneNumber}");
		}

		public void Delete(int landlordId, int tenantId)
		{
			EnsureAccessible(landlordId, tenantId);
			var existing = _tenants.GetById(tenantId)
				?? throw new InvalidOperationException("Không tìm thấy khách thuê.");
			if (_tenants.HasAnyContracts(tenantId))
			{
				throw new InvalidOperationException(
					"Khách đang có lịch sử hợp đồng. Hãy gỡ khách khỏi hợp đồng trước khi xóa.");
			}

			string snapshot = existing.FullName;
			if (!_tenants.Delete(tenantId))
			{
				throw new InvalidOperationException("Không thể xóa khách thuê.");
			}

			_audits.Add(landlordId, "Delete", "Tenants", tenantId.ToString(), snapshot, null);
		}

		private void EnsureLandlord(int landlordId)
		{
			var user = _users.GetById(landlordId);
			if (user == null || user.Role != "Landlord")
			{
				throw new UnauthorizedAccessException("Chỉ chủ nhà mới quản lý khách thuê.");
			}
		}

		private void EnsureAccessible(int landlordId, int tenantId)
		{
			EnsureLandlord(landlordId);
			if (!_tenants.IsAccessible(landlordId, tenantId))
			{
				throw new UnauthorizedAccessException("Khách thuê không thuộc hệ thống nhà trọ của bạn.");
			}
		}

		private static void Validate(TenantFormDto form)
		{
			form.FullName = form.FullName.Trim();
			form.IdCardNumber = form.IdCardNumber.Trim();
			form.PhoneNumber = form.PhoneNumber.Trim();
			form.Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim();
			form.PermanentAddress = string.IsNullOrWhiteSpace(form.PermanentAddress)
				? null
				: form.PermanentAddress.Trim();

			if (form.FullName.Length < 2 || form.FullName.Length > 100)
			{
				throw new ArgumentException("Họ tên phải có từ 2 đến 100 ký tự.");
			}
			if (form.DateOfBirth == default || form.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
			{
				throw new ArgumentException("Ngày sinh không hợp lệ.");
			}
			if (form.IdCardNumber.Length < 9 || form.IdCardNumber.Length > 20 ||
				!form.IdCardNumber.All(char.IsDigit))
			{
				throw new ArgumentException("CCCD phải gồm từ 9 đến 20 chữ số.");
			}
			string phoneDigits = form.PhoneNumber.TrimStart('+');
			if (phoneDigits.Length < 9 || phoneDigits.Length > 15 || !phoneDigits.All(char.IsDigit))
			{
				throw new ArgumentException("Số điện thoại không hợp lệ.");
			}
			if (form.Email != null)
			{
				try
				{
					_ = new MailAddress(form.Email);
				}
				catch (FormatException)
				{
					throw new ArgumentException("Email không hợp lệ.");
				}
			}
		}

		private static Tenant ToEntity(TenantFormDto form) => new()
		{
			Id = form.Id,
			FullName = InputFormatter.FormatFullName(form.FullName),
			DateOfBirth = form.DateOfBirth,
			IdCardNumber = form.IdCardNumber,
			PhoneNumber = form.PhoneNumber,
			Email = form.Email,
			PermanentAddress = form.PermanentAddress
		};

		private static TenantDto MapList(Tenant t)
		{
			var active = t.ContractTenants?
				.Where(ct =>
					ct.Contract != null &&
					string.Equals(ct.Contract.Status, StatusActive, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(ct => ct.IsMainTenant)
				.ThenByDescending(ct => ct.Contract!.StartDate)
				.FirstOrDefault();

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

			if (active?.Contract?.Room?.Building == null)
			{
				return dto;
			}

			dto.CurrentPropertyName = active.Contract.Room.Building.Property?.Name ?? "-";
			dto.CurrentBuildingName = active.Contract.Room.Building.Name;
			dto.CurrentRoomNumber = active.Contract.Room.RoomNumber;
			dto.CurrentRoleDisplay = active.IsMainTenant ? "Người thuê chính" : "Người ở cùng";
			dto.CurrentStatusDisplay = FormatContractStatus(active.Contract.Status, active.Contract.EndDate);
			return dto;
		}

		private static string FormatContractStatus(string status, DateOnly? endDate)
		{
			if (string.Equals(status, StatusActive, StringComparison.OrdinalIgnoreCase) &&
				endDate.HasValue)
			{
				DateOnly today = DateOnly.FromDateTime(DateTime.Today);
				if (endDate.Value < today) return "Đã hết hạn";
				if (endDate.Value < today.AddMonths(1)) return "Sắp hết hạn";
			}

			return status switch
			{
				"Active" => "Đang hoạt động",
				"Expired" => "Đã hết hạn",
				"Terminated" => "Đã chấm dứt",
				_ => status
			};
		}
	}
}
