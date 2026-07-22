using System;

namespace iHome.BLL.DTOs.Manager
{
	// Editable tenant identity fields for manager create/edit tenant dialogs.
	public class TenantFormDto
	{
		// Tenant primary key; zero when creating a new tenant.
		public int Id { get; set; }
		// Full name from the form.
		public string FullName { get; set; } = string.Empty;
		// Date of birth from the form.
		public DateOnly DateOfBirth { get; set; }
		// ID card number from the form.
		public string IdCardNumber { get; set; } = string.Empty;
		// Phone number from the form.
		public string PhoneNumber { get; set; } = string.Empty;
		// Optional email from the form.
		public string? Email { get; set; }
		// Optional permanent address from the form.
		public string? PermanentAddress { get; set; }
	}
}
