using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class RoomRepository
	{
		private readonly IHomeDbContext _context;

		public RoomRepository() : this(new IHomeDbContext())
		{
		}

		public RoomRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		public bool Add(Room room)
		{
			_context.Rooms.Add(room);
			return _context.SaveChanges() > 0;
		}

		public List<Room> GetAll() => _context.Rooms.ToList();

		public int CountByStatus(string status) =>
			_context.Rooms.Count(r => r.Status == status);

		public List<Room> GetByBuilding(int buildingId) =>
			_context.Rooms
				.Include(r => r.RoomType)
				.Include(r => r.Building)
					.ThenInclude(b => b.Property)
				.Include(r => r.Contracts)
					.ThenInclude(c => c.ContractTenants)
				.Where(r => r.BuildingId == buildingId)
				.OrderBy(r => r.Floor)
				.ThenBy(r => r.RoomNumber)
				.ToList();

		public Room GetById(int id) =>
			_context.Rooms
				.Include(r => r.RoomType)
				.Include(r => r.Building)
					.ThenInclude(b => b.Property)
				.Include(r => r.Contracts)
					.ThenInclude(c => c.ContractTenants)
						.ThenInclude(ct => ct.Tenant)
				.FirstOrDefault(r => r.Id == id);

		public bool RoomNumberExists(int buildingId, string roomNumber, int? excludeRoomId = null)
		{
			string normalized = roomNumber.Trim();
			return _context.Rooms.Any(r =>
				r.BuildingId == buildingId
				&& r.RoomNumber == normalized
				&& (!excludeRoomId.HasValue || r.Id != excludeRoomId.Value));
		}

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

		public bool Delete(int id)
		{
			var existing = _context.Rooms
				.Include(r => r.Contracts)
				.FirstOrDefault(r => r.Id == id);
			if (existing == null) return false;
			if (existing.Contracts.Count > 0) return false;

			_context.Rooms.Remove(existing);
			return _context.SaveChanges() > 0;
		}

		public bool HasContracts(int roomId) =>
			_context.Contracts.Any(c => c.RoomId == roomId);
	}
}
