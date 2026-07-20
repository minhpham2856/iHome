using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class BuildingRepository
	{
		private readonly IHomeDbContext _context;

		public BuildingRepository() : this(new IHomeDbContext())
		{
		}

		public BuildingRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		public bool Add(Building newBuilding)
		{
			_context.Buildings.Add(newBuilding);
			return _context.SaveChanges() > 0;
		}

		public List<Building> GetAll() => _context.Buildings.ToList();

		public List<Building> GetByProperty(int propertyId) =>
			_context.Buildings
				.Include(b => b.Manager)
				.Where(b => b.PropertyId == propertyId)
				.OrderByDescending(b => b.IsActive)
				.ThenBy(b => b.Name)
				.ToList();

		public List<Building> GetByLandlord(int landlordId) =>
			_context.Buildings
				.Include(b => b.Manager)
				.Include(b => b.Property)
				.Where(b => b.Property.LandlordId == landlordId)
				.OrderByDescending(b => b.IsActive)
				.ThenBy(b => b.Name)
				.ToList();

		public List<Building> GetByManager(int managerId) =>
			_context.Buildings
				.Include(b => b.Property)
				.Where(b => b.ManagerId == managerId)
				.OrderBy(b => b.Property.Name)
				.ThenBy(b => b.Name)
				.ToList();

		// gán manager cho các tòa trống / đang thuộc manager này; không ghi đè manager khác
		public void SyncManagerAssignments(int propertyId, int managerId, IReadOnlyCollection<int> buildingIds)
		{
			var selected = buildingIds.ToHashSet();
			var propertyBuildings = _context.Buildings
				.Where(b => b.PropertyId == propertyId)
				.ToList();

			foreach (var building in propertyBuildings)
			{
				if (selected.Contains(building.Id))
				{
					if (building.ManagerId == null || building.ManagerId == managerId)
					{
						building.ManagerId = managerId;
					}
				}
				else if (building.ManagerId == managerId)
				{
					building.ManagerId = null;
				}
			}

			_context.SaveChanges();
		}

		// bỏ mọi tòa đang gán cho manager (mọi nhà trọ)
		public void ClearManagerAssignments(int managerId)
		{
			foreach (var building in _context.Buildings.Where(b => b.ManagerId == managerId))
			{
				building.ManagerId = null;
			}
			_context.SaveChanges();
		}

		public Building GetById(int id) =>
			_context.Buildings
				.Include(b => b.Manager)
				.Include(b => b.Property)
				.FirstOrDefault(b => b.Id == id);

		public void Update(Building newBuilding)
		{
			var building = _context.Buildings.FirstOrDefault(b => b.Id == newBuilding.Id);
			if (building == null) return;

			building.Name = newBuilding.Name;
			building.NumberOfFloors = newBuilding.NumberOfFloors;
			building.Description = newBuilding.Description;
			building.IsActive = newBuilding.IsActive;
			building.ManagerId = newBuilding.ManagerId;
			_context.SaveChanges();
		}

		public bool Disable(int id)
		{
			var building = _context.Buildings.FirstOrDefault(b => b.Id == id);
			if (building == null) return false;

			building.IsActive = false;
			return _context.SaveChanges() > 0;
		}
	}
}
