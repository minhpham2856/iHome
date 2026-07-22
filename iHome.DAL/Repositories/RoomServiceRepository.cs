using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for RoomService.
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

		// Rooms in a building with their service assignments.
		public List<Room> GetRoomsWithServicesByBuilding(int buildingId)
		{
			return _context.Rooms
				.Include(r => r.RoomServices)
					.ThenInclude(rs => rs.Service)
				.Where(r => r.BuildingId == buildingId)
				.OrderBy(r => r.Floor)
				.ThenBy(r => r.RoomNumber)
				.ToList();
		}

		// One room with building/property and room-service links.
		public Room GetRoomWithServices(int roomId)
		{
			return _context.Rooms
				.Include(r => r.Building)
					.ThenInclude(b => b.Property)
				.Include(r => r.RoomServices)
					.ThenInclude(rs => rs.Service)
				.FirstOrDefault(r => r.Id == roomId);
		}

		// Service ids currently active on the room.
		public List<int> GetAssignedServiceIds(int roomId)
		{
			return _context.RoomServices
				.Where(rs => rs.RoomId == roomId && rs.IsActive)
				.Select(rs => rs.ServiceId)
				.ToList();
		}

		// Sync: selected services active; other property services soft-off if a row exists.
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
