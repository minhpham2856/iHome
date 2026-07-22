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
		private readonly IHomeDbContext _context;

		public TenantRepository() : this(new IHomeDbContext())
		{
		}

		public TenantRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Total tenant count.
		public int Count()
		{
			return _context.Tenants.Count();
		}

		// One tenant with contract membership links.
		public Tenant? GetById(int id)
		{
			return _context.Tenants
				.Include(t => t.ContractTenants)
				.FirstOrDefault(t => t.Id == id);
		}

		// Tenants in landlord scope: contract-linked or landlord-created via audit.
		public List<Tenant> GetForLandlord(int landlordId)
		{
			var linkedIds = _context.ContractTenants
				.AsNoTracking()
				.Where(ct => ct.Contract.Room.Building.Property.LandlordId == landlordId)
				.Select(ct => ct.TenantId);

			// Scope via audit: tenants this landlord created.
			var createdIds = GetCreatedTenantIds(landlordId);

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

		// Contract links for one tenant under the landlord's portfolio.
		public List<ContractTenant> GetContractLinks(int landlordId, int tenantId)
		{
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

		// True when another tenant already uses this id card number.
		public bool IdCardExists(string idCardNumber, int? excludingTenantId = null)
		{
			return _context.Tenants.Any(t =>
				t.IdCardNumber == idCardNumber &&
				(!excludingTenantId.HasValue || t.Id != excludingTenantId.Value));
		}

		// Insert a new tenant.
		public bool Add(Tenant tenant)
		{
			_context.Tenants.Add(tenant);
			return _context.SaveChanges() > 0;
		}

		// Update tenant profile fields.
		public bool Update(Tenant changes)
		{
			var existing = _context.Tenants.FirstOrDefault(t => t.Id == changes.Id);
			if (existing == null) return false;

			existing.FullName = changes.FullName;
			existing.DateOfBirth = changes.DateOfBirth;
			existing.IdCardNumber = changes.IdCardNumber;
			existing.PhoneNumber = changes.PhoneNumber;
			existing.Email = changes.Email;
			existing.PermanentAddress = changes.PermanentAddress;
			return _context.SaveChanges() > 0;
		}

		// Delete only when the tenant has no contract links.
		public bool Delete(Tenant tenant)
		{
			ArgumentNullException.ThrowIfNull(tenant);
			var existing = _context.Tenants
				.Include(t => t.ContractTenants)
				.FirstOrDefault(t => t.Id == tenant.Id);
			if (existing == null) return false;
			if (existing.ContractTenants.Count > 0) return false;

			_context.Tenants.Remove(existing);
			return _context.SaveChanges() > 0;
		}

		// True when the tenant appears on any contract.
		public bool HasAnyContracts(Tenant tenant)
		{
			ArgumentNullException.ThrowIfNull(tenant);
			return _context.ContractTenants.Any(ct => ct.TenantId == tenant.Id);
		}

		// Access via contract link under landlord, else via creation audit.
		public bool IsAccessible(int landlordId, int tenantId)
		{
			bool linked = _context.ContractTenants.Any(ct =>
				ct.TenantId == tenantId &&
				ct.Contract.Room.Building.Property.LandlordId == landlordId);
			if (linked) return true;

			string recordId = tenantId.ToString(CultureInfo.InvariantCulture);
			return _context.AuditLogs.Any(a =>
				a.UserId == landlordId &&
				(a.TableName == "Tenants" || a.TableName == "Khách thuê") &&
				(a.Action == "Create" || a.Action == "Tạo mới") &&
				a.RecordId == recordId);
		}

		// Tenant ids created by this landlord — from audit (Tenants / Khách thuê + Create / Tạo mới).
		private List<int> GetCreatedTenantIds(int landlordId)
		{
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
