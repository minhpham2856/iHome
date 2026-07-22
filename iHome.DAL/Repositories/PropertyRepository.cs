using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Property.
	public class PropertyRepository
	{
		private readonly IHomeDbContext _context;

		public PropertyRepository() : this(new IHomeDbContext()) { }

		public PropertyRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert a new property.
		public bool Add(Property newProperty)
		{
			_context.Properties.Add(newProperty);
			return _context.SaveChanges() > 0;
		}

		// Properties owned by the landlord (active first).
		public List<Property> GetByLandlord(int landlordId)
		{
			return _context.Properties
				.Include(p => p.Buildings)
				.Where(p => p.LandlordId == landlordId)
				.OrderByDescending(p => p.IsActive)
				.ThenBy(p => p.Name)
				.ToList();
		}

		// Alias for GetByLandlord (landlord scope is the effective "all").
		public List<Property> GetAll(int landlordId)
		{
			return GetByLandlord(landlordId);
		}

		// One property with buildings.
		public Property GetById(int id)
		{
			return _context.Properties.Include(p => p.Buildings).FirstOrDefault(p => p.Id == id);
		}

		// Update fields; sync every building's IsActive to match the property.
		public void Update(Property newProperty)
		{
			var property = _context.Properties.FirstOrDefault(p => p.Id == newProperty.Id);
			if (property == null) return;

			property.Name = newProperty.Name;
			property.Address = newProperty.Address;
			property.Description = newProperty.Description;
			property.IsActive = newProperty.IsActive;
			property.UpdatedAt = DateTime.Now;

			foreach (var building in _context.Buildings.Where(b => b.PropertyId == newProperty.Id))
			{
				building.IsActive = newProperty.IsActive;
			}

			_context.SaveChanges();
		}

		// Soft-disable property and cascade IsActive=false to all buildings.
		public bool Disable(Property property)
		{
			ArgumentNullException.ThrowIfNull(property);
			var existing = _context.Properties.FirstOrDefault(p => p.Id == property.Id);
			if (existing == null) return false;

			existing.IsActive = false;
			existing.UpdatedAt = DateTime.Now;
			foreach (var building in _context.Buildings.Where(b => b.PropertyId == property.Id))
			{
				building.IsActive = false;
			}

			return _context.SaveChanges() > 0;
		}
	}
}
