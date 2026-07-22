using iHome.BLL.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace iHome.BLL.DTOs.Manager
{
	// Dòng lưới hóa đơn Manager.
	public class InvoiceDto
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

	// Form tạo/sửa hóa đơn và các dòng chi tiết.
	public class InvoiceFormDto
	{
		public int Id { get; set; }
		public int ContractId { get; set; }
		public int RoomId { get; set; }
		public DateTime InvoiceDate { get; set; }
		public DateOnly DueDate { get; set; }
		public decimal TotalAmount { get; set; }
		public string Status { get; set; } = InvoiceStatus.Unpaid;
		public List<InvoiceLineDto> Items { get; set; } = new();
	}

	// Một dòng tính tiền trên form hóa đơn — INotifyPropertyChanged để cập nhật tổng.
	public class InvoiceLineDto : INotifyPropertyChanged
	{
		private decimal _quantity;
		private decimal _amount;
		private decimal? _previousReading;
		private decimal? _currentReading;

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
		public decimal? PreviousReading
		{
			get => _previousReading;
			set
			{
				if (_previousReading == value) return;
				_previousReading = value;
				OnPropertyChanged();
			}
		}
		public decimal? CurrentReading
		{
			get => _currentReading;
			set
			{
				if (_currentReading == value) return;
				_currentReading = value;
				OnPropertyChanged();
			}
		}
		public bool CanEditPreviousReading { get; set; }
		public bool RequiresReading =>
			string.Equals(CalculationMethod, Enums.CalculationMethod.Metered, StringComparison.OrdinalIgnoreCase);
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

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
