using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Room entities.
	public class RoomRepository
	{
		private readonly IHomeDbContext _context;

		public RoomRepository() : this(new IHomeDbContext()) { }

		// Share one context across cooperating repos on a page.
		public RoomRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert a new room.
		public bool Add(Room room)
		{
			_context.Rooms.Add(room);
			return _context.SaveChanges() > 0;
		}

		// All rooms.
		public List<Room> GetAll()
		{
			return _context.Rooms.ToList();
		}

		// Count rooms with the given status label.
		public int CountByStatus(string status)
		{
			return _context.Rooms.Count(r => r.Status == status);
		}

		// Rooms in one building (type, occupancy graph).
		public List<Room> GetByBuilding(int buildingId)
		{
			return _context.Rooms
				.Include(r => r.RoomType)
				.Include(r => r.Building)
					.ThenInclude(b => b.Property)
				.Include(r => r.Contracts)
					.ThenInclude(c => c.ContractTenants)
				.Where(r => r.BuildingId == buildingId)
				.OrderBy(r => r.Floor)
				.ThenBy(r => r.RoomNumber)
				.ToList();
		}

		// Rooms across every building of a property.
		public List<Room> GetByProperty(int propertyId)
		{
			return _context.Rooms
				.Include(r => r.RoomType)
				.Include(r => r.Building)
					.ThenInclude(b => b.Property)
				.Include(r => r.Contracts)
					.ThenInclude(c => c.ContractTenants)
				.Where(r => r.Building.PropertyId == propertyId)
				.OrderBy(r => r.Building.Name)
				.ThenBy(r => r.Floor)
				.ThenBy(r => r.RoomNumber)
				.ToList();
		}

		// One room by id with full navigation graph.
		public Room GetById(int id)
		{
			return _context.Rooms
				.Include(r => r.RoomType)
				.Include(r => r.Building)
					.ThenInclude(b => b.Property)
				.Include(r => r.Contracts)
					.ThenInclude(c => c.ContractTenants)
						.ThenInclude(ct => ct.Tenant)
				.FirstOrDefault(r => r.Id == id);
		}

		// True when another room in the same building already uses this number.
		public bool RoomNumberExists(int buildingId, string roomNumber, int? excludeRoomId = null)
		{
			string normalized = roomNumber.Trim();
			return _context.Rooms.Any(r =>
				r.BuildingId == buildingId
				&& r.RoomNumber == normalized
				&& (!excludeRoomId.HasValue || r.Id != excludeRoomId.Value));
		}

		// Update editable room fields.
		public void Update(Room changes)
		{
			var existing = _context.Rooms.FirstOrDefault(r => r.Id == changes.Id);
			if (existing == null) return;

			existing.RoomTypeId = changes.RoomTypeId;
			existing.RoomNumber = changes.RoomNumber;
			existing.Floor = changes.Floor;
			existing.Status = changes.Status;
			existing.Notes = changes.Notes;
			_context.SaveChanges();
		}

		// Delete only when the room has no contract history.
		public bool Delete(Room room)
		{
			ArgumentNullException.ThrowIfNull(room);
			var existing = _context.Rooms
				.Include(r => r.Contracts)
				.FirstOrDefault(r => r.Id == room.Id);
			if (existing == null) return false;
			if (existing.Contracts.Count > 0) return false;

			_context.Rooms.Remove(existing);
			return _context.SaveChanges() > 0;
		}

		// True when at least one contract references this room.
		public bool HasContracts(Room room)
		{
			ArgumentNullException.ThrowIfNull(room);
			return _context.Contracts.Any(c => c.RoomId == room.Id);
		}
	}
}
