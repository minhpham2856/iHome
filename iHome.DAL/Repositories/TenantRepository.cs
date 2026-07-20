using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.DAL.Repositories
{
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

		public int Count() => _context.Tenants.Count();

		public Tenant? GetById(int id) =>
			_context.Tenants
				.Include(t => t.ContractTenants)
				.FirstOrDefault(t => t.Id == id);

		public List<Tenant> GetForLandlord(int landlordId)
		{
			var linkedIds = _context.ContractTenants
				.AsNoTracking()
				.Where(ct => ct.Contract.Room.Building.Property.LandlordId == landlordId)
				.Select(ct => ct.TenantId);

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

		public List<ContractTenant> GetContractLinks(int landlordId, int tenantId) =>
			_context.ContractTenants
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

		public bool IdCardExists(string idCardNumber, int? excludingTenantId = null) =>
			_context.Tenants.Any(t =>
				t.IdCardNumber == idCardNumber &&
				(!excludingTenantId.HasValue || t.Id != excludingTenantId.Value));

		public bool Add(Tenant tenant)
		{
			_context.Tenants.Add(tenant);
			return _context.SaveChanges() > 0;
		}

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

		public bool Delete(int id)
		{
			var existing = _context.Tenants
				.Include(t => t.ContractTenants)
				.FirstOrDefault(t => t.Id == id);
			if (existing == null) return false;
			if (existing.ContractTenants.Count > 0) return false;

			_context.Tenants.Remove(existing);
			return _context.SaveChanges() > 0;
		}

		public bool HasAnyContracts(int tenantId) =>
			_context.ContractTenants.Any(ct => ct.TenantId == tenantId);

		public bool IsAccessible(int landlordId, int tenantId)
		{
			bool linked = _context.ContractTenants.Any(ct =>
				ct.TenantId == tenantId &&
				ct.Contract.Room.Building.Property.LandlordId == landlordId);
			if (linked) return true;

			string recordId = tenantId.ToString(CultureInfo.InvariantCulture);
			return _context.AuditLogs.Any(a =>
				a.UserId == landlordId &&
				a.TableName == "Tenants" &&
				a.Action == "Create" &&
				a.RecordId == recordId);
		}

		private List<int> GetCreatedTenantIds(int landlordId) =>
			_context.AuditLogs
				.AsNoTracking()
				.Where(a =>
					a.UserId == landlordId &&
					a.TableName == "Tenants" &&
					a.Action == "Create" &&
					a.RecordId != null)
				.Select(a => a.RecordId!)
				.AsEnumerable()
				.Select(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : 0)
				.Where(id => id > 0)
				.Distinct()
				.ToList();
	}
}
