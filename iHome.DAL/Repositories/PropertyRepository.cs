using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Property.
	public class PropertyRepository
	{
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public PropertyRepository() : this(new IHomeDbContext()) { }

		public PropertyRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert and save.
		public bool Add(Property newProperty)
		{
			// Stage the new property entity for insert in the change tracker
			_context.Properties.Add(newProperty);
			// Persist the insert and return whether at least one row was written
			return _context.SaveChanges() > 0;
		}

		// Query ByLandlord records.
		public List<Property> GetByLandlord(int landlordId)
		{
			// Load properties owned by the landlord with child buildings, active first then alphabetically
			return _context.Properties
				.Include(p => p.Buildings)
				.Where(p => p.LandlordId == landlordId)
				.OrderByDescending(p => p.IsActive)
				.ThenBy(p => p.Name)
				.ToList();
		}

		// Query All records.
		public List<Property> GetAll(int landlordId)
		{
			// Delegate to GetByLandlord because landlord scope is the effective "all" view for this app
			return GetByLandlord(landlordId);
		}

		// Load one record by id.
		public Property GetById(int id)
		{
			// Load one property by id and eager-load its buildings collection for detail screens
			return _context.Properties.Include(p => p.Buildings).FirstOrDefault(p => p.Id == id);
		}

		// Persist changes to an existing record.
		public void Update(Property newProperty)
		{
			// Load the tracked property row matching the incoming entity id
			var property = _context.Properties.FirstOrDefault(p => p.Id == newProperty.Id);
			// Exit quietly when the target row no longer exists
			if (property == null) return;

			// Map editable scalar fields from the incoming entity onto the tracked row
			property.Name = newProperty.Name;
			property.Address = newProperty.Address;
			property.Description = newProperty.Description;
			property.IsActive = newProperty.IsActive;
			property.UpdatedAt = DateTime.Now;

			// Sync every building of this property to the same active status
			foreach (var building in _context.Buildings.Where(b => b.PropertyId == newProperty.Id))
			{
				building.IsActive = newProperty.IsActive;
			}

			// Commit property and cascading building status updates together
			_context.SaveChanges();
		}

		// Disable when allowed by rules.
		public bool Disable(Property property)
		{
			// Guard against null property input before lookup
			ArgumentNullException.ThrowIfNull(property);
			// Reload the property row from the database by id
			var existing = _context.Properties.FirstOrDefault(p => p.Id == property.Id);
			// Return false when the property was already deleted or never existed
			if (existing == null) return false;

			// Soft-disable the property and stamp the update time
			existing.IsActive = false;
			existing.UpdatedAt = DateTime.Now;
			// Cascade deactivation to every building under this property
			foreach (var building in _context.Buildings.Where(b => b.PropertyId == property.Id))
			{
				building.IsActive = false;
			}

			// Persist property and building deactivations and report whether rows were written
			return _context.SaveChanges() > 0;
		}
	}
}
