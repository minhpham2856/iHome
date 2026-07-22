using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Manager write operations.
	public class ManagerOperationsRepository
	{
		private const string ActiveContract = "Đang hoạt động";
		private const string OccupiedRoom = "Đang ở";
		private const string EmptyRoom = "Trống";
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public ManagerOperationsRepository()
		{
			// Instantiate a dedicated DbContext for manager write operations and transactional workflows
			_context = new IHomeDbContext();
		}

		// Query ManageableTenants records.
		public List<Tenant> GetManageableTenants(int managerId)
		{
			// Collect tenant ids already linked to contracts in buildings managed by this user
			var linkedIds = _context.ContractTenants
				.AsNoTracking()
				.Where(ct => ct.Contract.Room.Building.ManagerId == managerId)
				.Select(ct => ct.TenantId);
			// Collect tenant ids this manager created according to audit log history
			var createdIds = GetCreatedTenantIds(managerId);

			// Return tenants the manager may manage: contract-linked or self-created, sorted by name
			return _context.Tenants
				.AsNoTracking()
				.Where(t => linkedIds.Contains(t.Id) || createdIds.Contains(t.Id))
				.OrderBy(t => t.FullName)
				.ToList();
		}

		// Query UnassignedManagerTenants records.
		public List<Tenant> GetUnassignedManagerTenants(int managerId)
		{
			// Resolve tenant ids created by this manager from audit logs
			var createdIds = GetCreatedTenantIds(managerId);
			// Return created tenants not yet assigned to any contract in the manager's buildings
			return _context.Tenants
				.AsNoTracking()
				.Where(t =>
					createdIds.Contains(t.Id) &&
					!t.ContractTenants.Any(ct => ct.Contract.Room.Building.ManagerId == managerId))
				.OrderBy(t => t.FullName)
				.ToList();
		}

		// CreateTenant — public entry point.
		public Tenant CreateTenant(int managerId, Tenant tenant)
		{
			// Verify the manager is assigned to at least one active building before creating tenants
			EnsureManagerHasBuilding(managerId);
			// Wrap tenant insert and audit logging in one database transaction
			using var transaction = _context.Database.BeginTransaction();
			// Stage the new tenant row for insert so identity id is generated on save
			_context.Tenants.Add(tenant);
			// Persist the tenant row and populate tenant.Id from the database identity
			_context.SaveChanges();
			// Record an audit log entry describing the newly created tenant profile fields
			AddAudit(managerId, "Tạo mới", "Khách thuê", tenant.Id, tenant.FullName, null,
				FormatLines(new Dictionary<string, string?>
				{
					["Họ tên"] = tenant.FullName,
					["CCCD"] = tenant.IdCardNumber,
					["Số điện thoại"] = tenant.PhoneNumber
				}));
			// Commit the audit row together with the tenant insert
			_context.SaveChanges();
			// Finalize the transaction after tenant and audit rows are both saved
			transaction.Commit();
			return tenant;
		}

		// Persist changes to an existing record.
		public void UpdateTenant(int managerId, Tenant changes)
		{
			// Ensure this manager is allowed to modify the target tenant before loading data
			EnsureTenantAccess(managerId, changes.Id);
			// Load the tracked tenant row that will receive field updates
			var tenant = _context.Tenants.Single(t => t.Id == changes.Id);
			// Snapshot key profile fields before mutation for audit diff generation
			var before = new Dictionary<string, string?>
			{
				["Họ tên"] = tenant.FullName,
				["CCCD"] = tenant.IdCardNumber,
				["Số điện thoại"] = tenant.PhoneNumber,
				["Email"] = tenant.Email
			};
			// Map incoming tenant profile values onto the tracked database row
			tenant.FullName = changes.FullName;
			tenant.DateOfBirth = changes.DateOfBirth;
			tenant.IdCardNumber = changes.IdCardNumber;
			tenant.PhoneNumber = changes.PhoneNumber;
			tenant.Email = changes.Email;
			tenant.PermanentAddress = changes.PermanentAddress;
			// Snapshot the same fields after mutation for audit diff generation
			var after = new Dictionary<string, string?>
			{
				["Họ tên"] = tenant.FullName,
				["CCCD"] = tenant.IdCardNumber,
				["Số điện thoại"] = tenant.PhoneNumber,
				["Email"] = tenant.Email
			};
			// Build old/new audit text only for fields that actually changed
			var (oldValue, newValue) = DiffChanged(before, after);
			// Stage an audit log describing the tenant update with field-level diffs
			AddAudit(managerId, "Cập nhật", "Khách thuê", tenant.Id, tenant.FullName, oldValue, newValue);
			// Commit tenant updates and the audit row together
			_context.SaveChanges();
		}

		// DeleteTenant when allowed by rules.
		public void DeleteTenant(int managerId, int tenantId)
		{
			// Ensure this manager is allowed to delete the target tenant
			EnsureTenantAccess(managerId, tenantId);
			// Load the tenant with contract links to enforce delete safety rules
			var tenant = _context.Tenants
				.Include(t => t.ContractTenants)
				.Single(t => t.Id == tenantId);
			// Block delete when the tenant still appears on any contract
			if (tenant.ContractTenants.Count > 0)
			{
				throw new InvalidOperationException("Khách đang có lịch sử hợp đồng. Hãy gỡ khách khỏi hợp đồng trước khi xóa.");
			}

			// Wrap tenant delete and audit logging in one database transaction
			using var transaction = _context.Database.BeginTransaction();
			// Stage the tenant row for hard delete
			_context.Tenants.Remove(tenant);
			// Record an audit log capturing the deleted tenant identity fields
			AddAudit(managerId, "Xóa", "Khách thuê", tenant.Id, tenant.FullName,
				FormatLines(new Dictionary<string, string?>
				{
					["Họ tên"] = tenant.FullName,
					["CCCD"] = tenant.IdCardNumber
				}), null);
			// Commit tenant deletion and audit row together
			_context.SaveChanges();
			// Finalize the transaction after both operations succeed
			transaction.Commit();
		}

		// IdCardExists — public entry point.
		public bool IdCardExists(string idCardNumber, int? excludingTenantId = null)
		{
			// Return true when another tenant already uses this national id card number
			return _context.Tenants.Any(t =>
				t.IdCardNumber == idCardNumber &&
				(!excludingTenantId.HasValue || t.Id != excludingTenantId.Value));
		}

		// Query Contracts records.
		public List<Contract> GetContracts(int managerId, int? buildingId = null)
		{
			// Load contracts for rooms in buildings managed by this user with room, building, and tenant links
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

		// Create contract: named tenant count must equal room MaxOccupancy (e.g. double room = 2)
		// Rent/deposit are one amount for the whole contract, not split per tenant
		public Contract CreateContract(
			int managerId,
			Contract contract,
			int mainTenantId,
			IReadOnlyCollection<int>? coTenantIds = null)
		{
			// Load and authorize the target room including building and room type metadata
			var room = GetManagedRoom(managerId, contract.RoomId);
			// Verify the main tenant is accessible to this manager
			EnsureTenantAccess(managerId, mainTenantId);
			// Reject contract creation when the room is in maintenance status
			if (string.Equals(room.Status, "Bảo trì", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Không thể tạo hợp đồng cho phòng đang bảo trì.");
			}

			// Named tenant list: main tenant first, then co-tenants (no duplicates)
			var tenantIds = new List<int> { mainTenantId };
			if (coTenantIds != null)
			{
				// Append valid co-tenant ids excluding duplicates and the main tenant id
				tenantIds.AddRange(coTenantIds.Where(id => id > 0 && id != mainTenantId).Distinct());
			}

			// Read the room type occupancy limit that governs how many named tenants are required
			int maxOccupancy = room.RoomType.MaxOccupancy;
			if (maxOccupancy < 1)
			{
				throw new InvalidOperationException("Phòng không còn sức chứa cho khách thuê.");
			}
			// Enforce that the named tenant count exactly matches the room capacity rule
			if (tenantIds.Count != maxOccupancy)
			{
				throw new InvalidOperationException(
					$"Hợp đồng phải có đủ {maxOccupancy} khách đứng tên theo sức chứa phòng.");
			}

			// When creating an active contract, validate room and tenant availability for the date range
			if (contract.Status == ActiveContract)
			{
				EnsureContractRoomAvailable(contract.RoomId, contract.StartDate, contract.EndDate, null);
				foreach (int tenantId in tenantIds)
				{
					EnsureTenantAccess(managerId, tenantId);
					EnsureTenantAvailable(tenantId, contract.StartDate, contract.EndDate, null);
				}
			}

			// Wrap contract insert, tenant links, room status update, and audit in one transaction
			using var transaction = _context.Database.BeginTransaction();
			// Stage the contract row for insert so identity id is generated on first save
			_context.Contracts.Add(contract);
			// Persist the contract row and populate contract.Id from the database identity
			_context.SaveChanges();
			// Index 0 = main tenant (IsMainTenant = true)
			for (int index = 0; index < tenantIds.Count; index++)
			{
				// Insert one contract-tenant junction row per named tenant with main-tenant flag by index
				_context.ContractTenants.Add(new ContractTenant
				{
					ContractId = contract.Id,
					TenantId = tenantIds[index],
					IsMainTenant = index == 0
				});
			}
			// Capture today's calendar date for immediate occupancy evaluation
			var today = DateOnly.FromDateTime(DateTime.Today);
			// Mark the room occupied when the new contract is active and covers today
			if (contract.Status == ActiveContract && contract.StartDate <= today && contract.EndDate >= today)
			{
				room.Status = OccupiedRoom;
			}
			// Record an audit log summarizing the created contract and named tenants
			AddAudit(managerId, "Tạo mới", "Hợp đồng", contract.Id, room.RoomNumber, null,
				FormatLines(new Dictionary<string, string?>
				{
					["Phòng"] = contract.RoomId.ToString(CultureInfo.InvariantCulture),
					["Khách"] = string.Join(',', tenantIds),
					["Ngày bắt đầu"] = contract.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
					["Ngày kết thúc"] = contract.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
					["Tiền thuê"] = contract.MonthlyRent.ToString("N0", CultureInfo.InvariantCulture)
				}));
			// Commit contract links, room status change, and audit row together
			_context.SaveChanges();
			// Finalize the transaction after all related writes succeed
			transaction.Commit();
			return contract;
		}

		// Persist changes to an existing record.
		public void UpdateContract(int managerId, Contract changes)
		{
			// Load the managed contract with its room and enforce manager ownership in the same query
			var contract = _context.Contracts
				.Include(c => c.Room)
				.Single(c => c.Id == changes.Id && c.Room.Building.ManagerId == managerId);
			// When the updated contract will be active, ensure the room has no overlapping active contract
			if (changes.Status == ActiveContract)
			{
				EnsureContractRoomAvailable(contract.RoomId, changes.StartDate, changes.EndDate, contract.Id);
			}
			// Snapshot editable contract fields before mutation for audit diff generation
			var before = new Dictionary<string, string?>
			{
				["Ngày bắt đầu"] = contract.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Ngày kết thúc"] = contract.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Tiền thuê"] = contract.MonthlyRent.ToString("N0", CultureInfo.InvariantCulture),
				["Tiền cọc"] = contract.DepositAmount.ToString("N0", CultureInfo.InvariantCulture),
				["Trạng thái"] = FormatContractStatus(contract.Status),
				["Ghi chú"] = contract.Notes
			};
			// Map incoming contract values onto the tracked database row
			contract.StartDate = changes.StartDate;
			contract.EndDate = changes.EndDate;
			contract.MonthlyRent = changes.MonthlyRent;
			contract.DepositAmount = changes.DepositAmount;
			contract.Status = changes.Status;
			contract.Notes = changes.Notes;
			// Snapshot the same fields after mutation for audit diff generation
			var after = new Dictionary<string, string?>
			{
				["Ngày bắt đầu"] = contract.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Ngày kết thúc"] = contract.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Tiền thuê"] = contract.MonthlyRent.ToString("N0", CultureInfo.InvariantCulture),
				["Tiền cọc"] = contract.DepositAmount.ToString("N0", CultureInfo.InvariantCulture),
				["Trạng thái"] = FormatContractStatus(contract.Status),
				["Ghi chú"] = contract.Notes
			};
			// Build old/new audit text only for fields that actually changed
			var (oldValue, newValue) = DiffChanged(before, after);
			// Stage an audit log describing the contract update with field-level diffs
			AddAudit(managerId, "Cập nhật", "Hợp đồng", contract.Id, contract.Room.RoomNumber, oldValue, newValue);
			// Commit contract field updates and the audit row together
			_context.SaveChanges();
			// Recompute the room occupancy status based on active contracts as of today
			RefreshRoomStatus(contract.RoomId);
		}

		// DeleteContract when allowed by rules.
		public void DeleteContract(int managerId, int contractId)
		{
			// Load the managed contract with room, tenant links, and invoices for delete validation
			var contract = _context.Contracts
				.Include(c => c.Room)
				.Include(c => c.ContractTenants)
				.Include(c => c.Invoices)
				.Single(c => c.Id == contractId && c.Room.Building.ManagerId == managerId);
			// Block delete when billing history already exists for this contract
			if (contract.Invoices.Count > 0)
			{
				throw new InvalidOperationException("Không thể xóa hợp đồng đã phát sinh hóa đơn. Hãy chuyển trạng thái sang chấm dứt.");
			}

			// Remember room id before the contract row is removed so status can be refreshed afterward
			int roomId = contract.RoomId;
			// Wrap contract delete, junction cleanup, audit logging, and room refresh in one transaction
			using var transaction = _context.Database.BeginTransaction();
			// Remove all contract-tenant junction rows tied to this contract
			_context.ContractTenants.RemoveRange(contract.ContractTenants);
			// Remove the contract row itself
			_context.Contracts.Remove(contract);
			// Record an audit log describing the deleted contract and affected room
			AddAudit(managerId, "Xóa", "Hợp đồng", contract.Id, contract.Room.RoomNumber,
				FormatLines(new Dictionary<string, string?>
				{
					["Phòng"] = roomId.ToString(CultureInfo.InvariantCulture)
				}), null);
			// Commit contract and junction deletions plus the audit row
			_context.SaveChanges();
			// Recompute room occupancy after the contract is removed
			RefreshRoomStatus(roomId);
			// Finalize the transaction after delete and room refresh succeed
			transaction.Commit();
		}

		// AssignTenant — public entry point.
		public void AssignTenant(int managerId, int contractId, int tenantId, bool isMainTenant)
		{
			// Load the managed contract with room, room type, and current tenant membership
			var contract = GetManagedContract(managerId, contractId);
			// Verify the tenant is accessible to this manager
			EnsureTenantAccess(managerId, tenantId);
			// Ensure the tenant has no overlapping active contract in the proposed date range
			EnsureTenantAvailable(tenantId, contract.StartDate, contract.EndDate, contract.Id);
			// Look for an existing contract-tenant link for this tenant on the contract
			var existing = contract.ContractTenants.SingleOrDefault(ct => ct.TenantId == tenantId);
			// Prevent demoting the only main tenant without choosing a replacement first
			if (existing?.IsMainTenant == true && !isMainTenant)
			{
				throw new InvalidOperationException("Hợp đồng phải có người thuê chính. Hãy chọn khách khác làm người thuê chính trước.");
			}
			// Prevent adding a new tenant when the room has reached MaxOccupancy
			if (existing == null && contract.ContractTenants.Count >= contract.Room.RoomType.MaxOccupancy)
			{
				throw new InvalidOperationException("Phòng đã đạt số người tối đa.");
			}

			// Wrap tenant assignment updates and audit logging in one transaction
			using var transaction = _context.Database.BeginTransaction();
			if (isMainTenant)
			{
				// Clear main-tenant flag on every current member before promoting the selected tenant
				foreach (var member in contract.ContractTenants)
				{
					member.IsMainTenant = false;
				}
			}
			if (existing == null)
			{
				// Insert a new contract-tenant junction row when the tenant is not yet on the contract
				_context.ContractTenants.Add(new ContractTenant
				{
					ContractId = contractId,
					TenantId = tenantId,
					IsMainTenant = isMainTenant
				});
			}
			else
			{
				// Update the main-tenant flag on an existing junction row
				existing.IsMainTenant = isMainTenant;
			}
			// Resolve tenant display name for audit detail text
			string tenantName = _context.Tenants
				.Where(t => t.Id == tenantId)
				.Select(t => t.FullName)
				.FirstOrDefault() ?? tenantId.ToString(CultureInfo.InvariantCulture);
			// Build a human-readable audit detail combining room number and tenant name
			string detail = $"{contract.Room.RoomNumber} · {tenantName}";
			// Stage an audit log describing the tenant assignment action
			AddAudit(managerId, "Gán khách", "Khách trên hợp đồng", contractId, detail, null,
				FormatLines(new Dictionary<string, string?>
				{
					["Khách"] = tenantId.ToString(CultureInfo.InvariantCulture),
					["Người thuê chính"] = isMainTenant ? "Có" : "Không"
				}));
			// Commit assignment changes and the audit row together
			_context.SaveChanges();
			// Finalize the transaction after assignment succeeds
			transaction.Commit();
		}

		// RemoveTenant — public entry point.
		public void RemoveTenant(int managerId, int contractId, int tenantId)
		{
			// Load the managed contract with room, room type, and current tenant membership
			var contract = GetManagedContract(managerId, contractId);
			// Locate the contract-tenant junction row or fail when the tenant is not on this contract
			var link = contract.ContractTenants.SingleOrDefault(ct => ct.TenantId == tenantId)
				?? throw new InvalidOperationException("Khách không thuộc hợp đồng đã chọn.");
			// Prevent removing the last remaining tenant from the contract
			if (contract.ContractTenants.Count == 1)
			{
				throw new InvalidOperationException("Hợp đồng phải có ít nhất một khách thuê.");
			}
			// Prevent removing the main tenant until another tenant is promoted
			if (link.IsMainTenant)
			{
				throw new InvalidOperationException("Hãy chọn một khách khác làm người thuê chính trước khi gỡ.");
			}

			// Resolve tenant display name from navigation property or fallback query
			string tenantName = link.Tenant?.FullName
				?? _context.Tenants.Where(t => t.Id == tenantId).Select(t => t.FullName).FirstOrDefault()
				?? tenantId.ToString(CultureInfo.InvariantCulture);
			// Build a human-readable audit detail combining room number and tenant name
			string detail = $"{contract.Room.RoomNumber} · {tenantName}";
			// Stage the contract-tenant junction row for removal
			_context.ContractTenants.Remove(link);
			// Record an audit log describing which tenant was removed from the contract
			AddAudit(managerId, "Gỡ khách", "Khách trên hợp đồng", contractId, detail,
				FormatLines(new Dictionary<string, string?>
				{
					["Khách"] = tenantId.ToString(CultureInfo.InvariantCulture)
				}), null);
			// Commit junction removal and audit row together
			_context.SaveChanges();
		}

		// Query Invoices records.
		public List<Invoice> GetInvoices(int managerId, int? buildingId = null)
		{
			// Load invoices for managed buildings with line items, contract/room/building graph, tenants, and payments
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

		// Query InvoiceBillingContract records.
		public Contract GetInvoiceBillingContract(int managerId, int contractId)
		{
			// Load the billing contract with room services, service metadata, readings, and tenant membership for invoice generation
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

		// CreateInvoice — public entry point.
		public Invoice CreateInvoice(
			int managerId,
			Invoice invoice,
			IReadOnlyCollection<InvoiceItem> items,
			IReadOnlyCollection<ServiceReading> readings)
		{
			// Verify the manager may bill against the target contract
			EnsureContractAccess(managerId, invoice.ContractId);
			// Enforce one invoice per contract per calendar month
			EnsureUniqueInvoiceMonth(invoice.ContractId, invoice.InvoiceDate, null);
			// Wrap invoice insert, line items, readings, and audit in one transaction
			using var transaction = _context.Database.BeginTransaction();
			// Stage the invoice header row for insert so identity id is generated on first save
			_context.Invoices.Add(invoice);
			// Persist the invoice row and populate invoice.Id from the database identity
			_context.SaveChanges();
			foreach (var item in items)
			{
				// Bind each line item to the newly generated invoice id before insert
				item.InvoiceId = invoice.Id;
			}
			// Stage all invoice line items for bulk insert
			_context.InvoiceItems.AddRange(items);
			// Stage all service meter readings captured during billing for bulk insert
			_context.ServiceReadings.AddRange(readings);
			// Resolve room number for audit detail text from the billed contract
			string roomNumber = _context.Contracts
				.Where(c => c.Id == invoice.ContractId)
				.Select(c => c.Room.RoomNumber)
				.FirstOrDefault()
				?? invoice.ContractId.ToString(CultureInfo.InvariantCulture);
			// Build audit detail combining room number and invoice month/year
			string detail = $"{roomNumber} · {invoice.InvoiceDate.ToString("MM/yyyy", CultureInfo.InvariantCulture)}";
			// Record an audit log summarizing the created invoice totals and line count
			AddAudit(managerId, "Tạo mới", "Hóa đơn", invoice.Id, detail, null,
				FormatLines(new Dictionary<string, string?>
				{
					["Hợp đồng"] = invoice.ContractId.ToString(CultureInfo.InvariantCulture),
					["Tổng tiền"] = invoice.TotalAmount.ToString("N0", CultureInfo.InvariantCulture),
					["Số mục"] = items.Count.ToString(CultureInfo.InvariantCulture)
				}));
			// Commit line items, readings, and audit row together with the invoice header
			_context.SaveChanges();
			// Finalize the transaction after all billing writes succeed
			transaction.Commit();
			return invoice;
		}

		// Persist changes to an existing record.
		public void UpdateInvoice(int managerId, Invoice changes)
		{
			// Load the managed invoice with contract room and existing line items
			var invoice = _context.Invoices
				.Include(i => i.Contract)
					.ThenInclude(c => c.Room)
				.Include(i => i.InvoiceItems)
				.Single(i => i.Id == changes.Id && i.Contract.Room.Building.ManagerId == managerId);
			// Snapshot editable invoice fields before mutation for audit diff generation
			var before = new Dictionary<string, string?>
			{
				["Ngày hóa đơn"] = invoice.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Hạn thanh toán"] = invoice.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Tổng tiền"] = invoice.TotalAmount.ToString("N0", CultureInfo.InvariantCulture),
				["Trạng thái"] = invoice.Status
			};
			// Map allowed invoice updates onto the tracked database row
			invoice.DueDate = changes.DueDate;
			invoice.Status = changes.Status;
			// Snapshot the same fields after mutation for audit diff generation
			var after = new Dictionary<string, string?>
			{
				["Ngày hóa đơn"] = invoice.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Hạn thanh toán"] = invoice.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
				["Tổng tiền"] = invoice.TotalAmount.ToString("N0", CultureInfo.InvariantCulture),
				["Trạng thái"] = invoice.Status
			};
			// Build old/new audit text only for fields that actually changed
			var (oldValue, newValue) = DiffChanged(before, after);
			// Stage an audit log describing the invoice update with field-level diffs
			AddAudit(managerId, "Cập nhật", "Hóa đơn", invoice.Id, invoice.Contract.Room.RoomNumber, oldValue, newValue);
			// Commit invoice updates and the audit row together
			_context.SaveChanges();
		}

		// DeleteInvoice when allowed by rules.
		public void DeleteInvoice(int managerId, int invoiceId)
		{
			// Load the managed invoice with contract room, line items, and payments for delete validation
			var invoice = _context.Invoices
				.Include(i => i.Contract)
					.ThenInclude(c => c.Room)
				.Include(i => i.InvoiceItems)
				.Include(i => i.Payments)
				.Single(i => i.Id == invoiceId && i.Contract.Room.Building.ManagerId == managerId);
			// Block delete when payment history already exists for this invoice
			if (invoice.Payments.Count > 0)
			{
				throw new InvalidOperationException("Không thể xóa hóa đơn đã có thanh toán.");
			}

			// Wrap invoice delete, line item cleanup, and audit logging in one transaction
			using var transaction = _context.Database.BeginTransaction();
			// Remove all invoice line items before deleting the invoice header
			_context.InvoiceItems.RemoveRange(invoice.InvoiceItems);
			// Remove the invoice header row itself
			_context.Invoices.Remove(invoice);
			// Record an audit log describing the deleted invoice total
			AddAudit(managerId, "Xóa", "Hóa đơn", invoice.Id, invoice.Contract.Room.RoomNumber,
				FormatLines(new Dictionary<string, string?>
				{
					["Tổng tiền"] = invoice.TotalAmount.ToString("N0", CultureInfo.InvariantCulture)
				}), null);
			// Commit line item and invoice deletions plus the audit row
			_context.SaveChanges();
			// Finalize the transaction after delete succeeds
			transaction.Commit();
		}

		// Query RoomServices records.
		public List<RoomService> GetRoomServices(int managerId, int? buildingId = null)
		{
			// Load room-service assignments for managed buildings with room, building, and service metadata
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

		// SetRoomService — public entry point.
		public void SetRoomService(int managerId, int roomId, int serviceId, bool isActive)
		{
			// Load and authorize the target room including building and room type metadata
			var room = GetManagedRoom(managerId, roomId);
			// Load the service with its parent property or fail when the service id does not exist
			var service = _context.Services
				.Include(s => s.Property)
				.SingleOrDefault(s => s.Id == serviceId)
				?? throw new UnauthorizedAccessException("Dịch vụ không tồn tại.");
			// Enforce that room and service belong to the same property
			if (room.Building.PropertyId != service.PropertyId)
			{
				throw new InvalidOperationException("Chỉ có thể gán dịch vụ cùng nhà trọ với phòng.");
			}
			// Verify the manager actively manages at least one building in the service property
			bool managesProperty = _context.Buildings.Any(b =>
				b.ManagerId == managerId &&
				b.IsActive &&
				b.PropertyId == service.PropertyId);
			if (!managesProperty)
			{
				throw new UnauthorizedAccessException("Dịch vụ không thuộc nhà trọ được phân công.");
			}
			// Prevent activating a service that has been deactivated at catalog level
			if (!service.IsActive && isActive)
			{
				throw new InvalidOperationException("Dịch vụ đã ngừng hoạt động và không thể gán mới.");
			}

			// Look up the composite-key room-service junction row if it already exists
			var assignment = _context.RoomServices.Find(roomId, serviceId);
			if (assignment == null)
			{
				// Create a new junction row when assigning a service to the room for the first time
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
				// Toggle the active flag on an existing junction row
				assignment.IsActive = isActive;
			}
			// Record an audit log describing the service assignment or unassignment action
			AddAudit(managerId, isActive ? "Gán dịch vụ" : "Bỏ gán dịch vụ",
				"Dịch vụ phòng", roomId, $"{room.RoomNumber} · {service.ServiceName}", null,
				FormatLines(new Dictionary<string, string?>
				{
					["Dịch vụ"] = serviceId.ToString(CultureInfo.InvariantCulture)
				}));
			// Commit junction insert/update and audit row together
			_context.SaveChanges();
		}

		private List<int> GetCreatedTenantIds(int managerId)
		{
			// Read audit log rows where this manager created tenant records and parse RecordId into integer ids
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
			// Throw when the manager has no active building assignment
			if (!_context.Buildings.Any(b => b.ManagerId == managerId && b.IsActive))
			{
				throw new UnauthorizedAccessException("Quản lý chưa được phân công tòa nhà.");
			}
		}

		private void EnsureTenantAccess(int managerId, int tenantId)
		{
			// Grant access when the tenant is linked to a contract in one of the manager's buildings
			bool linked = _context.ContractTenants.Any(ct =>
				ct.TenantId == tenantId &&
				ct.Contract.Room.Building.ManagerId == managerId);
			// Grant access when audit logs show this manager created the tenant record
			bool created = _context.AuditLogs.Any(a =>
				a.UserId == managerId &&
				(a.TableName == "Tenants" || a.TableName == "Khách thuê") &&
				(a.Action == "Create" || a.Action == "Tạo mới") &&
				a.RecordId == tenantId.ToString(CultureInfo.InvariantCulture));
			// Deny access when neither contract link nor creation audit exists
			if (!linked && !created)
			{
				throw new UnauthorizedAccessException("Bạn không có quyền thao tác khách thuê này.");
			}
		}

		private Room GetManagedRoom(int managerId, int roomId)
		{
			// Load one room with building and room type only when it belongs to a building managed by this user
			return _context.Rooms
				.Include(r => r.Building)
				.Include(r => r.RoomType)
				.SingleOrDefault(r => r.Id == roomId && r.Building.ManagerId == managerId)
				?? throw new UnauthorizedAccessException("Phòng không thuộc tòa nhà được phân công.");
		}

		private Contract GetManagedContract(int managerId, int contractId)
		{
			// Load one contract with room, building, room type, and tenant links only when managed by this user
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
			// Throw when no contract with this id exists under a building managed by the user
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
			// Detect any other active contract on the same room whose date range overlaps the proposed range
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
			// Detect any other active contract membership for this tenant whose date range overlaps the proposed range
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
			// Compute inclusive month start and exclusive next-month boundary for duplicate detection
			DateTime monthStart = new(invoiceDate.Year, invoiceDate.Month, 1);
			DateTime nextMonth = monthStart.AddMonths(1);
			// Throw when another invoice already exists for this contract in the same calendar month
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
			// Load the room row that must receive an updated occupancy status
			var room = _context.Rooms.Single(r => r.Id == roomId);
			// Skip automatic status changes while the room is in maintenance
			if (!string.Equals(room.Status, "Bảo trì", StringComparison.OrdinalIgnoreCase))
			{
				// Capture today's calendar date for active-contract evaluation
				var today = DateOnly.FromDateTime(DateTime.Today);
				// Set occupied when an active contract covers today, otherwise mark the room empty
				room.Status = _context.Contracts.Any(c =>
					c.RoomId == roomId &&
					c.Status == ActiveContract &&
					c.StartDate <= today &&
					c.EndDate >= today)
					? OccupiedRoom
					: EmptyRoom;
				// Persist the derived room status change
				_context.SaveChanges();
			}
		}

		private void AddAudit(
			int managerId,
			string action,
			string tableName,
			int recordId,
			string? detail,
			string? oldValue,
			string? newValue)
		{
			// Stage a new audit log row describing the manager action and optional before/after values
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

		// Vietnamese labels (match AuditField keys) — DAL does not reference BLL enums
		private static (string? OldValue, string? NewValue) DiffChanged(
			IReadOnlyDictionary<string, string?> before,
			IReadOnlyDictionary<string, string?> after)
		{
			// Accumulate human-readable old and new lines for fields that changed
			var oldLines = new List<string>();
			var newLines = new List<string>();
			// Walk the union of keys from both snapshots so newly added or removed fields are included
			foreach (string key in before.Keys.Union(after.Keys).Distinct())
			{
				// Read raw before/after values when the key exists in each dictionary
				before.TryGetValue(key, out string? oldRaw);
				after.TryGetValue(key, out string? newRaw);
				// Normalize whitespace-only values to empty string for stable equality comparison
				string oldNorm = string.IsNullOrWhiteSpace(oldRaw) ? string.Empty : oldRaw.Trim();
				string newNorm = string.IsNullOrWhiteSpace(newRaw) ? string.Empty : newRaw.Trim();
				// Skip keys whose normalized values are identical
				if (oldNorm == newNorm) continue;
				// Append formatted old/new lines using em dash placeholder for blank values
				oldLines.Add($"{key}: {(string.IsNullOrWhiteSpace(oldRaw) ? "—" : oldRaw.Trim())}");
				newLines.Add($"{key}: {(string.IsNullOrWhiteSpace(newRaw) ? "—" : newRaw.Trim())}");
			}
			// Return null for a side with no changed lines so audit storage stays compact
			return (
				oldLines.Count == 0 ? null : string.Join("\n", oldLines),
				newLines.Count == 0 ? null : string.Join("\n", newLines));
		}

		private static string? FormatLines(IReadOnlyDictionary<string, string?> values)
		{
			// Build non-empty label/value lines from the supplied dictionary
			var lines = values
				.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
				.Select(kv => $"{kv.Key}: {kv.Value!.Trim()}")
				.ToList();
			// Return null when every value was blank so callers can omit audit text entirely
			return lines.Count == 0 ? null : string.Join("\n", lines);
		}

		private static string FormatContractStatus(string status)
		{
			// Pass contract status through unchanged for audit display
			return status;
		}
	}
}
