using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class ManagerOperationsRepository
	{
		private const string ActiveContract = "Active";
		private const string OccupiedRoom = "Occupied";
		private const string VacantRoom = "Vacant";
		private readonly IHomeDbContext _context;

		public ManagerOperationsRepository()
		{
			_context = new IHomeDbContext();
		}

		public List<Tenant> GetManageableTenants(int managerId)
		{
			var linkedIds = _context.ContractTenants
				.AsNoTracking()
				.Where(ct => ct.Contract.Room.Building.ManagerId == managerId)
				.Select(ct => ct.TenantId);
			var createdIds = GetCreatedTenantIds(managerId);

			return _context.Tenants
				.AsNoTracking()
				.Where(t => linkedIds.Contains(t.Id) || createdIds.Contains(t.Id))
				.OrderBy(t => t.FullName)
				.ToList();
		}

		public List<Tenant> GetUnassignedManagerTenants(int managerId)
		{
			var createdIds = GetCreatedTenantIds(managerId);
			return _context.Tenants
				.AsNoTracking()
				.Where(t =>
					createdIds.Contains(t.Id) &&
					!t.ContractTenants.Any(ct => ct.Contract.Room.Building.ManagerId == managerId))
				.OrderBy(t => t.FullName)
				.ToList();
		}

		public Tenant CreateTenant(int managerId, Tenant tenant)
		{
			EnsureManagerHasBuilding(managerId);
			using var transaction = _context.Database.BeginTransaction();
			_context.Tenants.Add(tenant);
			_context.SaveChanges();
			AddAudit(managerId, "Create", "Tenants", tenant.Id, null, tenant.FullName);
			_context.SaveChanges();
			transaction.Commit();
			return tenant;
		}

		public void UpdateTenant(int managerId, Tenant changes)
		{
			EnsureTenantAccess(managerId, changes.Id);
			var tenant = _context.Tenants.Single(t => t.Id == changes.Id);
			string oldValue = $"{tenant.FullName}|{tenant.IdCardNumber}|{tenant.PhoneNumber}";
			tenant.FullName = changes.FullName;
			tenant.DateOfBirth = changes.DateOfBirth;
			tenant.IdCardNumber = changes.IdCardNumber;
			tenant.PhoneNumber = changes.PhoneNumber;
			tenant.Email = changes.Email;
			tenant.PermanentAddress = changes.PermanentAddress;
			AddAudit(managerId, "Update", "Tenants", tenant.Id, oldValue,
				$"{tenant.FullName}|{tenant.IdCardNumber}|{tenant.PhoneNumber}");
			_context.SaveChanges();
		}

		public void DeleteTenant(int managerId, int tenantId)
		{
			EnsureTenantAccess(managerId, tenantId);
			var tenant = _context.Tenants
				.Include(t => t.ContractTenants)
				.Single(t => t.Id == tenantId);
			if (tenant.ContractTenants.Count > 0)
			{
				throw new InvalidOperationException("Khách đang có lịch sử hợp đồng. Hãy gỡ khách khỏi hợp đồng trước khi xóa.");
			}

			using var transaction = _context.Database.BeginTransaction();
			_context.Tenants.Remove(tenant);
			AddAudit(managerId, "Delete", "Tenants", tenant.Id, tenant.FullName, null);
			_context.SaveChanges();
			transaction.Commit();
		}

		public bool IdCardExists(string idCardNumber, int? excludingTenantId = null) =>
			_context.Tenants.Any(t =>
				t.IdCardNumber == idCardNumber &&
				(!excludingTenantId.HasValue || t.Id != excludingTenantId.Value));

		public List<Contract> GetContracts(int managerId, int? buildingId = null) =>
			_context.Contracts
				.AsNoTracking()
				.Include(c => c.Room)
					.ThenInclude(r => r.Building)
				.Include(c => c.ContractTenants)
					.ThenInclude(ct => ct.Tenant)
				.Where(c =>
					c.Room.Building.ManagerId == managerId &&
					(!buildingId.HasValue || c.Room.BuildingId == buildingId.Value))
				.OrderByDescending(c => c.StartDate)
				.ThenBy(c => c.Room.Building.Name)
				.ThenBy(c => c.Room.RoomNumber)
				.ToList();

		// Tạo hợp đồng: bắt buộc đủ số khách đứng tên = MaxOccupancy phòng (vd. phòng đôi = 2)
		// Tiền thuê/cọc vẫn 1 mức chung cho cả hợp đồng, không chia theo từng khách
		public Contract CreateContract(
			int managerId,
			Contract contract,
			int mainTenantId,
			IReadOnlyCollection<int>? coTenantIds = null)
		{
			var room = GetManagedRoom(managerId, contract.RoomId);
			EnsureTenantAccess(managerId, mainTenantId);
			if (string.Equals(room.Status, "Maintenance", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Không thể tạo hợp đồng cho phòng đang bảo trì.");
			}

			// Danh sách đứng tên: người thuê chính đứng đầu, sau đó khách còn lại (không trùng)
			var tenantIds = new List<int> { mainTenantId };
			if (coTenantIds != null)
			{
				tenantIds.AddRange(coTenantIds.Where(id => id > 0 && id != mainTenantId).Distinct());
			}

			int maxOccupancy = room.RoomType.MaxOccupancy;
			if (maxOccupancy < 1)
			{
				throw new InvalidOperationException("Phòng không còn sức chứa cho khách thuê.");
			}
			if (tenantIds.Count != maxOccupancy)
			{
				throw new InvalidOperationException(
					$"Hợp đồng phải có đủ {maxOccupancy} khách đứng tên theo sức chứa phòng.");
			}

			if (contract.Status == ActiveContract)
			{
				EnsureContractRoomAvailable(contract.RoomId, contract.StartDate, contract.EndDate, null);
				foreach (int tenantId in tenantIds)
				{
					EnsureTenantAccess(managerId, tenantId);
					EnsureTenantAvailable(tenantId, contract.StartDate, contract.EndDate, null);
				}
			}

			using var transaction = _context.Database.BeginTransaction();
			_context.Contracts.Add(contract);
			_context.SaveChanges();
			// index 0 = người thuê chính (IsMainTenant = true)
			for (int index = 0; index < tenantIds.Count; index++)
			{
				_context.ContractTenants.Add(new ContractTenant
				{
					ContractId = contract.Id,
					TenantId = tenantIds[index],
					IsMainTenant = index == 0
				});
			}
			var today = DateOnly.FromDateTime(DateTime.Today);
			if (contract.Status == ActiveContract && contract.StartDate <= today && contract.EndDate >= today)
			{
				room.Status = OccupiedRoom;
			}
			AddAudit(managerId, "Create", "Contracts", contract.Id, null,
				$"Room={contract.RoomId};Tenants={string.Join(',', tenantIds)}");
			_context.SaveChanges();
			transaction.Commit();
			return contract;
		}

		public void UpdateContract(int managerId, Contract changes)
		{
			var contract = _context.Contracts
				.Include(c => c.Room)
				.Single(c => c.Id == changes.Id && c.Room.Building.ManagerId == managerId);
			if (changes.Status == ActiveContract)
			{
				EnsureContractRoomAvailable(contract.RoomId, changes.StartDate, changes.EndDate, contract.Id);
			}
			string oldValue = $"{contract.StartDate}|{contract.EndDate}|{contract.MonthlyRent}|{contract.Status}";
			contract.StartDate = changes.StartDate;
			contract.EndDate = changes.EndDate;
			contract.MonthlyRent = changes.MonthlyRent;
			contract.DepositAmount = changes.DepositAmount;
			contract.Status = changes.Status;
			contract.Notes = changes.Notes;
			AddAudit(managerId, "Update", "Contracts", contract.Id, oldValue,
				$"{contract.StartDate}|{contract.EndDate}|{contract.MonthlyRent}|{contract.Status}");
			_context.SaveChanges();
			RefreshRoomStatus(contract.RoomId);
		}

		public void DeleteContract(int managerId, int contractId)
		{
			var contract = _context.Contracts
				.Include(c => c.Room)
				.Include(c => c.ContractTenants)
				.Include(c => c.Invoices)
				.Single(c => c.Id == contractId && c.Room.Building.ManagerId == managerId);
			if (contract.Invoices.Count > 0)
			{
				throw new InvalidOperationException("Không thể xóa hợp đồng đã phát sinh hóa đơn. Hãy chuyển trạng thái sang chấm dứt.");
			}

			int roomId = contract.RoomId;
			using var transaction = _context.Database.BeginTransaction();
			_context.ContractTenants.RemoveRange(contract.ContractTenants);
			_context.Contracts.Remove(contract);
			AddAudit(managerId, "Delete", "Contracts", contract.Id, $"Room={roomId}", null);
			_context.SaveChanges();
			RefreshRoomStatus(roomId);
			transaction.Commit();
		}

		public void AssignTenant(int managerId, int contractId, int tenantId, bool isMainTenant)
		{
			var contract = GetManagedContract(managerId, contractId);
			EnsureTenantAccess(managerId, tenantId);
			EnsureTenantAvailable(tenantId, contract.StartDate, contract.EndDate, contract.Id);
			var existing = contract.ContractTenants.SingleOrDefault(ct => ct.TenantId == tenantId);
			if (existing?.IsMainTenant == true && !isMainTenant)
			{
				throw new InvalidOperationException("Hợp đồng phải có người thuê chính. Hãy chọn khách khác làm người thuê chính trước.");
			}
			if (existing == null && contract.ContractTenants.Count >= contract.Room.RoomType.MaxOccupancy)
			{
				throw new InvalidOperationException("Phòng đã đạt số người tối đa.");
			}

			using var transaction = _context.Database.BeginTransaction();
			if (isMainTenant)
			{
				foreach (var member in contract.ContractTenants)
				{
					member.IsMainTenant = false;
				}
			}
			if (existing == null)
			{
				_context.ContractTenants.Add(new ContractTenant
				{
					ContractId = contractId,
					TenantId = tenantId,
					IsMainTenant = isMainTenant
				});
			}
			else
			{
				existing.IsMainTenant = isMainTenant;
			}
			AddAudit(managerId, "AssignTenant", "ContractTenants", contractId, null,
				$"Tenant={tenantId};Main={isMainTenant}");
			_context.SaveChanges();
			transaction.Commit();
		}

		public void RemoveTenant(int managerId, int contractId, int tenantId)
		{
			var contract = GetManagedContract(managerId, contractId);
			var link = contract.ContractTenants.SingleOrDefault(ct => ct.TenantId == tenantId)
				?? throw new InvalidOperationException("Khách không thuộc hợp đồng đã chọn.");
			if (contract.ContractTenants.Count == 1)
			{
				throw new InvalidOperationException("Hợp đồng phải có ít nhất một khách thuê.");
			}
			if (link.IsMainTenant)
			{
				throw new InvalidOperationException("Hãy chọn một khách khác làm người thuê chính trước khi gỡ.");
			}

			_context.ContractTenants.Remove(link);
			AddAudit(managerId, "RemoveTenant", "ContractTenants", contractId,
				$"Tenant={tenantId}", null);
			_context.SaveChanges();
		}

		public List<Invoice> GetInvoices(int managerId, int? buildingId = null) =>
			_context.Invoices
				.AsNoTracking()
				.Include(i => i.InvoiceItems)
				.Include(i => i.Contract)
					.ThenInclude(c => c.Room)
						.ThenInclude(r => r.Building)
				.Include(i => i.Contract)
					.ThenInclude(c => c.ContractTenants)
						.ThenInclude(ct => ct.Tenant)
				.Include(i => i.Payments)
				.Where(i =>
					i.Contract.Room.Building.ManagerId == managerId &&
					(!buildingId.HasValue || i.Contract.Room.BuildingId == buildingId.Value))
				.OrderByDescending(i => i.InvoiceDate)
				.ToList();

		public Contract GetInvoiceBillingContract(int managerId, int contractId) =>
			_context.Contracts
				.AsNoTracking()
				.Include(c => c.Room)
					.ThenInclude(r => r.Building)
				.Include(c => c.Room)
					.ThenInclude(r => r.RoomServices)
						.ThenInclude(rs => rs.Service)
				.Include(c => c.Room)
					.ThenInclude(r => r.RoomServices)
						.ThenInclude(rs => rs.ServiceReadings)
				.Include(c => c.ContractTenants)
				.SingleOrDefault(c =>
					c.Id == contractId &&
					c.Room.Building.ManagerId == managerId &&
					c.Room.Building.IsActive)
				?? throw new UnauthorizedAccessException("Hợp đồng không thuộc tòa nhà được phân công.");

		public Invoice CreateInvoice(
			int managerId,
			Invoice invoice,
			IReadOnlyCollection<InvoiceItem> items,
			IReadOnlyCollection<ServiceReading> readings)
		{
			EnsureContractAccess(managerId, invoice.ContractId);
			EnsureUniqueInvoiceMonth(invoice.ContractId, invoice.InvoiceDate, null);
			using var transaction = _context.Database.BeginTransaction();
			_context.Invoices.Add(invoice);
			_context.SaveChanges();
			foreach (var item in items)
			{
				item.InvoiceId = invoice.Id;
			}
			_context.InvoiceItems.AddRange(items);
			_context.ServiceReadings.AddRange(readings);
			AddAudit(managerId, "Create", "Invoices", invoice.Id, null,
				$"Contract={invoice.ContractId};Total={invoice.TotalAmount};Items={items.Count}");
			_context.SaveChanges();
			transaction.Commit();
			return invoice;
		}

		public void UpdateInvoice(int managerId, Invoice changes)
		{
			var invoice = _context.Invoices
				.Include(i => i.Contract)
					.ThenInclude(c => c.Room)
				.Include(i => i.InvoiceItems)
				.Single(i => i.Id == changes.Id && i.Contract.Room.Building.ManagerId == managerId);
			string oldValue = $"{invoice.InvoiceDate:d}|{invoice.DueDate}|{invoice.TotalAmount}|{invoice.Status}";
			invoice.DueDate = changes.DueDate;
			invoice.Status = changes.Status;
			AddAudit(managerId, "Update", "Invoices", invoice.Id, oldValue,
				$"{invoice.InvoiceDate:d}|{invoice.DueDate}|{invoice.TotalAmount}|{invoice.Status}");
			_context.SaveChanges();
		}

		public void DeleteInvoice(int managerId, int invoiceId)
		{
			var invoice = _context.Invoices
				.Include(i => i.Contract)
					.ThenInclude(c => c.Room)
				.Include(i => i.InvoiceItems)
				.Include(i => i.Payments)
				.Single(i => i.Id == invoiceId && i.Contract.Room.Building.ManagerId == managerId);
			if (invoice.Payments.Count > 0)
			{
				throw new InvalidOperationException("Không thể xóa hóa đơn đã có thanh toán.");
			}

			using var transaction = _context.Database.BeginTransaction();
			_context.InvoiceItems.RemoveRange(invoice.InvoiceItems);
			_context.Invoices.Remove(invoice);
			AddAudit(managerId, "Delete", "Invoices", invoice.Id, $"Total={invoice.TotalAmount}", null);
			_context.SaveChanges();
			transaction.Commit();
		}

		public List<RoomService> GetRoomServices(int managerId, int? buildingId = null) =>
			_context.RoomServices
				.AsNoTracking()
				.Include(rs => rs.Room)
					.ThenInclude(r => r.Building)
				.Include(rs => rs.Service)
				.Where(rs =>
					rs.Room.Building.ManagerId == managerId &&
					(!buildingId.HasValue || rs.Room.BuildingId == buildingId.Value))
				.OrderBy(rs => rs.Room.Building.Name)
				.ThenBy(rs => rs.Room.RoomNumber)
				.ThenBy(rs => rs.Service.ServiceName)
				.ToList();

		public void SetRoomService(int managerId, int roomId, int serviceId, bool isActive)
		{
			var room = GetManagedRoom(managerId, roomId);
			var service = _context.Services
				.Include(s => s.Property)
				.SingleOrDefault(s => s.Id == serviceId)
				?? throw new UnauthorizedAccessException("Dịch vụ không tồn tại.");
			if (room.Building.PropertyId != service.PropertyId)
			{
				throw new InvalidOperationException("Chỉ có thể gán dịch vụ cùng nhà trọ với phòng.");
			}
			bool managesProperty = _context.Buildings.Any(b =>
				b.ManagerId == managerId &&
				b.IsActive &&
				b.PropertyId == service.PropertyId);
			if (!managesProperty)
			{
				throw new UnauthorizedAccessException("Dịch vụ không thuộc nhà trọ được phân công.");
			}
			if (!service.IsActive && isActive)
			{
				throw new InvalidOperationException("Dịch vụ đã ngừng hoạt động và không thể gán mới.");
			}

			var assignment = _context.RoomServices.Find(roomId, serviceId);
			if (assignment == null)
			{
				assignment = new RoomService
				{
					RoomId = roomId,
					ServiceId = serviceId,
					IsActive = isActive
				};
				_context.RoomServices.Add(assignment);
			}
			else
			{
				assignment.IsActive = isActive;
			}
			AddAudit(managerId, isActive ? "AssignService" : "UnassignService",
				"RoomServices", roomId, null, $"Service={serviceId}");
			_context.SaveChanges();
		}

		private List<int> GetCreatedTenantIds(int managerId) =>
			_context.AuditLogs
				.AsNoTracking()
				.Where(a =>
					a.UserId == managerId &&
					a.TableName == "Tenants" &&
					a.Action == "Create" &&
					a.RecordId != null)
				.Select(a => a.RecordId!)
				.AsEnumerable()
				.Select(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : 0)
				.Where(id => id > 0)
				.Distinct()
				.ToList();

		private void EnsureManagerHasBuilding(int managerId)
		{
			if (!_context.Buildings.Any(b => b.ManagerId == managerId && b.IsActive))
			{
				throw new UnauthorizedAccessException("Quản lý chưa được phân công tòa nhà.");
			}
		}

		private void EnsureTenantAccess(int managerId, int tenantId)
		{
			bool linked = _context.ContractTenants.Any(ct =>
				ct.TenantId == tenantId &&
				ct.Contract.Room.Building.ManagerId == managerId);
			bool created = _context.AuditLogs.Any(a =>
				a.UserId == managerId &&
				a.TableName == "Tenants" &&
				a.Action == "Create" &&
				a.RecordId == tenantId.ToString(CultureInfo.InvariantCulture));
			if (!linked && !created)
			{
				throw new UnauthorizedAccessException("Bạn không có quyền thao tác khách thuê này.");
			}
		}

		private Room GetManagedRoom(int managerId, int roomId) =>
			_context.Rooms
				.Include(r => r.Building)
				.Include(r => r.RoomType)
				.SingleOrDefault(r => r.Id == roomId && r.Building.ManagerId == managerId)
				?? throw new UnauthorizedAccessException("Phòng không thuộc tòa nhà được phân công.");

		private Contract GetManagedContract(int managerId, int contractId) =>
			_context.Contracts
				.Include(c => c.Room)
					.ThenInclude(r => r.Building)
				.Include(c => c.Room)
					.ThenInclude(r => r.RoomType)
				.Include(c => c.ContractTenants)
				.SingleOrDefault(c => c.Id == contractId && c.Room.Building.ManagerId == managerId)
				?? throw new UnauthorizedAccessException("Hợp đồng không thuộc tòa nhà được phân công.");

		private void EnsureContractAccess(int managerId, int contractId)
		{
			if (!_context.Contracts.Any(c =>
				c.Id == contractId &&
				c.Room.Building.ManagerId == managerId))
			{
				throw new UnauthorizedAccessException("Hợp đồng không thuộc tòa nhà được phân công.");
			}
		}

		private void EnsureContractRoomAvailable(
			int roomId,
			DateOnly startDate,
			DateOnly endDate,
			int? excludingContractId)
		{
			if (_context.Contracts.Any(c =>
				c.RoomId == roomId &&
				c.Status == ActiveContract &&
				(!excludingContractId.HasValue || c.Id != excludingContractId.Value) &&
				c.StartDate <= endDate &&
				c.EndDate >= startDate))
			{
				throw new InvalidOperationException("Phòng đã có hợp đồng hoạt động trong khoảng thời gian này.");
			}
		}

		private void EnsureTenantAvailable(
			int tenantId,
			DateOnly startDate,
			DateOnly endDate,
			int? excludingContractId)
		{
			if (_context.ContractTenants.Any(ct =>
				ct.TenantId == tenantId &&
				ct.Contract.Status == ActiveContract &&
				(!excludingContractId.HasValue || ct.ContractId != excludingContractId.Value) &&
				ct.Contract.StartDate <= endDate &&
				ct.Contract.EndDate >= startDate))
			{
				throw new InvalidOperationException("Khách đã thuộc một hợp đồng hoạt động trong khoảng thời gian này.");
			}
		}

		private void EnsureUniqueInvoiceMonth(int contractId, DateTime invoiceDate, int? excludingInvoiceId)
		{
			DateTime monthStart = new(invoiceDate.Year, invoiceDate.Month, 1);
			DateTime nextMonth = monthStart.AddMonths(1);
			if (_context.Invoices.Any(i =>
				i.ContractId == contractId &&
				(!excludingInvoiceId.HasValue || i.Id != excludingInvoiceId.Value) &&
				i.InvoiceDate >= monthStart &&
				i.InvoiceDate < nextMonth))
			{
				throw new InvalidOperationException("Hợp đồng đã có hóa đơn trong tháng này.");
			}
		}

		private void RefreshRoomStatus(int roomId)
		{
			var room = _context.Rooms.Single(r => r.Id == roomId);
			if (!string.Equals(room.Status, "Maintenance", StringComparison.OrdinalIgnoreCase))
			{
				var today = DateOnly.FromDateTime(DateTime.Today);
				room.Status = _context.Contracts.Any(c =>
					c.RoomId == roomId &&
					c.Status == ActiveContract &&
					c.StartDate <= today &&
					c.EndDate >= today)
					? OccupiedRoom
					: VacantRoom;
				_context.SaveChanges();
			}
		}

		private void AddAudit(
			int managerId,
			string action,
			string tableName,
			int recordId,
			string? oldValue,
			string? newValue)
		{
			_context.AuditLogs.Add(new AuditLog
			{
				UserId = managerId,
				Action = action,
				TableName = tableName,
				RecordId = recordId.ToString(CultureInfo.InvariantCulture),
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}
	}
}
