using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace iHome.DAL.Repositories
{
	public class PropertyRepository
	{
		private readonly IHomeDbContext _context;

		public PropertyRepository() : this(new IHomeDbContext()) { }

		public PropertyRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		public bool Add(Property newProperty)
		{
			_context.Properties.Add(newProperty);
			return _context.SaveChanges() > 0;
		}

		public List<Property> GetByLandlord(int landlordId) =>
			_context.Properties
				.Include(p => p.Buildings)
				.Where(p => p.LandlordId == landlordId)
				.OrderByDescending(p => p.IsActive)
				.ThenBy(p => p.Name)
				.ToList();

		public List<Property> GetAll(int landlordId) => GetByLandlord(landlordId);

		public Property GetById(int id) => _context.Properties.Include(p => p.Buildings).FirstOrDefault(p => p.Id == id);

		public void Update(Property newProperty)
		{
			var property = _context.Properties.FirstOrDefault(p => p.Id == newProperty.Id);
			if (property == null) return;

			property.Name = newProperty.Name;
			property.Address = newProperty.Address;
			property.Description = newProperty.Description;
			property.IsActive = newProperty.IsActive;
			property.UpdatedAt = DateTime.Now;

			// sync every building of this property to the same active status
			foreach (var building in _context.Buildings.Where(b => b.PropertyId == newProperty.Id))
			{
				building.IsActive = newProperty.IsActive;
			}

			_context.SaveChanges();
		}

		public bool Disable(int id)
		{
			var property = _context.Properties.FirstOrDefault(p => p.Id == id);
			if (property == null) return false;

			property.IsActive = false;
			property.UpdatedAt = DateTime.Now;
			foreach (var building in _context.Buildings.Where(b => b.PropertyId == id))
			{
				building.IsActive = false;
			}

			return _context.SaveChanges() > 0;
		}
	}
}
