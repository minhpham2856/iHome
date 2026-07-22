using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// Truy cập dữ liệu ghi cho vai trò Quản lý.
	public class ManagerOperationsRepository
	{
		private const string ActiveContract = "Đang hoạt động";
		private const string OccupiedRoom = "Đang ở";
		private const string EmptyRoom = "Trống";
		private readonly IHomeDbContext _context;

		public ManagerOperationsRepository()
		{
			_context = new IHomeDbContext();
		}

		// Khách thuê thuộc phạm vi quản lý (hợp đồng hoặc tự tạo qua audit).
		public List<Tenant> GetManageableTenants(int managerId)
		{
			var linkedIds = _context.ContractTenants
				.AsNoTracking()
				.Where(ct => ct.Contract.Room.Building.ManagerId == managerId)
				.Select(ct => ct.TenantId);
			// Phạm vi qua audit: khách do quản lý này tạo.
			var createdIds = GetCreatedTenantIds(managerId);

			return _context.Tenants
				.AsNoTracking()
				.Where(t => linkedIds.Contains(t.Id) || createdIds.Contains(t.Id))
				.OrderBy(t => t.FullName)
				.ToList();
		}

		// Khách do quản lý tạo nhưng chưa gắn hợp đồng trong tòa được phân công.
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

		// Tạo khách thuê và ghi audit.
		public Tenant CreateTenant(int managerId, Tenant tenant)
		{
			EnsureManagerHasBuilding(managerId);
			using var transaction = _context.Database.BeginTransaction();
			_context.Tenants.Add(tenant);
			_context.SaveChanges();
			// Ghi audit tạo khách (họ tên, CCCD, SĐT).
			AddAudit(managerId, "Tạo mới", "Khách thuê", tenant.Id, tenant.FullName, null,
				FormatLines(new Dictionary<string, string?>
				{
					["Họ tên"] = tenant.FullName,
					["CCCD"] = tenant.IdCardNumber,
					["Số điện thoại"] = tenant.PhoneNumber
				}));
			_context.SaveChanges();
			transaction.Commit();
			return tenant;
		}

		// Cập nhật khách thuê; audit chỉ các trường đổi.
		public void UpdateTenant(int managerId, Tenant changes)
		{
			EnsureTenantAccess(managerId, changes.Id);
			var tenant = _context.Tenants.Single(t => t.Id == changes.Id);
			// Snapshot trước/sau để DiffChanged ghi audit.
			var before = new Dictionary<string, string?>
			{
				["Họ tên"] = tenant.FullName,
				["CCCD"] = tenant.IdCardNumber,
				["Số điện thoại"] = tenant.PhoneNumber,
				["Email"] = tenant.Email
			};
			tenant.FullName = changes.FullName;
			tenant.DateOfBirth = changes.DateOfBirth;
			tenant.IdCardNumber = changes.IdCardNumber;
			tenant.PhoneNumber = changes.PhoneNumber;
			tenant.Email = changes.Email;
			tenant.PermanentAddress = changes.PermanentAddress;
			var after = new Dictionary<string, string?>
			{
				["Họ tên"] = tenant.FullName,
				["CCCD"] = tenant.IdCardNumber,
				["Số điện thoại"] = tenant.PhoneNumber,
				["Email"] = tenant.Email
			};
			var (oldValue, newValue) = DiffChanged(before, after);
			AddAudit(managerId, "Cập nhật", "Khách thuê", tenant.Id, tenant.FullName, oldValue, newValue);
			_context.SaveChanges();
		}

		// Xóa khách khi không còn liên kết hợp đồng; ghi audit.
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
			AddAudit(managerId, "Xóa", "Khách thuê", tenant.Id, tenant.FullName,
				FormatLines(new Dictionary<string, string?>
				{
					["Họ tên"] = tenant.FullName,
					["CCCD"] = tenant.IdCardNumber
				}), null);
			_context.SaveChanges();
			transaction.Commit();
		}

		// Kiểm tra trùng số CCCD (có thể loại trừ một khách).
		public bool IdCardExists(string idCardNumber, int? excludingTenantId = null)
		{
			return _context.Tenants.Any(t =>
				t.IdCardNumber == idCardNumber &&
				(!excludingTenantId.HasValue || t.Id != excludingTenantId.Value));
		}

		// Danh sách hợp đồng trong tòa do quản lý phụ trách.
		public List<Contract> GetContracts(int managerId, int? buildingId = null)
		{
			return _context.Contracts
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
		}

		// Tạo hợp đồng: 1..MaxOccupancy khách đứng tên (MaxOccupancy là trần, không bắt buộc đủ).
		// Tiền thuê/cọc là một khoản cho cả hợp đồng, không chia theo khách.
		public Contract CreateContract(
			int managerId,
			Contract contract,
			int mainTenantId,
			IReadOnlyCollection<int>? coTenantIds = null)
		{
			var room = GetManagedRoom(managerId, contract.RoomId);
			EnsureTenantAccess(managerId, mainTenantId);
			if (string.Equals(room.Status, "Bảo trì", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Không thể tạo hợp đồng cho phòng đang bảo trì.");
			}

			// Danh sách khách: người thuê chính trước, rồi đồng thuê (không trùng).
			var tenantIds = new List<int> { mainTenantId };
			if (coTenantIds != null)
			{
				tenantIds.AddRange(coTenantIds.Where(id => id > 0 && id != mainTenantId).Distinct());
			}

			// MaxOccupancy: ít nhất 1, nhiều nhất MaxOccupancy.
			int maxOccupancy = room.RoomType.MaxOccupancy;
			if (maxOccupancy < 1)
			{
				throw new InvalidOperationException("Phòng không còn sức chứa cho khách thuê.");
			}
			if (tenantIds.Count < 1 || tenantIds.Count > maxOccupancy)
			{
				throw new InvalidOperationException(
					$"Số khách đứng tên phải từ 1 đến {maxOccupancy} (sức chứa tối đa của loại phòng).");
			}

			// Hợp đồng hoạt động: kiểm tra chồng ngày phòng và từng khách.
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
			// Index 0 = người thuê chính (IsMainTenant = true).
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
			// Làm mới trạng thái phòng nếu hợp đồng hoạt động bao phủ hôm nay.
			if (contract.Status == ActiveContract && contract.StartDate <= today && contract.EndDate >= today)
			{
				room.Status = OccupiedRoom;
			}
			AddAudit(managerId, "Tạo mới", "Hợp đồng", contract.Id, room.RoomNumber, null,
				FormatLines(new Dictionary<string, string?>
				{
					["Phòng"] = contract.RoomId.ToString(CultureInfo.InvariantCulture),
					["Khách"] = string.Join(',', tenantIds),
					["Ngày bắt đầu"] = contract.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
					["Ngày kết thúc"] = contract.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
					["Tiền thuê"] = contract.MonthlyRent.ToString("N0", CultureInfo.InvariantCulture)
				}));
			_context.SaveChanges();
			transaction.Commit();
			return contract;
		}

		// Cập nhật hợp đồng; kiểm tra chồng ngày rồi làm mới trạng thái phòng.
		public void UpdateContract(int managerId, Contract changes)
		{
			var contract = _context.Contracts
				.Include(c => c.Room)
				.Single(c => c.Id == changes.Id && c.Room.Building.ManagerId == managerId);
			// Khi chuyển sang hoạt động: phòng không được chồng khoảng ngày với HĐ khác.
			if (changes.Status == ActiveContract)
			{
				EnsureContractRoomAvailable(contract.RoomId, changes.StartDate, changes.EndDate, contract.Id);
			}
			var before = new Dictionary<string, string?>
			{
				["Ngày bắt đầu"] = contract.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Ngày kết thúc"] = contract.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Tiền thuê"] = contract.MonthlyRent.ToString("N0", CultureInfo.InvariantCulture),
				["Tiền cọc"] = contract.DepositAmount.ToString("N0", CultureInfo.InvariantCulture),
				["Trạng thái"] = FormatContractStatus(contract.Status),
				["Ghi chú"] = contract.Notes
			};
			contract.StartDate = changes.StartDate;
			contract.EndDate = changes.EndDate;
			contract.MonthlyRent = changes.MonthlyRent;
			contract.DepositAmount = changes.DepositAmount;
			contract.Status = changes.Status;
			contract.Notes = changes.Notes;
			var after = new Dictionary<string, string?>
			{
				["Ngày bắt đầu"] = contract.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Ngày kết thúc"] = contract.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Tiền thuê"] = contract.MonthlyRent.ToString("N0", CultureInfo.InvariantCulture),
				["Tiền cọc"] = contract.DepositAmount.ToString("N0", CultureInfo.InvariantCulture),
				["Trạng thái"] = FormatContractStatus(contract.Status),
				["Ghi chú"] = contract.Notes
			};
			var (oldValue, newValue) = DiffChanged(before, after);
			AddAudit(managerId, "Cập nhật", "Hợp đồng", contract.Id, contract.Room.RoomNumber, oldValue, newValue);
			_context.SaveChanges();
			// Làm mới trạng thái phòng theo hợp đồng hoạt động hôm nay.
			RefreshRoomStatus(contract.RoomId);
		}

		// Xóa hợp đồng khi chưa có hóa đơn; làm mới trạng thái phòng.
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
			AddAudit(managerId, "Xóa", "Hợp đồng", contract.Id, contract.Room.RoomNumber,
				FormatLines(new Dictionary<string, string?>
				{
					["Phòng"] = roomId.ToString(CultureInfo.InvariantCulture)
				}), null);
			_context.SaveChanges();
			// Làm mới trạng thái phòng sau khi xóa hợp đồng.
			RefreshRoomStatus(roomId);
			transaction.Commit();
		}

		// Gán / cập nhật khách trên hợp đồng (MaxOccupancy là trần).
		public void AssignTenant(int managerId, int contractId, int tenantId, bool isMainTenant)
		{
			var contract = GetManagedContract(managerId, contractId);
			EnsureTenantAccess(managerId, tenantId);
			// Không cho khách chồng khoảng ngày với hợp đồng hoạt động khác.
			EnsureTenantAvailable(tenantId, contract.StartDate, contract.EndDate, contract.Id);
			var existing = contract.ContractTenants.SingleOrDefault(ct => ct.TenantId == tenantId);
			if (existing?.IsMainTenant == true && !isMainTenant)
			{
				throw new InvalidOperationException("Hợp đồng phải có người thuê chính. Hãy chọn khách khác làm người thuê chính trước.");
			}
			// MaxOccupancy: không thêm khách mới khi đã đủ trần.
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
			string tenantName = _context.Tenants
				.Where(t => t.Id == tenantId)
				.Select(t => t.FullName)
				.FirstOrDefault() ?? tenantId.ToString(CultureInfo.InvariantCulture);
			string detail = $"{contract.Room.RoomNumber} · {tenantName}";
			AddAudit(managerId, "Gán khách", "Khách trên hợp đồng", contractId, detail, null,
				FormatLines(new Dictionary<string, string?>
				{
					["Khách"] = tenantId.ToString(CultureInfo.InvariantCulture),
					["Người thuê chính"] = isMainTenant ? "Có" : "Không"
				}));
			_context.SaveChanges();
			transaction.Commit();
		}

		// Gỡ khách khỏi hợp đồng (không gỡ người thuê chính / khách cuối).
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

			string tenantName = link.Tenant?.FullName
				?? _context.Tenants.Where(t => t.Id == tenantId).Select(t => t.FullName).FirstOrDefault()
				?? tenantId.ToString(CultureInfo.InvariantCulture);
			string detail = $"{contract.Room.RoomNumber} · {tenantName}";
			_context.ContractTenants.Remove(link);
			AddAudit(managerId, "Gỡ khách", "Khách trên hợp đồng", contractId, detail,
				FormatLines(new Dictionary<string, string?>
				{
					["Khách"] = tenantId.ToString(CultureInfo.InvariantCulture)
				}), null);
			_context.SaveChanges();
		}

		// Danh sách hóa đơn trong tòa do quản lý phụ trách.
		public List<Invoice> GetInvoices(int managerId, int? buildingId = null)
		{
			return _context.Invoices
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
		}

		// Hợp đồng kèm dữ liệu phục vụ lập hóa đơn.
		public Contract GetInvoiceBillingContract(int managerId, int contractId)
		{
			return _context.Contracts
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
		}

		// Tạo hóa đơn + dòng + chỉ số; một HĐ tối đa một hóa đơn / tháng; ghi audit.
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
			string roomNumber = _context.Contracts
				.Where(c => c.Id == invoice.ContractId)
				.Select(c => c.Room.RoomNumber)
				.FirstOrDefault()
				?? invoice.ContractId.ToString(CultureInfo.InvariantCulture);
			string detail = $"{roomNumber} · {invoice.InvoiceDate.ToString("MM/yyyy", CultureInfo.InvariantCulture)}";
			AddAudit(managerId, "Tạo mới", "Hóa đơn", invoice.Id, detail, null,
				FormatLines(new Dictionary<string, string?>
				{
					["Hợp đồng"] = invoice.ContractId.ToString(CultureInfo.InvariantCulture),
					["Tổng tiền"] = invoice.TotalAmount.ToString("N0", CultureInfo.InvariantCulture),
					["Số mục"] = items.Count.ToString(CultureInfo.InvariantCulture)
				}));
			_context.SaveChanges();
			transaction.Commit();
			return invoice;
		}

		// Cập nhật hạn/trạng thái hóa đơn; audit các trường đổi.
		public void UpdateInvoice(int managerId, Invoice changes)
		{
			var invoice = _context.Invoices
				.Include(i => i.Contract)
					.ThenInclude(c => c.Room)
				.Include(i => i.InvoiceItems)
				.Single(i => i.Id == changes.Id && i.Contract.Room.Building.ManagerId == managerId);
			var before = new Dictionary<string, string?>
			{
				["Ngày hóa đơn"] = invoice.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Hạn thanh toán"] = invoice.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Tổng tiền"] = invoice.TotalAmount.ToString("N0", CultureInfo.InvariantCulture),
				["Trạng thái"] = invoice.Status
			};
			invoice.DueDate = changes.DueDate;
			invoice.Status = changes.Status;
			var after = new Dictionary<string, string?>
			{
				["Ngày hóa đơn"] = invoice.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Hạn thanh toán"] = invoice.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Tổng tiền"] = invoice.TotalAmount.ToString("N0", CultureInfo.InvariantCulture),
				["Trạng thái"] = invoice.Status
			};
			var (oldValue, newValue) = DiffChanged(before, after);
			AddAudit(managerId, "Cập nhật", "Hóa đơn", invoice.Id, invoice.Contract.Room.RoomNumber, oldValue, newValue);
			_context.SaveChanges();
		}

		// Xóa hóa đơn khi chưa có thanh toán; ghi audit.
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
			AddAudit(managerId, "Xóa", "Hóa đơn", invoice.Id, invoice.Contract.Room.RoomNumber,
				FormatLines(new Dictionary<string, string?>
				{
					["Tổng tiền"] = invoice.TotalAmount.ToString("N0", CultureInfo.InvariantCulture)
				}), null);
			_context.SaveChanges();
			transaction.Commit();
		}

		// Gán dịch vụ theo phòng trong tòa được phân công.
		public List<RoomService> GetRoomServices(int managerId, int? buildingId = null)
		{
			return _context.RoomServices
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
		}

		// Bật/tắt gán dịch vụ phòng; ghi audit.
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
			AddAudit(managerId, isActive ? "Gán dịch vụ" : "Bỏ gán dịch vụ",
				"Dịch vụ phòng", roomId, $"{room.RoomNumber} · {service.ServiceName}", null,
				FormatLines(new Dictionary<string, string?>
				{
					["Dịch vụ"] = serviceId.ToString(CultureInfo.InvariantCulture)
				}));
			_context.SaveChanges();
		}

		// Id khách do quản lý tạo — đọc từ audit (Tenants / Khách thuê + Create / Tạo mới).
		private List<int> GetCreatedTenantIds(int managerId)
		{
			return _context.AuditLogs
				.AsNoTracking()
				.Where(a =>
					a.UserId == managerId &&
					(a.TableName == "Tenants" || a.TableName == "Khách thuê") &&
					(a.Action == "Create" || a.Action == "Tạo mới") &&
					a.RecordId != null)
				.Select(a => a.RecordId!)
				.AsEnumerable()
				.Select(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : 0)
				.Where(id => id > 0)
				.Distinct()
				.ToList();
		}

		private void EnsureManagerHasBuilding(int managerId)
		{
			if (!_context.Buildings.Any(b => b.ManagerId == managerId && b.IsActive))
			{
				throw new UnauthorizedAccessException("Quản lý chưa được phân công tòa nhà.");
			}
		}

		// Phạm vi khách: liên kết hợp đồng trong tòa QL hoặc audit tạo khách.
		private void EnsureTenantAccess(int managerId, int tenantId)
		{
			bool linked = _context.ContractTenants.Any(ct =>
				ct.TenantId == tenantId &&
				ct.Contract.Room.Building.ManagerId == managerId);
			bool created = _context.AuditLogs.Any(a =>
				a.UserId == managerId &&
				(a.TableName == "Tenants" || a.TableName == "Khách thuê") &&
				(a.Action == "Create" || a.Action == "Tạo mới") &&
				a.RecordId == tenantId.ToString(CultureInfo.InvariantCulture));
			if (!linked && !created)
			{
				throw new UnauthorizedAccessException("Bạn không có quyền thao tác khách thuê này.");
			}
		}

		private Room GetManagedRoom(int managerId, int roomId)
		{
			return _context.Rooms
				.Include(r => r.Building)
				.Include(r => r.RoomType)
				.SingleOrDefault(r => r.Id == roomId && r.Building.ManagerId == managerId)
				?? throw new UnauthorizedAccessException("Phòng không thuộc tòa nhà được phân công.");
		}

		private Contract GetManagedContract(int managerId, int contractId)
		{
			return _context.Contracts
				.Include(c => c.Room)
					.ThenInclude(r => r.Building)
				.Include(c => c.Room)
					.ThenInclude(r => r.RoomType)
				.Include(c => c.ContractTenants)
				.SingleOrDefault(c => c.Id == contractId && c.Room.Building.ManagerId == managerId)
				?? throw new UnauthorizedAccessException("Hợp đồng không thuộc tòa nhà được phân công.");
		}

		private void EnsureContractAccess(int managerId, int contractId)
		{
			if (!_context.Contracts.Any(c =>
				c.Id == contractId &&
				c.Room.Building.ManagerId == managerId))
			{
				throw new UnauthorizedAccessException("Hợp đồng không thuộc tòa nhà được phân công.");
			}
		}

		// Chồng khoảng ngày: StartA <= EndB && EndA >= StartB trên hợp đồng hoạt động cùng phòng.
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

		// Chồng khoảng ngày: khách đã thuộc HĐ hoạt động khác trong cùng khoảng.
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

		// Làm mới Trống/Đang ở theo HĐ hoạt động bao phủ hôm nay (bỏ qua Bảo trì).
		private void RefreshRoomStatus(int roomId)
		{
			var room = _context.Rooms.Single(r => r.Id == roomId);
			if (!string.Equals(room.Status, "Bảo trì", StringComparison.OrdinalIgnoreCase))
			{
				var today = DateOnly.FromDateTime(DateTime.Today);
				room.Status = _context.Contracts.Any(c =>
					c.RoomId == roomId &&
					c.Status == ActiveContract &&
					c.StartDate <= today &&
					c.EndDate >= today)
					? OccupiedRoom
					: EmptyRoom;
				_context.SaveChanges();
			}
		}

		// Ghi một dòng audit (hành động quản lý + Old/New tùy chọn).
		private void AddAudit(
			int managerId,
			string action,
			string tableName,
			int recordId,
			string? detail,
			string? oldValue,
			string? newValue)
		{
			_context.AuditLogs.Add(new AuditLog
			{
				UserId = managerId,
				Action = action,
				TableName = tableName,
				RecordId = recordId.ToString(CultureInfo.InvariantCulture),
				Detail = detail,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}

		// Nhãn tiếng Việt (khóa AuditField) — DAL không tham chiếu enum BLL.
		private static (string? OldValue, string? NewValue) DiffChanged(
			IReadOnlyDictionary<string, string?> before,
			IReadOnlyDictionary<string, string?> after)
		{
			var oldLines = new List<string>();
			var newLines = new List<string>();
			foreach (string key in before.Keys.Union(after.Keys).Distinct())
			{
				before.TryGetValue(key, out string? oldRaw);
				after.TryGetValue(key, out string? newRaw);
				string oldNorm = string.IsNullOrWhiteSpace(oldRaw) ? string.Empty : oldRaw.Trim();
				string newNorm = string.IsNullOrWhiteSpace(newRaw) ? string.Empty : newRaw.Trim();
				if (oldNorm == newNorm) continue;
				oldLines.Add($"{key}: {(string.IsNullOrWhiteSpace(oldRaw) ? "—" : oldRaw.Trim())}");
				newLines.Add($"{key}: {(string.IsNullOrWhiteSpace(newRaw) ? "—" : newRaw.Trim())}");
			}
			return (
				oldLines.Count == 0 ? null : string.Join("\n", oldLines),
				newLines.Count == 0 ? null : string.Join("\n", newLines));
		}

		private static string? FormatLines(IReadOnlyDictionary<string, string?> values)
		{
			var lines = values
				.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
				.Select(kv => $"{kv.Key}: {kv.Value!.Trim()}")
				.ToList();
			return lines.Count == 0 ? null : string.Join("\n", lines);
		}

		private static string FormatContractStatus(string status)
		{
			return status;
		}
	}
}
