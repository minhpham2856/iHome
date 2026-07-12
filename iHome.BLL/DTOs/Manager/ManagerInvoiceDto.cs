using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace iHome.BLL.DTOs
{
	public class ManagerInvoiceDto
	{
		public int Id { get; set; }
		public int ContractId { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public string MainTenantName { get; set; } = string.Empty;
		public DateTime InvoiceDate { get; set; }
		public DateOnly DueDate { get; set; }
		public decimal TotalAmount { get; set; }
		public decimal PaidAmount { get; set; }
		public decimal Balance { get; set; }
		public string Status { get; set; } = string.Empty;
		public string StatusDisplay { get; set; } = string.Empty;
	}

	public class ManagerInvoiceFormDto
	{
		public int Id { get; set; }
		public int ContractId { get; set; }
		public int RoomId { get; set; }
		public DateTime InvoiceDate { get; set; }
		public DateOnly DueDate { get; set; }
		public decimal TotalAmount { get; set; }
		public string Status { get; set; } = "Unpaid";
		public List<ManagerInvoiceLineDto> Items { get; set; } = new();
	}

	public class ManagerInvoiceLineDto : INotifyPropertyChanged
	{
		private decimal _quantity;
		private decimal _amount;

		public int? ServiceId { get; set; }
		public string Description { get; set; } = string.Empty;
		public string Unit { get; set; } = string.Empty;
		public string CalculationMethod { get; set; } = string.Empty;
		public decimal Quantity
		{
			get => _quantity;
			set
			{
				if (_quantity == value) return;
				_quantity = value;
				OnPropertyChanged();
			}
		}
		public decimal UnitPrice { get; set; }
		public decimal Amount
		{
			get => _amount;
			set
			{
				if (_amount == value) return;
				_amount = value;
				OnPropertyChanged();
			}
		}
		public decimal? PreviousReading { get; set; }
		public decimal? CurrentReading { get; set; }
		public bool RequiresReading =>
			string.Equals(CalculationMethod, "Metered", StringComparison.OrdinalIgnoreCase);
		public string CalculationDisplay => CalculationMethod switch
		{
			"Rent" => "Tiền phòng",
			"Metered" => "Theo chỉ số",
			"PerPerson" => "Theo người",
			"Stored" => "Đã tính",
			_ => "Theo tháng"
		};

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
