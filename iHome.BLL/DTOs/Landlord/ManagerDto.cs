using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
	// Manager account row for the landlord manager management grid.
	public class ManagerDto
	{
		// Manager user primary key.
		public int Id { get; set; }
		// Manager full name shown in the list.
		public string FullName { get; set; } = string.Empty;
		// Login username for the manager account.
		public string Username { get; set; } = string.Empty;
		// Email address used for notifications and login recovery.
		public string Email { get; set; } = string.Empty;
		// Optional contact phone number.
		public string? PhoneNumber { get; set; }
		// Whether the account can sign in and operate.
		public bool IsActive { get; set; }
		// Vietnamese active/inactive label for the status column.
		public string StatusDisplay { get; set; } = string.Empty;
		// Primary property this manager is scoped to.
		public int PropertyId { get; set; }
		// Name of the manager's assigned property.
		public string PropertyName { get; set; } = string.Empty;
		// Comma-separated or summarized list of buildings under this manager.
		public string AssignedBuildingsSummary { get; set; } = string.Empty;
		// Count of buildings currently assigned to the manager.
		public int AssignedBuildingCount { get; set; }
	}

	// Editable manager profile and building assignment payload for create/edit forms.
	public class ManagerFormDto
	{
		// Manager user primary key; zero when creating a new account.
		public int Id { get; set; }
		// Full name from the form.
		public string FullName { get; set; } = string.Empty;
		// Email from the form.
		public string Email { get; set; } = string.Empty;
		// Optional phone from the form.
		public string? PhoneNumber { get; set; }
		// Active flag from the form; defaults to true for new managers.
		public bool IsActive { get; set; } = true;
		// Property the manager is responsible for.
		public int PropertyId { get; set; }
		// Building Ids selected for assignment on the form.
		public List<int> BuildingIds { get; set; } = new();
	}

	// Property entry for manager create/edit property dropdown.
	public class ManagerPropertyOptionDto
	{
		// Property primary key.
		public int Id { get; set; }
		// Property name shown in the ComboBox.
		public string Name { get; set; } = string.Empty;
	}

	// Building row in the manager assignment checklist with ownership hints.
	public class ManagerBuildingOptionDto
	{
		// Building primary key.
		public int BuildingId { get; set; }
		// Parent property Id for grouping buildings.
		public int PropertyId { get; set; }
		// Parent property name for context in the checklist.
		public string PropertyName { get; set; } = string.Empty;
		// Building name shown in the assignment list.
		public string BuildingName { get; set; } = string.Empty;
		// Whether the landlord has ticked this building for the current manager.
		public bool IsSelected { get; set; }
		// Id of another manager already assigned; null when unassigned.
		public int? CurrentManagerId { get; set; }
		// Name of the manager currently holding this building; null when free.
		public string? CurrentManagerName { get; set; }
		// True when another manager owns the building and it cannot be selected.
		public bool IsAssignedToOther { get; set; }
		// False when the building is blocked because it belongs to another manager.
		public bool CanSelect => !IsAssignedToOther;
		// Label for the checklist; appends current assignee when building is taken.
		public string DisplayName =>
			IsAssignedToOther
				? $"{BuildingName} (đang gán: {CurrentManagerName})"
				: BuildingName;
	}

	// Credentials returned after creating a manager account (shown once to the landlord).
	public class ManagerAccountCreatedDto
	{
		// Newly created user Id.
		public int UserId { get; set; }
		// Generated login username to share with the manager.
		public string Username { get; set; } = string.Empty;
		// One-time temporary password before the manager changes it.
		public string TemporaryPassword { get; set; } = string.Empty;
		// True when the welcome email was sent successfully.
		public bool EmailSent { get; set; }
		// SMTP or delivery error message when EmailSent is false.
		public string? EmailError { get; set; }
	}
}
