using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class RoomRepository
	{
		private readonly IHomeDbContext _context;

		// build the context internally — no constructor injection by design
		public RoomRepository()
		{
			_context = new IHomeDbContext();
		}

		public List<Room> GetAll() => _context.Rooms.ToList();

		// count rooms whose Status equals the given value (e.g. "Occupied", "Vacant", "Maintenance")
		public int CountByStatus(string status) =>
			_context.Rooms.Count(r => r.Status == status);
	}
}
