using System;

namespace iHome.BLL.DTOs
{
	public class ManagerTenantFormDto
	{
		public int Id { get; set; }
		public string FullName { get; set; } = string.Empty;
		public DateOnly DateOfBirth { get; set; }
		public string IdCardNumber { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string? Email { get; set; }
		public string? PermanentAddress { get; set; }
	}
}
