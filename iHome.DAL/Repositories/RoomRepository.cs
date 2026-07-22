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
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		// Parameterless ctor — creates its own context for simple call sites.
		public RoomRepository() : this(new IHomeDbContext()) { }

		// Preferred ctor — share one context across cooperating repos on a page.
		public RoomRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert a new room row.
		public bool Add(Room room)
		{
			// Stage the new room entity for insert in the change tracker
			_context.Rooms.Add(room);
			// Persist the insert and return whether at least one row was written
			return _context.SaveChanges() > 0;
		}

		// Query All records.
		public List<Room> GetAll()
		{
			// Load all room rows without filters, includes, or ordering
			return _context.Rooms.ToList();
		}

		// CountByStatus — public entry point.
		public int CountByStatus(string status)
		{
			// Count rooms whose Status column equals the supplied status label
			return _context.Rooms.Count(r => r.Status == status);
		}

		// Query ByBuilding records.
		public List<Room> GetByBuilding(int buildingId)
		{
			// Load rooms in one building with type, building/property, contracts, and contract tenants, ordered by floor then room number
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

		// Load one record by id.
		public Room GetById(int id)
		{
			// Load one room by id with full navigation graph for detail and edit screens
			return _context.Rooms
				.Include(r => r.RoomType)
				.Include(r => r.Building)
					.ThenInclude(b => b.Property)
				.Include(r => r.Contracts)
					.ThenInclude(c => c.ContractTenants)
						.ThenInclude(ct => ct.Tenant)
				.FirstOrDefault(r => r.Id == id);
		}

		// RoomNumberExists — public entry point.
		public bool RoomNumberExists(int buildingId, string roomNumber, int? excludeRoomId = null)
		{
			// Normalize the room number by trimming whitespace before uniqueness comparison
			string normalized = roomNumber.Trim();
			// Return true when another room in the same building already uses this room number
			return _context.Rooms.Any(r =>
				r.BuildingId == buildingId
				&& r.RoomNumber == normalized
				&& (!excludeRoomId.HasValue || r.Id != excludeRoomId.Value));
		}

		// Persist changes to an existing record.
		public void Update(Room changes)
		{
			// Load the tracked room row matching the incoming entity id
			var existing = _context.Rooms.FirstOrDefault(r => r.Id == changes.Id);
			// Exit quietly when the target row no longer exists
			if (existing == null) return;

			// Map editable scalar fields from the incoming entity onto the tracked row
			existing.RoomTypeId = changes.RoomTypeId;
			existing.RoomNumber = changes.RoomNumber;
			existing.Floor = changes.Floor;
			existing.Status = changes.Status;
			existing.Notes = changes.Notes;
			// Commit the updated room columns to the database
			_context.SaveChanges();
		}

		// Delete when allowed by rules.
		public bool Delete(Room room)
		{
			// Guard against null room input before lookup
			ArgumentNullException.ThrowIfNull(room);
			// Load the room with contracts to enforce delete safety rules
			var existing = _context.Rooms
				.Include(r => r.Contracts)
				.FirstOrDefault(r => r.Id == room.Id);
			// Return false when the room was already deleted or never existed
			if (existing == null) return false;
			// Block delete when the room still has contract history
			if (existing.Contracts.Count > 0) return false;

			// Remove the room row from the change tracker
			_context.Rooms.Remove(existing);
			// Persist the delete and report whether a row was removed
			return _context.SaveChanges() > 0;
		}

		// HasContracts — public entry point.
		public bool HasContracts(Room room)
		{
			// Guard against null room input before existence check
			ArgumentNullException.ThrowIfNull(room);
			// Return true when at least one contract row references this room id
			return _context.Contracts.Any(c => c.RoomId == room.Id);
		}
	}
}
