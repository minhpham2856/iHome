using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
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

		public bool Add(Service service)
		{
			_context.Services.Add(service);
			return _context.SaveChanges() > 0;
		}

		public List<Service> GetByProperty(int propertyId) =>
			_context.Services
				.Include(s => s.Property)
				.Include(s => s.RoomServices)
				.Where(s => s.PropertyId == propertyId)
				.OrderByDescending(s => s.IsActive)
				.ThenBy(s => s.ServiceName)
				.ToList();

		public Service GetById(int id) =>
			_context.Services
				.Include(s => s.Property)
				.Include(s => s.RoomServices)
				.FirstOrDefault(s => s.Id == id);

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

			bool wasActive = existing.IsActive;
			existing.IsActive = changes.IsActive;
			// ngừng dịch vụ → cắt mọi gán phòng đang dùng dịch vụ đó
			if (wasActive && !changes.IsActive)
			{
				DeactivateAssignments(existing);
			}

			_context.SaveChanges();
		}

		public bool Disable(int id)
		{
			var existing = _context.Services
				.Include(s => s.RoomServices)
				.FirstOrDefault(s => s.Id == id);
			if (existing == null) return false;

			existing.IsActive = false;
			DeactivateAssignments(existing);
			return _context.SaveChanges() > 0;
		}

		public bool HasActiveAssignments(int serviceId) =>
			_context.RoomServices.Any(rs => rs.ServiceId == serviceId && rs.IsActive);

		private static void DeactivateAssignments(Service service)
		{
			foreach (var assignment in service.RoomServices.Where(rs => rs.IsActive))
			{
				assignment.IsActive = false;
			}
		}
	}
}
