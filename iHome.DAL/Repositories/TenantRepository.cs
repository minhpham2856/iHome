using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Tenant.
	public class TenantRepository
	{
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public TenantRepository() : this(new IHomeDbContext())
		{
		}

		public TenantRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Count — public entry point.
		public int Count()
		{
			// Return the total number of tenant rows in the database
			return _context.Tenants.Count();
		}

		public Tenant? GetById(int id)
		{
			// Load one tenant by id with contract membership links for detail views
			return _context.Tenants
				.Include(t => t.ContractTenants)
				.FirstOrDefault(t => t.Id == id);
		}

		// Query ForLandlord records.
		public List<Tenant> GetForLandlord(int landlordId)
		{
			// Collect tenant ids linked to any contract under this landlord's properties
			var linkedIds = _context.ContractTenants
				.AsNoTracking()
				.Where(ct => ct.Contract.Room.Building.Property.LandlordId == landlordId)
				.Select(ct => ct.TenantId);

			// Collect tenant ids created by this landlord according to audit log history
			var createdIds = GetCreatedTenantIds(landlordId);

			// Return tenants that are either contract-linked or landlord-created, with full contract navigation graph
			return _context.Tenants
				.AsNoTracking()
				.Include(t => t.ContractTenants)
					.ThenInclude(ct => ct.Contract)
						.ThenInclude(c => c.Room)
							.ThenInclude(r => r.Building)
								.ThenInclude(b => b.Property)
				.Where(t => linkedIds.Contains(t.Id) || createdIds.Contains(t.Id))
				.OrderBy(t => t.FullName)
				.ToList();
		}

		// Query ContractLinks records.
		public List<ContractTenant> GetContractLinks(int landlordId, int tenantId)
		{
			// Load contract-tenant junction rows for one tenant scoped to the landlord's portfolio
			return _context.ContractTenants
				.AsNoTracking()
				.Include(ct => ct.Contract)
					.ThenInclude(c => c.Room)
						.ThenInclude(r => r.Building)
							.ThenInclude(b => b.Property)
				.Where(ct =>
					ct.TenantId == tenantId &&
					ct.Contract.Room.Building.Property.LandlordId == landlordId)
				.OrderByDescending(ct => ct.Contract.StartDate)
				.ThenBy(ct => ct.Contract.Room.Building.Name)
				.ThenBy(ct => ct.Contract.Room.RoomNumber)
				.ToList();
		}

		// IdCardExists — public entry point.
		public bool IdCardExists(string idCardNumber, int? excludingTenantId = null)
		{
			// Return true when another tenant already uses this national id card number
			return _context.Tenants.Any(t =>
				t.IdCardNumber == idCardNumber &&
				(!excludingTenantId.HasValue || t.Id != excludingTenantId.Value));
		}

		// Insert and save.
		public bool Add(Tenant tenant)
		{
			// Stage the new tenant entity for insert in the change tracker
			_context.Tenants.Add(tenant);
			// Persist the insert and return whether at least one row was written
			return _context.SaveChanges() > 0;
		}

		// Persist changes to an existing record.
		public bool Update(Tenant changes)
		{
			// Load the tracked tenant row matching the incoming entity id
			var existing = _context.Tenants.FirstOrDefault(t => t.Id == changes.Id);
			// Return false when the target row no longer exists
			if (existing == null) return false;

			// Map editable tenant profile fields from the incoming entity onto the tracked row
			existing.FullName = changes.FullName;
			existing.DateOfBirth = changes.DateOfBirth;
			existing.IdCardNumber = changes.IdCardNumber;
			existing.PhoneNumber = changes.PhoneNumber;
			existing.Email = changes.Email;
			existing.PermanentAddress = changes.PermanentAddress;
			// Commit tenant updates and report whether a row was written
			return _context.SaveChanges() > 0;
		}

		// Delete when allowed by rules.
		public bool Delete(Tenant tenant)
		{
			// Guard against null tenant input before lookup
			ArgumentNullException.ThrowIfNull(tenant);
			// Load the tenant with contract links to enforce delete safety rules
			var existing = _context.Tenants
				.Include(t => t.ContractTenants)
				.FirstOrDefault(t => t.Id == tenant.Id);
			// Return false when the tenant was already deleted or never existed
			if (existing == null) return false;
			// Block delete when the tenant still appears on any contract
			if (existing.ContractTenants.Count > 0) return false;

			// Remove the tenant row from the change tracker
			_context.Tenants.Remove(existing);
			// Persist the delete and report whether a row was removed
			return _context.SaveChanges() > 0;
		}

		// HasAnyContracts — public entry point.
		public bool HasAnyContracts(Tenant tenant)
		{
			// Guard against null tenant input before existence check
			ArgumentNullException.ThrowIfNull(tenant);
			// Return true when at least one contract-tenant junction row references this tenant
			return _context.ContractTenants.Any(ct => ct.TenantId == tenant.Id);
		}

		// IsAccessible — public entry point.
		public bool IsAccessible(int landlordId, int tenantId)
		{
			// Check whether the tenant is linked to any contract under this landlord's properties
			bool linked = _context.ContractTenants.Any(ct =>
				ct.TenantId == tenantId &&
				ct.Contract.Room.Building.Property.LandlordId == landlordId);
			// Grant access immediately when a contract link exists
			if (linked) return true;

			// Otherwise grant access when audit logs show this landlord created the tenant record
			string recordId = tenantId.ToString(CultureInfo.InvariantCulture);
			return _context.AuditLogs.Any(a =>
				a.UserId == landlordId &&
				(a.TableName == "Tenants" || a.TableName == "Khách thuê") &&
				(a.Action == "Create" || a.Action == "Tạo mới") &&
				a.RecordId == recordId);
		}

		private List<int> GetCreatedTenantIds(int landlordId)
		{
			// Read audit log rows where this landlord created tenant records and parse RecordId into integer ids
			return _context.AuditLogs
				.AsNoTracking()
				.Where(a =>
					a.UserId == landlordId &&
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
	}
}
