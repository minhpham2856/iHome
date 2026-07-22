using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Service.
	public class ServiceRepository
	{
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public ServiceRepository() : this(new IHomeDbContext())
		{
		}

		public ServiceRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert and save.
		public bool Add(Service service)
		{
			// Stage the new service entity for insert in the change tracker
			_context.Services.Add(service);
			// Persist the insert and return whether at least one row was written
			return _context.SaveChanges() > 0;
		}

		// Query ByProperty records.
		public List<Service> GetByProperty(int propertyId)
		{
			// Load services for one property with property and room assignment links, active first then by name
			return _context.Services
				.Include(s => s.Property)
				.Include(s => s.RoomServices)
				.Where(s => s.PropertyId == propertyId)
				.OrderByDescending(s => s.IsActive)
				.ThenBy(s => s.ServiceName)
				.ToList();
		}

		// Load one record by id.
		public Service GetById(int id)
		{
			// Load one service by id with property and room assignment links for edit/detail views
			return _context.Services
				.Include(s => s.Property)
				.Include(s => s.RoomServices)
				.FirstOrDefault(s => s.Id == id);
		}

		// Persist changes to an existing record.
		public void Update(Service changes)
		{
			// Load the tracked service row with room assignments so deactivation can cascade in memory
			var existing = _context.Services
				.Include(s => s.RoomServices)
				.FirstOrDefault(s => s.Id == changes.Id);
			// Exit quietly when the target row no longer exists
			if (existing == null) return;

			// Map editable scalar fields from the incoming entity onto the tracked row
			existing.ServiceName = changes.ServiceName;
			existing.Unit = changes.Unit;
			existing.UnitPrice = changes.UnitPrice;
			existing.CalculationMethod = changes.CalculationMethod;

			// Remember prior active flag to detect a transition from active to inactive
			bool wasActive = existing.IsActive;
			existing.IsActive = changes.IsActive;
			// Deactivate service → remove all room assignments using that service
			if (wasActive && !changes.IsActive)
			{
				DeactivateAssignments(existing);
			}

			// Commit service field updates and any cascaded assignment deactivations
			_context.SaveChanges();
		}

		// Disable when allowed by rules.
		public bool Disable(Service service)
		{
			// Guard against null service input before lookup
			ArgumentNullException.ThrowIfNull(service);
			// Reload the service with room assignments so deactivation can cascade
			var existing = _context.Services
				.Include(s => s.RoomServices)
				.FirstOrDefault(s => s.Id == service.Id);
			// Return false when the service was already deleted or never existed
			if (existing == null) return false;

			// Soft-disable the service at catalog level
			existing.IsActive = false;
			// Turn off every active room assignment pointing at this service
			DeactivateAssignments(existing);
			// Persist the service deactivation and assignment updates
			return _context.SaveChanges() > 0;
		}

		// HasActiveAssignments — public entry point.
		public bool HasActiveAssignments(Service service)
		{
			// Guard against null service input before existence check
			ArgumentNullException.ThrowIfNull(service);
			// Return true when at least one room still has an active assignment to this service
			return _context.RoomServices.Any(rs => rs.ServiceId == service.Id && rs.IsActive);
		}

		private static void DeactivateAssignments(Service service)
		{
			// Walk every active room-service junction row on the loaded service and mark it inactive
			foreach (var assignment in service.RoomServices.Where(rs => rs.IsActive))
			{
				assignment.IsActive = false;
			}
		}
	}
}
