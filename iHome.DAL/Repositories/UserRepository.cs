using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace iHome.DAL.Repositories
{
	public class UserRepository
	{
		private readonly IHomeDbContext _context;

		public UserRepository()
		{
			_context = new IHomeDbContext();
		}

		public void Add(User newUser)
		{
			_context.Users.Add(newUser);
			_context.SaveChanges();
		}

		public User GetById(int id) => _context.Users.FirstOrDefault(u => u.Id == id);
		public User GetByRole(string role) => _context.Users.FirstOrDefault(u => u.Role == role);
		public User GetByUsername(string username) => _context.Users.FirstOrDefault(u => u.Username == username);
	}
}
