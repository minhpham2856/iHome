using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class RoomServiceRepository
	{
		private readonly IHomeDbContext _context;

		public RoomServiceRepository() : this(new IHomeDbContext())
		{
		}

		public RoomServiceRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		public List<Room> GetRoomsWithServicesByBuilding(int buildingId) =>
			_context.Rooms
				.Include(r => r.RoomServices)
					.ThenInclude(rs => rs.Service)
				.Where(r => r.BuildingId == buildingId)
				.OrderBy(r => r.Floor)
				.ThenBy(r => r.RoomNumber)
				.ToList();

		public Room GetRoomWithServices(int roomId) =>
			_context.Rooms
				.Include(r => r.Building)
					.ThenInclude(b => b.Property)
				.Include(r => r.RoomServices)
					.ThenInclude(rs => rs.Service)
				.FirstOrDefault(r => r.Id == roomId);

		public List<int> GetAssignedServiceIds(int roomId) =>
			_context.RoomServices
				.Where(rs => rs.RoomId == roomId && rs.IsActive)
				.Select(rs => rs.ServiceId)
				.ToList();

		// sync: selected services active for room; other property services soft-off if row exists
		public void SyncRoomAssignments(int roomId, int propertyId, IReadOnlyCollection<int> selectedServiceIds)
		{
			var selected = selectedServiceIds.ToHashSet();
			var propertyServiceIds = _context.Services
				.Where(s => s.PropertyId == propertyId)
				.Select(s => s.Id)
				.ToList();

			var existing = _context.RoomServices
				.Where(rs => rs.RoomId == roomId && propertyServiceIds.Contains(rs.ServiceId))
				.ToList();

			foreach (int serviceId in propertyServiceIds)
			{
				bool shouldActive = selected.Contains(serviceId);
				var row = existing.FirstOrDefault(rs => rs.ServiceId == serviceId);
				if (row == null)
				{
					if (!shouldActive) continue;
					_context.RoomServices.Add(new RoomService
					{
						RoomId = roomId,
						ServiceId = serviceId,
						IsActive = true
					});
					continue;
				}

				row.IsActive = shouldActive;
			}

			_context.SaveChanges();
		}
	}
}
