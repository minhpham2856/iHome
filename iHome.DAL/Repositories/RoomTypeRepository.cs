using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
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

		public bool Add(RoomType roomType)
		{
			_context.RoomTypes.Add(roomType);
			return _context.SaveChanges() > 0;
		}

		public List<RoomType> GetByProperty(int propertyId) =>
			_context.RoomTypes
				.Include(rt => rt.Rooms)
				.Include(rt => rt.Property)
				.Where(rt => rt.PropertyId == propertyId)
				.OrderBy(rt => rt.TypeName)
				.ToList();

		public RoomType GetById(int id) =>
			_context.RoomTypes
				.Include(rt => rt.Rooms)
				.Include(rt => rt.Property)
				.FirstOrDefault(rt => rt.Id == id);

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

		public bool Delete(int id)
		{
			var existing = _context.RoomTypes
				.Include(rt => rt.Rooms)
				.FirstOrDefault(rt => rt.Id == id);
			if (existing == null) return false;
			if (existing.Rooms.Count > 0) return false;

			_context.RoomTypes.Remove(existing);
			return _context.SaveChanges() > 0;
		}

		public bool HasRooms(int roomTypeId) =>
			_context.Rooms.Any(r => r.RoomTypeId == roomTypeId);
	}
}
