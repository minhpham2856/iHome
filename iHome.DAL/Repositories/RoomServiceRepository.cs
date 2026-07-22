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
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public RoomServiceRepository() : this(new IHomeDbContext())
		{
		}

		public RoomServiceRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Query RoomsWithServicesByBuilding records.
		public List<Room> GetRoomsWithServicesByBuilding(int buildingId)
		{
			// Load rooms in one building with their active service assignments and service metadata, ordered by floor and room number
			return _context.Rooms
				.Include(r => r.RoomServices)
					.ThenInclude(rs => rs.Service)
				.Where(r => r.BuildingId == buildingId)
				.OrderBy(r => r.Floor)
				.ThenBy(r => r.RoomNumber)
				.ToList();
		}

		// Query RoomWithServices records.
		public Room GetRoomWithServices(int roomId)
		{
			// Load one room with building, property, and all room-service links including service details
			return _context.Rooms
				.Include(r => r.Building)
					.ThenInclude(b => b.Property)
				.Include(r => r.RoomServices)
					.ThenInclude(rs => rs.Service)
				.FirstOrDefault(r => r.Id == roomId);
		}

		// Query AssignedServiceIds records.
		public List<int> GetAssignedServiceIds(int roomId)
		{
			// Return ids of services currently marked active on the given room
			return _context.RoomServices
				.Where(rs => rs.RoomId == roomId && rs.IsActive)
				.Select(rs => rs.ServiceId)
				.ToList();
		}

		// sync: selected services active for room; other property services soft-off if row exists
		public void SyncRoomAssignments(int roomId, int propertyId, IReadOnlyCollection<int> selectedServiceIds)
		{
			// Convert selected service ids into a HashSet for fast lookup while syncing assignments
			var selected = selectedServiceIds.ToHashSet();
			// Load every service id defined on the property so only in-scope services are considered
			var propertyServiceIds = _context.Services
				.Where(s => s.PropertyId == propertyId)
				.Select(s => s.Id)
				.ToList();

			// Load existing room-service junction rows for this room limited to the property's service catalog
			var existing = _context.RoomServices
				.Where(rs => rs.RoomId == roomId && propertyServiceIds.Contains(rs.ServiceId))
				.ToList();

			// Ensure each property service has the correct active/inactive assignment state on this room
			foreach (int serviceId in propertyServiceIds)
			{
				// Determine whether the UI selected this service for the room
				bool shouldActive = selected.Contains(serviceId);
				// Look for an existing junction row for this room-service pair
				var row = existing.FirstOrDefault(rs => rs.ServiceId == serviceId);
				if (row == null)
				{
					// Skip creating a row when the service was not selected and no prior assignment exists
					if (!shouldActive) continue;
					// Insert a new active room-service link when the service is newly selected
					_context.RoomServices.Add(new RoomService
					{
						RoomId = roomId,
						ServiceId = serviceId,
						IsActive = true
					});
					continue;
				}

				// Update the IsActive flag on an existing junction row to reflect the current selection
				row.IsActive = shouldActive;
			}

			// Commit all inserts and assignment flag updates for the room
			_context.SaveChanges();
		}
	}
}
