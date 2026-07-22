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
		private readonly IHomeDbContext _context;

		public ServiceRepository() : this(new IHomeDbContext())
		{
		}

		public ServiceRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert a new service.
		public bool Add(Service service)
		{
			_context.Services.Add(service);
			return _context.SaveChanges() > 0;
		}

		// Services for one property (active first).
		public List<Service> GetByProperty(int propertyId)
		{
			return _context.Services
				.Include(s => s.Property)
				.Include(s => s.RoomServices)
				.Where(s => s.PropertyId == propertyId)
				.OrderByDescending(s => s.IsActive)
				.ThenBy(s => s.ServiceName)
				.ToList();
		}

		// One service with property and room assignments.
		public Service GetById(int id)
		{
			return _context.Services
				.Include(s => s.Property)
				.Include(s => s.RoomServices)
				.FirstOrDefault(s => s.Id == id);
		}

		// Update fields; deactivating cascades room assignments off.
		public void Update(Service changes)
		{
			var existing = _context.Services
				.Include(s => s.RoomServices)
				.FirstOrDefault(s => s.Id == changes.Id);
			if (existing == null) return;

			existing.ServiceName = changes.ServiceName;
			existing.Unit = changes.Unit;
			existing.UnitPrice = changes.UnitPrice;
			existing.CalculationMethod = changes.CalculationMethod;

			// Deactivate service → turn off all room assignments using it.
			bool wasActive = existing.IsActive;
			existing.IsActive = changes.IsActive;
			if (wasActive && !changes.IsActive)
			{
				DeactivateAssignments(existing);
			}

			_context.SaveChanges();
		}

		// Soft-disable service and cascade room assignments off.
		public bool Disable(Service service)
		{
			ArgumentNullException.ThrowIfNull(service);
			var existing = _context.Services
				.Include(s => s.RoomServices)
				.FirstOrDefault(s => s.Id == service.Id);
			if (existing == null) return false;

			existing.IsActive = false;
			DeactivateAssignments(existing);
			return _context.SaveChanges() > 0;
		}

		// True when at least one room still has an active assignment.
		public bool HasActiveAssignments(Service service)
		{
			ArgumentNullException.ThrowIfNull(service);
			return _context.RoomServices.Any(rs => rs.ServiceId == service.Id && rs.IsActive);
		}

		private static void DeactivateAssignments(Service service)
		{
			foreach (var assignment in service.RoomServices.Where(rs => rs.IsActive))
			{
				assignment.IsActive = false;
			}
		}
	}
}
