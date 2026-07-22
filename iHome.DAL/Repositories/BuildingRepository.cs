using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Building.
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

		// Insert a new building.
		public bool Add(Building newBuilding)
		{
			_context.Buildings.Add(newBuilding);
			return _context.SaveChanges() > 0;
		}

		// All buildings.
		public List<Building> GetAll()
		{
			return _context.Buildings.ToList();
		}

		// Buildings of one property (active first).
		public List<Building> GetByProperty(int propertyId)
		{
			return _context.Buildings
				.Include(b => b.Manager)
				.Where(b => b.PropertyId == propertyId)
				.OrderByDescending(b => b.IsActive)
				.ThenBy(b => b.Name)
				.ToList();
		}

		// Buildings under properties owned by the landlord.
		public List<Building> GetByLandlord(int landlordId)
		{
			return _context.Buildings
				.Include(b => b.Manager)
				.Include(b => b.Property)
				.Where(b => b.Property.LandlordId == landlordId)
				.OrderByDescending(b => b.IsActive)
				.ThenBy(b => b.Name)
				.ToList();
		}

		// Buildings assigned to the manager.
		public List<Building> GetByManager(int managerId)
		{
			return _context.Buildings
				.Include(b => b.Property)
				.Where(b => b.ManagerId == managerId)
				.OrderBy(b => b.Property.Name)
				.ThenBy(b => b.Name)
				.ToList();
		}

		// Assign manager only to unassigned buildings or ones already owned by this manager.
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
					// Never overwrite another manager's assignment.
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

		// Clear all buildings assigned to this manager across every property.
		public void ClearManagerAssignments(User manager)
		{
			ArgumentNullException.ThrowIfNull(manager);
			foreach (var building in _context.Buildings.Where(b => b.ManagerId == manager.Id))
			{
				building.ManagerId = null;
			}
			_context.SaveChanges();
		}

		// One building with manager and property.
		public Building GetById(int id)
		{
			return _context.Buildings
				.Include(b => b.Manager)
				.Include(b => b.Property)
				.FirstOrDefault(b => b.Id == id);
		}

		// Update building fields.
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

		// Soft-disable a building.
		public bool Disable(Building building)
		{
			ArgumentNullException.ThrowIfNull(building);
			var existing = _context.Buildings.FirstOrDefault(b => b.Id == building.Id);
			if (existing == null) return false;

			existing.IsActive = false;
			return _context.SaveChanges() > 0;
		}
	}
}
