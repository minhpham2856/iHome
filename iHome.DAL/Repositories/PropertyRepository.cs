using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iHome.DAL.Repositories
{
	public class PropertyRepository
	{
		private readonly IHomeDbContext _context;

		// build the context internally — no constructor injection by design
		public PropertyRepository()
		{
			_context = new IHomeDbContext();
		}

		public bool Add(Property newProperty)
		{
			_context.Properties.Add(newProperty);
			return _context.SaveChanges() > 0;
		}

		public List<Property> GetAll(int id) => _context.Properties.ToList();
		public Property GetById(int id) => _context.Properties.FirstOrDefault(b => b.Id == id);
		public void Update(Property newProperty)
		{
			var property = _context.Properties.FirstOrDefault(b => b.Id == newProperty.Id);
			if (property != null)
			{
				property.Name = newProperty.Name;
				property.Address = newProperty.Address;
				_context.SaveChanges();
			}
		}

	}
}
