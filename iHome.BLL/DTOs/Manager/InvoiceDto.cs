using iHome.BLL.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace iHome.BLL.DTOs.Manager
{
	// Invoice summary row for the manager invoice list grid.
	public class InvoiceDto
	{
		// Invoice primary key.
		public int Id { get; set; }
		// Contract this invoice was generated for.
		public int ContractId { get; set; }
		// Building name for display in the invoice grid.
		public string BuildingName { get; set; } = string.Empty;
		// Room number billed on this invoice.
		public string RoomNumber { get; set; } = string.Empty;
		// Primary tenant name for quick identification.
		public string MainTenantName { get; set; } = string.Empty;
		// Date/time the invoice was created.
		public DateTime InvoiceDate { get; set; }
		// Last date payment is expected without penalty.
		public DateOnly DueDate { get; set; }
		// Total amount billed including all line items.
		public decimal TotalAmount { get; set; }
		// Amount already received from the tenant.
		public decimal PaidAmount { get; set; }
		// Remaining amount owed (TotalAmount minus PaidAmount).
		public decimal Balance { get; set; }
		// Raw status code (Unpaid, Partial, Paid, Overdue, …).
		public string Status { get; set; } = string.Empty;
		// Vietnamese status label for grid binding.
		public string StatusDisplay { get; set; } = string.Empty;
	}

	// Payload for creating or editing an invoice and its line items on the manager form.
	public class InvoiceFormDto
	{
		// Invoice primary key; zero when creating.
		public int Id { get; set; }
		// Contract being billed.
		public int ContractId { get; set; }
		// Room Id denormalized for service lookup and validation.
		public int RoomId { get; set; }
		// Invoice issue date from the form.
		public DateTime InvoiceDate { get; set; }
		// Payment due date from the form.
		public DateOnly DueDate { get; set; }
		// Computed or entered total; updated as line items change.
		public decimal TotalAmount { get; set; }
		// Invoice status from the form; defaults to unpaid for new invoices.
		public string Status { get; set; } = InvoiceStatus.Unpaid;
		// Editable line items bound to the invoice detail grid.
		public List<InvoiceLineDto> Items { get; set; } = new();
	}

	// One billable line on an invoice form; supports INotifyPropertyChanged for live total recalculation.
	public class InvoiceLineDto : INotifyPropertyChanged
	{
		private decimal _quantity;
		private decimal _amount;
		private decimal? _previousReading;
		private decimal? _currentReading;

		// Linked service Id; null for ad-hoc or rent lines not tied to a catalog service.
		public int? ServiceId { get; set; }
		// Line description shown in the invoice grid (service name or custom text).
		public string Description { get; set; } = string.Empty;
		// Billing unit for this line (kWh, m³, month, …).
		public string Unit { get; set; } = string.Empty;
		// How this line amount is derived (Metered, PerRoom, Rent, …).
		public string CalculationMethod { get; set; } = string.Empty;
		public decimal Quantity
		{
			get => _quantity;
			set
			{
				// Skip INotifyPropertyChanged when value unchanged — avoids redundant UI refresh
				if (_quantity == value) return;
				// Store new billed quantity (units consumed or headcount)
				_quantity = value;
				// Notify WPF binding to recalculate line amount display
				OnPropertyChanged();
			}
		}
		// Price per unit before quantity or meter delta is applied.
		public decimal UnitPrice { get; set; }
		public decimal Amount
		{
			get => _amount;
			set
			{
				// No-op when amount unchanged — prevents binding loop churn
				if (_amount == value) return;
				// Line total = quantity × unit price (or meter delta × price)
				_amount = value;
				OnPropertyChanged();
			}
		}
		public decimal? PreviousReading
		{
			get => _previousReading;
			set
			{
				// Ignore duplicate assignment for meter previous index
				if (_previousReading == value) return;
				// Prior month meter reading — baseline for consumption calculation
				_previousReading = value;
				OnPropertyChanged();
			}
		}
		public decimal? CurrentReading
		{
			get => _currentReading;
			set
			{
				// Ignore duplicate assignment for meter current index
				if (_currentReading == value) return;
				// Current month meter reading — user enters on invoice form
				_currentReading = value;
				OnPropertyChanged();
			}
		}
		// true khi DB chưa có chỉ số trước tháng lập HĐ → UI cho nhập chỉ số cũ (khách/phòng mới)
		public bool CanEditPreviousReading { get; set; }
		// True when line item uses metered billing — UI shows previous/current reading fields
		public bool RequiresReading =>
			string.Equals(CalculationMethod, Enums.CalculationMethod.Metered, StringComparison.OrdinalIgnoreCase);
		// Map internal CalculationMethod code to Vietnamese label for grid column
		public string CalculationDisplay => CalculationMethod switch
		{
			"Rent" => "Tiền phòng",
			_ when string.Equals(CalculationMethod, Enums.CalculationMethod.Metered, StringComparison.OrdinalIgnoreCase)
				=> Enums.CalculationMethod.Metered,
			_ when string.Equals(CalculationMethod, Enums.CalculationMethod.PerPerson, StringComparison.OrdinalIgnoreCase)
				=> Enums.CalculationMethod.PerPerson,
			_ when string.Equals(CalculationMethod, Enums.CalculationMethod.PerRoom, StringComparison.OrdinalIgnoreCase)
				=> Enums.CalculationMethod.PerRoom,
			"Stored" => "Đã tính",
			_ => "Theo tháng"
		};

		// Raised when Quantity, Amount, or meter readings change so the form can refresh totals.
		public event PropertyChangedEventHandler? PropertyChanged;

		// Raise PropertyChanged for WPF two-way bindings on invoice line grid
		private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
