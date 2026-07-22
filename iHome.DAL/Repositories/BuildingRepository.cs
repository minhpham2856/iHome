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
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public BuildingRepository() : this(new IHomeDbContext())
		{
		}

		public BuildingRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert and save.
		public bool Add(Building newBuilding)
		{
			// Stage the new building entity for insert in the change tracker
			_context.Buildings.Add(newBuilding);
			// Persist the insert and return whether at least one row was written
			return _context.SaveChanges() > 0;
		}

		// Query All records.
		public List<Building> GetAll()
		{
			// Load all building rows without filters, includes, or ordering
			return _context.Buildings.ToList();
		}

		// Query ByProperty records.
		public List<Building> GetByProperty(int propertyId)
		{
			// Query buildings belonging to one property, include assigned manager, active first then by name
			return _context.Buildings
				.Include(b => b.Manager)
				.Where(b => b.PropertyId == propertyId)
				.OrderByDescending(b => b.IsActive)
				.ThenBy(b => b.Name)
				.ToList();
		}

		// Query ByLandlord records.
		public List<Building> GetByLandlord(int landlordId)
		{
			// Query buildings whose parent property is owned by the given landlord, with manager and property loaded
			return _context.Buildings
				.Include(b => b.Manager)
				.Include(b => b.Property)
				.Where(b => b.Property.LandlordId == landlordId)
				.OrderByDescending(b => b.IsActive)
				.ThenBy(b => b.Name)
				.ToList();
		}

		// Query ByManager records.
		public List<Building> GetByManager(int managerId)
		{
			// Query buildings directly assigned to the manager, ordered by property name then building name
			return _context.Buildings
				.Include(b => b.Property)
				.Where(b => b.ManagerId == managerId)
				.OrderBy(b => b.Property.Name)
				.ThenBy(b => b.Name)
				.ToList();
		}

		// Assign manager to unassigned buildings or buildings already owned by this manager; never overwrite another manager
		public void SyncManagerAssignments(int propertyId, int managerId, IReadOnlyCollection<int> buildingIds)
		{
			// Convert the selected building id list into a HashSet for fast membership checks inside the loop
			var selected = buildingIds.ToHashSet();
			// Load every building row for the target property into memory for in-place manager updates
			var propertyBuildings = _context.Buildings
				.Where(b => b.PropertyId == propertyId)
				.ToList();

			// Walk each building in the property and assign or clear the manager according to selection rules
			foreach (var building in propertyBuildings)
			{
				if (selected.Contains(building.Id))
				{
					// Assign this manager only when the building is unassigned or already owned by the same manager
					if (building.ManagerId == null || building.ManagerId == managerId)
					{
						building.ManagerId = managerId;
					}
				}
				else if (building.ManagerId == managerId)
				{
					// When deselected, clear the assignment only if this manager currently owns the building
					building.ManagerId = null;
				}
			}

			// Commit all manager assignment changes for the property in one transaction
			_context.SaveChanges();
		}

		// Clear all buildings assigned to this manager across every property
		public void ClearManagerAssignments(User manager)
		{
			// Guard against null manager input before querying by manager id
			ArgumentNullException.ThrowIfNull(manager);
			// Find every building currently assigned to this manager and detach the assignment
			foreach (var building in _context.Buildings.Where(b => b.ManagerId == manager.Id))
			{
				building.ManagerId = null;
			}
			// Persist the cleared assignments across all affected buildings
			_context.SaveChanges();
		}

		// Load one record by id.
		public Building GetById(int id)
		{
			// Load one building by primary key with manager and parent property navigation properties
			return _context.Buildings
				.Include(b => b.Manager)
				.Include(b => b.Property)
				.FirstOrDefault(b => b.Id == id);
		}

		// Persist changes to an existing record.
		public void Update(Building newBuilding)
		{
			// Load the tracked building row that matches the incoming entity id
			var building = _context.Buildings.FirstOrDefault(b => b.Id == newBuilding.Id);
			// Exit quietly when the target row no longer exists
			if (building == null) return;

			// Map scalar fields from the incoming DTO/entity onto the tracked database row
			building.Name = newBuilding.Name;
			building.NumberOfFloors = newBuilding.NumberOfFloors;
			building.Description = newBuilding.Description;
			building.IsActive = newBuilding.IsActive;
			building.ManagerId = newBuilding.ManagerId;
			// Write the updated building columns to the database
			_context.SaveChanges();
		}

		// Disable when allowed by rules.
		public bool Disable(Building building)
		{
			// Guard against null building input before lookup
			ArgumentNullException.ThrowIfNull(building);
			// Reload the building row from the database by id to ensure we mutate a tracked entity
			var existing = _context.Buildings.FirstOrDefault(b => b.Id == building.Id);
			// Return false when the building was already deleted or never existed
			if (existing == null) return false;

			// Soft-disable the building by flipping IsActive to false
			existing.IsActive = false;
			// Persist the deactivation and report whether a row was updated
			return _context.SaveChanges() > 0;
		}
	}
}
