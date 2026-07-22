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
		private readonly IHomeDbContext _context;

		public RoomTypeRepository() : this(new IHomeDbContext())
		{
		}

		public RoomTypeRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert a new room type.
		public bool Add(RoomType roomType)
		{
			_context.RoomTypes.Add(roomType);
			return _context.SaveChanges() > 0;
		}

		// Room types for one property.
		public List<RoomType> GetByProperty(int propertyId)
		{
			return _context.RoomTypes
				.Include(rt => rt.Rooms)
				.Include(rt => rt.Property)
				.Where(rt => rt.PropertyId == propertyId)
				.OrderBy(rt => rt.TypeName)
				.ToList();
		}

		// One room type with rooms and parent property.
		public RoomType GetById(int id)
		{
			return _context.RoomTypes
				.Include(rt => rt.Rooms)
				.Include(rt => rt.Property)
				.FirstOrDefault(rt => rt.Id == id);
		}

		// Update room type fields (including MaxOccupancy ceiling).
		public void Update(RoomType changes)
		{
			var existing = _context.RoomTypes.FirstOrDefault(rt => rt.Id == changes.Id);
			if (existing == null) return;

			existing.TypeName = changes.TypeName;
			existing.MaxOccupancy = changes.MaxOccupancy;
			existing.Area = changes.Area;
			existing.BaseRent = changes.BaseRent;
			existing.Description = changes.Description;
			_context.SaveChanges();
		}

		// Delete only when no room still references this type.
		public bool Delete(RoomType roomType)
		{
			ArgumentNullException.ThrowIfNull(roomType);
			var existing = _context.RoomTypes
				.Include(rt => rt.Rooms)
				.FirstOrDefault(rt => rt.Id == roomType.Id);
			if (existing == null) return false;
			if (existing.Rooms.Count > 0) return false;

			_context.RoomTypes.Remove(existing);
			return _context.SaveChanges() > 0;
		}

		// True when at least one room uses this type.
		public bool HasRooms(RoomType roomType)
		{
			ArgumentNullException.ThrowIfNull(roomType);
			return _context.Rooms.Any(r => r.RoomTypeId == roomType.Id);
		}
	}
}
