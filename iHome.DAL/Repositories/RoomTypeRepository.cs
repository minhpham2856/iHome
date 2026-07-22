using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for RoomType.
	public class RoomTypeRepository
	{
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public RoomTypeRepository() : this(new IHomeDbContext())
		{
		}

		public RoomTypeRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert and save.
		public bool Add(RoomType roomType)
		{
			// Stage the new room type entity for insert in the change tracker
			_context.RoomTypes.Add(roomType);
			// Persist the insert and return whether at least one row was written
			return _context.SaveChanges() > 0;
		}

		// Query ByProperty records.
		public List<RoomType> GetByProperty(int propertyId)
		{
			// Load room types for one property with related rooms and property, ordered by type name
			return _context.RoomTypes
				.Include(rt => rt.Rooms)
				.Include(rt => rt.Property)
				.Where(rt => rt.PropertyId == propertyId)
				.OrderBy(rt => rt.TypeName)
				.ToList();
		}

		// Load one record by id.
		public RoomType GetById(int id)
		{
			// Load one room type by id with its rooms and parent property for edit/detail views
			return _context.RoomTypes
				.Include(rt => rt.Rooms)
				.Include(rt => rt.Property)
				.FirstOrDefault(rt => rt.Id == id);
		}

		// Persist changes to an existing record.
		public void Update(RoomType changes)
		{
			// Load the tracked room type row matching the incoming entity id
			var existing = _context.RoomTypes.FirstOrDefault(rt => rt.Id == changes.Id);
			// Exit quietly when the target row no longer exists
			if (existing == null) return;

			// Map editable scalar fields from the incoming entity onto the tracked row
			existing.TypeName = changes.TypeName;
			existing.MaxOccupancy = changes.MaxOccupancy;
			existing.Area = changes.Area;
			existing.BaseRent = changes.BaseRent;
			existing.Description = changes.Description;
			// Commit the updated room type columns to the database
			_context.SaveChanges();
		}

		// Delete when allowed by rules.
		public bool Delete(RoomType roomType)
		{
			// Guard against null room type input before lookup
			ArgumentNullException.ThrowIfNull(roomType);
			// Load the room type with its rooms collection to enforce referential safety before delete
			var existing = _context.RoomTypes
				.Include(rt => rt.Rooms)
				.FirstOrDefault(rt => rt.Id == roomType.Id);
			// Return false when the room type was already deleted or never existed
			if (existing == null) return false;
			// Block delete when any room still references this room type
			if (existing.Rooms.Count > 0) return false;

			// Remove the room type row from the change tracker
			_context.RoomTypes.Remove(existing);
			// Persist the delete and report whether a row was removed
			return _context.SaveChanges() > 0;
		}

		// HasRooms — public entry point.
		public bool HasRooms(RoomType roomType)
		{
			// Guard against null room type input before existence check
			ArgumentNullException.ThrowIfNull(roomType);
			// Return true when at least one room row references this room type id
			return _context.Rooms.Any(r => r.RoomTypeId == roomType.Id);
		}
	}
}
