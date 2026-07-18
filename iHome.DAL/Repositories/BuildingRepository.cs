using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iHome.DAL.Repositories
{
	public class BuildingRepository
	{
		private readonly IHomeDbContext _context;

		public BuildingRepository()
		{
			_context = new IHomeDbContext();
		}

		public bool Add(Building newBuilding)
		{
			_context.Buildings.Add(newBuilding);
			return _context.SaveChanges() > 0;
		}

		public List<Building> GetAll() => _context.Buildings.ToList();
		public List<Building> GetAll(int id) => _context.Buildings.ToList();
		public Building GetById(int id) => _context.Buildings.FirstOrDefault(b => b.Id == id);
		public void Update(Building newBuilding)
		{
			var building = _context.Buildings.FirstOrDefault(b => b.Id == newBuilding.Id);
			if (building != null)
			{
				building.Name = newBuilding.Name;
				building.NumberOfFloors = newBuilding.NumberOfFloors;
				_context.SaveChanges();
			}
		}

	}
}
