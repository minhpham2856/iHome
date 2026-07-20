using iHome.BLL.DTOs;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerInvoiceService
	{
		private const string ActiveContract = "Active";
		private const string MeteredMethod = "Metered";
		private readonly ManagerOperationsRepository _repository = new();

		public List<ManagerInvoiceDto> GetInvoices(int managerId, int? buildingId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			return _repository.GetInvoices(managerId, buildingId)
				.Select(invoice =>
				{
					decimal paid = invoice.Payments.Sum(payment => payment.Amount);
					bool isPaid = string.Equals(invoice.Status, "Paid", StringComparison.OrdinalIgnoreCase);
					return new ManagerInvoiceDto
					{
						Id = invoice.Id,
						ContractId = invoice.ContractId,
						BuildingName = invoice.Contract.Room.Building.Name,
						RoomNumber = invoice.Contract.Room.RoomNumber,
						MainTenantName = invoice.Contract.ContractTenants
							.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant.FullName ?? "Chưa có",
						InvoiceDate = invoice.InvoiceDate,
						DueDate = invoice.DueDate,
						TotalAmount = invoice.TotalAmount,
						PaidAmount = paid,
						// Hóa đơn đã thanh toán không còn số dư phải thu
						Balance = isPaid ? 0m : Math.Max(0m, invoice.TotalAmount - paid),
						Status = invoice.Status,
						StatusDisplay = FormatStatus(invoice.Status, invoice.DueDate)
					};
				})
				.ToList();
		}

		public ManagerInvoiceFormDto GetInvoice(int managerId, int invoiceId)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			var invoice = _repository.GetInvoices(managerId)
				.SingleOrDefault(item => item.Id == invoiceId)
				?? throw new UnauthorizedAccessException("Bạn không có quyền xem hóa đơn này.");
			return new ManagerInvoiceFormDto
			{
				Id = invoice.Id,
				ContractId = invoice.ContractId,
				RoomId = invoice.Contract.RoomId,
				InvoiceDate = invoice.InvoiceDate,
				DueDate = invoice.DueDate,
				TotalAmount = invoice.TotalAmount,
				Status = invoice.Status,
				Items = invoice.InvoiceItems
					.OrderBy(item => item.Id)
					.Select(item => new ManagerInvoiceLineDto
					{
						Description = item.Description,
						Quantity = item.Quantity,
						UnitPrice = item.UnitPrice,
						Amount = item.Amount,
						CalculationMethod = "Stored"
					})
					.ToList()
			};
		}

		public ManagerInvoiceFormDto GetInvoiceDraft(int managerId, int contractId, DateTime invoiceDate)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			if (contractId <= 0)
			{
				throw new ArgumentException("Vui lòng chọn hợp đồng.");
			}

			var contract = _repository.GetInvoiceBillingContract(managerId, contractId);
			DateOnly billingDate = DateOnly.FromDateTime(invoiceDate);
			if (!string.Equals(contract.Status, ActiveContract, StringComparison.OrdinalIgnoreCase) ||
				billingDate < contract.StartDate ||
				billingDate > contract.EndDate)
			{
				throw new InvalidOperationException("Chỉ có thể lập hóa đơn trong thời hạn của hợp đồng đang hoạt động.");
			}

			DateTime monthStart = new(invoiceDate.Year, invoiceDate.Month, 1);
			var items = new List<ManagerInvoiceLineDto>
			{
				new()
				{
					Description = $"Tiền phòng tháng {invoiceDate:MM/yyyy}",
					Unit = "Tháng",
					CalculationMethod = "Rent",
					Quantity = 1,
					UnitPrice = contract.MonthlyRent,
					Amount = contract.MonthlyRent
				}
			};

			int tenantCount = contract.ContractTenants.Select(item => item.TenantId).Distinct().Count();
			foreach (var assignment in contract.Room.RoomServices
				.Where(item => item.IsActive && item.Service.IsActive)
				.OrderBy(item => item.Service.ServiceName))
			{
				var service = assignment.Service;
				if (string.Equals(service.CalculationMethod, MeteredMethod, StringComparison.OrdinalIgnoreCase))
				{
					decimal previousReading = assignment.ServiceReadings
						.Where(reading => reading.ReadingDate < monthStart)
						.OrderByDescending(reading => reading.ReadingDate)
						.Select(reading => (decimal?)reading.CurrentReading)
						.FirstOrDefault() ?? 0m;
					items.Add(new ManagerInvoiceLineDto
					{
						ServiceId = service.Id,
						Description = service.ServiceName,
						Unit = service.Unit,
						CalculationMethod = service.CalculationMethod,
						UnitPrice = service.UnitPrice,
						PreviousReading = previousReading,
						Quantity = 0,
						Amount = 0
					});
					continue;
				}

				decimal quantity = string.Equals(
					service.CalculationMethod,
					"PerPerson",
					StringComparison.OrdinalIgnoreCase)
					? tenantCount
					: 1m;
				items.Add(new ManagerInvoiceLineDto
				{
					ServiceId = service.Id,
					Description = $"{service.ServiceName} tháng {invoiceDate:MM/yyyy}",
					Unit = service.Unit,
					CalculationMethod = service.CalculationMethod,
					Quantity = quantity,
					UnitPrice = service.UnitPrice,
					Amount = quantity * service.UnitPrice
				});
			}

			return new ManagerInvoiceFormDto
			{
				ContractId = contractId,
				RoomId = contract.RoomId,
				InvoiceDate = invoiceDate.Date,
				DueDate = DateOnly.FromDateTime(invoiceDate.Date.AddDays(7)),
				Status = "Unpaid",
				Items = items,
				TotalAmount = items.Sum(item => item.Amount)
			};
		}

		public int CreateInvoice(int managerId, ManagerInvoiceFormDto input)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			var calculated = GetInvoiceDraft(managerId, input.ContractId, input.InvoiceDate);
			var readings = new List<ServiceReading>();
			DateTime readingDate = new DateTime(input.InvoiceDate.Year, input.InvoiceDate.Month, 1)
				.AddMonths(1)
				.AddSeconds(-1);

			foreach (var item in calculated.Items.Where(item => item.RequiresReading))
			{
				var submitted = input.Items.SingleOrDefault(candidate => candidate.ServiceId == item.ServiceId);
				if (submitted?.CurrentReading == null)
				{
					throw new ArgumentException($"Vui lòng nhập chỉ số mới cho dịch vụ {item.Description}.");
				}
				if (submitted.CurrentReading.Value < item.PreviousReading!.Value)
				{
					throw new ArgumentException($"Chỉ số mới của {item.Description} không được nhỏ hơn chỉ số cũ.");
				}

				item.CurrentReading = submitted.CurrentReading.Value;
				item.Quantity = submitted.CurrentReading.Value - item.PreviousReading!.Value;
				item.Amount = item.Quantity * item.UnitPrice;
				item.Description = string.Format(
					CultureInfo.CurrentCulture,
					"{0} ({1:N2} - {2:N2} {3})",
					item.Description,
					item.PreviousReading,
					item.CurrentReading,
					item.Unit);
				readings.Add(new ServiceReading
				{
					RoomId = calculated.RoomId,
					ServiceId = item.ServiceId!.Value,
					ReadingDate = readingDate,
					CurrentReading = submitted.CurrentReading.Value
				});
			}

			calculated.DueDate = input.DueDate;
			calculated.Status = input.Status;
			calculated.TotalAmount = calculated.Items.Sum(item => item.Amount);
			ManagerValidation.ValidateInvoice(calculated, true);
			var invoiceItems = calculated.Items.Select(item => new InvoiceItem
			{
				Description = item.Description,
				Quantity = item.Quantity,
				UnitPrice = item.UnitPrice,
				Amount = item.Amount
			}).ToList();
			return _repository.CreateInvoice(
				managerId,
				ToEntity(calculated),
				invoiceItems,
				readings).Id;
		}

		public void UpdateInvoice(int managerId, ManagerInvoiceFormDto input)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			ManagerValidation.ValidateInvoice(input, false);
			_repository.UpdateInvoice(managerId, ToEntity(input));
		}

		public void DeleteInvoice(int managerId, int invoiceId)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			_repository.DeleteInvoice(managerId, invoiceId);
		}

		private static Invoice ToEntity(ManagerInvoiceFormDto input) => new()
		{
			Id = input.Id,
			ContractId = input.ContractId,
			InvoiceDate = input.InvoiceDate.Date,
			DueDate = input.DueDate,
			TotalAmount = input.TotalAmount,
			Status = input.Status
		};

		private static string FormatStatus(string status, DateOnly dueDate)
		{
			if (status == "Paid")
			{
				return "Đã thanh toán";
			}
			return dueDate < DateOnly.FromDateTime(DateTime.Today) ? "Quá hạn" : "Chưa thanh toán";
		}
	}
}
