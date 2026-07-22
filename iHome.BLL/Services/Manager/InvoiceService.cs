using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Service hóa đơn Manager: danh sách, draft tính tiền, tạo/sửa/xóa trong phạm vi tòa được phân công
	public class InvoiceService
	{
		// Read/write invoice operations scoped to manager buildings
		private readonly ManagerOperationsRepository _repository = new();

		// Grid: invoices with paid amount, balance, and display status
		public List<InvoiceDto> GetInvoices(int managerId, int? buildingId = null)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			return _repository.GetInvoices(managerId, buildingId)
				.Select(invoice =>
				{
					// Sum all payment rows linked to this invoice
					decimal paid = invoice.Payments.Sum(payment => payment.Amount);
					// Check DB status — Paid invoices always show zero balance
					bool isPaid = string.Equals(invoice.Status, InvoiceStatus.Paid, StringComparison.OrdinalIgnoreCase);
					return new InvoiceDto
					{
						Id = invoice.Id,
						ContractId = invoice.ContractId,
						BuildingName = invoice.Contract.Room.Building.Name,
						RoomNumber = invoice.Contract.Room.RoomNumber,
						// Main tenant name from ContractTenants or fallback label
						MainTenantName = invoice.Contract.ContractTenants
							.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant.FullName ?? "Chưa có",
						InvoiceDate = invoice.InvoiceDate,
						DueDate = invoice.DueDate,
						TotalAmount = invoice.TotalAmount,
						PaidAmount = paid,
						// Paid: Balance = 0 dù chưa có bản ghi Payment (tránh KPI Còn phải thu cộng nhầm)
						Balance = isPaid ? 0m : Math.Max(0m, invoice.TotalAmount - paid),
						Status = invoice.Status,
						// Derive Overdue display when unpaid and past due date
						StatusDisplay = FormatStatus(invoice.Status, invoice.DueDate)
					};
				})
				.ToList();
		}

		// Load existing invoice for view/edit — line items marked CalculationMethod=Stored
		public InvoiceFormDto GetInvoice(int managerId, int invoiceId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// Invoice must belong to manager's building scope
			var invoice = _repository.GetInvoices(managerId)
				.SingleOrDefault(item => item.Id == invoiceId)
				?? throw new UnauthorizedAccessException("Bạn không có quyền xem hóa đơn này.");
			return new InvoiceFormDto
			{
				Id = invoice.Id,
				ContractId = invoice.ContractId,
				RoomId = invoice.Contract.RoomId,
				InvoiceDate = invoice.InvoiceDate,
				DueDate = invoice.DueDate,
				TotalAmount = invoice.TotalAmount,
				Status = invoice.Status,
				// Map persisted InvoiceItems — no meter recalc on edit load
				Items = invoice.InvoiceItems
					.OrderBy(item => item.Id)
					.Select(item => new InvoiceLineDto
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

		// Draft hóa đơn tháng: tiền phòng + dịch vụ gán phòng (Fixed/PerPerson/Metered)
		public InvoiceFormDto GetInvoiceDraft(int managerId, int contractId, DateTime invoiceDate)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// Contract selection required before billing calculation
			if (contractId <= 0)
			{
				throw new ArgumentException("Vui lòng chọn hợp đồng.");
			}

			// Load contract with room, services, readings — manager scope enforced in DAL
			var contract = _repository.GetInvoiceBillingContract(managerId, contractId);
			DateOnly billingDate = DateOnly.FromDateTime(invoiceDate);
			// Billing date must fall inside active contract term
			if (!string.Equals(contract.Status, ContractStatus.Active, StringComparison.OrdinalIgnoreCase) ||
				billingDate < contract.StartDate ||
				billingDate > contract.EndDate)
			{
				throw new InvalidOperationException("Chỉ có thể lập hóa đơn trong thời hạn của hợp đồng đang hoạt động.");
			}

			// First instant of billing month — cutoff for prior meter readings
			DateTime monthStart = new(invoiceDate.Year, invoiceDate.Month, 1);
			// Start with rent line item — always one month at contract MonthlyRent
			var items = new List<InvoiceLineDto>
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

			// PerPerson: nhân số khách trên hợp đồng; Metered: chờ nhập chỉ số; còn lại: cố định theo phòng
			int tenantCount = contract.ContractTenants.Select(item => item.TenantId).Distinct().Count();
			// Iterate active room-service assignments sorted by service name
			foreach (var assignment in contract.Room.RoomServices
				.Where(item => item.IsActive && item.Service.IsActive)
				.OrderBy(item => item.Service.ServiceName))
			{
				var service = assignment.Service;
				// Metered services need user-supplied current/previous readings
				if (string.Equals(service.CalculationMethod, CalculationMethod.Metered, StringComparison.OrdinalIgnoreCase))
				{
					// Lấy chỉ số gần nhất trước tháng lập HĐ; null = chưa có → UI cho nhập chỉ số cũ
					decimal? storedPrevious = assignment.ServiceReadings
						.Where(reading => reading.ReadingDate < monthStart)
						.OrderByDescending(reading => reading.ReadingDate)
						.Select(reading => (decimal?)reading.CurrentReading)
						.FirstOrDefault();
					items.Add(new InvoiceLineDto
					{
						ServiceId = service.Id,
						Description = service.ServiceName,
						Unit = service.Unit,
						CalculationMethod = service.CalculationMethod,
						UnitPrice = service.UnitPrice,
						PreviousReading = storedPrevious ?? 0m,
						// true when no prior reading in DB — user must enter previous index
						CanEditPreviousReading = !storedPrevious.HasValue,
						Quantity = 0,
						Amount = 0
					});
					continue;
				}

				// PerPerson multiplies by tenant count; PerRoom uses quantity 1
				decimal quantity = string.Equals(
					service.CalculationMethod,
					CalculationMethod.PerPerson,
					StringComparison.OrdinalIgnoreCase)
					? tenantCount
					: 1m;
				items.Add(new InvoiceLineDto
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

			// Return draft form — due date defaults to invoice date + 7 days
			return new InvoiceFormDto
			{
				ContractId = contractId,
				RoomId = contract.RoomId,
				InvoiceDate = invoiceDate.Date,
				DueDate = DateOnly.FromDateTime(invoiceDate.Date.AddDays(7)),
				Status = InvoiceStatus.Unpaid,
				Items = items,
				TotalAmount = items.Sum(item => item.Amount)
			};
		}

		// Persist new invoice — recalculates amounts server-side from draft + meter readings
		public int CreateInvoice(int managerId, InvoiceFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// Tính lại từ draft để không tin số tiền client gửi lên
			var calculated = GetInvoiceDraft(managerId, input.ContractId, input.InvoiceDate);
			// ServiceReading rows to insert for metered line items
			var readings = new List<ServiceReading>();
			// End of billing month timestamp for new meter readings
			DateTime readingDate = new DateTime(input.InvoiceDate.Year, input.InvoiceDate.Month, 1)
				.AddMonths(1)
				.AddSeconds(-1);

			// Process each metered line — match submitted readings by ServiceId
			foreach (var item in calculated.Items.Where(item => item.RequiresReading))
			{
				// Find user-submitted line with same service id
				var submitted = input.Items.SingleOrDefault(candidate => candidate.ServiceId == item.ServiceId);
				// Current reading mandatory for metered services
				if (submitted?.CurrentReading == null)
				{
					throw new ArgumentException($"Vui lòng nhập chỉ số mới cho dịch vụ {item.Description}.");
				}

				// Khách/phòng mới: ưu tiên chỉ số cũ user nhập; phòng đã có lịch sử: dùng DB
				decimal previousReading = item.CanEditPreviousReading
					? submitted.PreviousReading ?? item.PreviousReading ?? 0m
					: item.PreviousReading ?? 0m;
				// New tenant/room must supply previous reading when DB has none
				if (item.CanEditPreviousReading && !submitted.PreviousReading.HasValue)
				{
					throw new ArgumentException($"Vui lòng nhập chỉ số cũ cho dịch vụ {item.Description}.");
				}
				// Consumption cannot be negative
				if (submitted.CurrentReading.Value < previousReading)
				{
					throw new ArgumentException($"Chỉ số mới của {item.Description} không được nhỏ hơn chỉ số cũ.");
				}

				// Apply calculated quantity and amount to draft line
				item.PreviousReading = previousReading;
				item.CurrentReading = submitted.CurrentReading.Value;
				item.Quantity = submitted.CurrentReading.Value - previousReading;
				item.Amount = item.Quantity * item.UnitPrice;
				// Enrich description with reading range for printed invoice
				item.Description = string.Format(
					CultureInfo.CurrentCulture,
					"{0} ({1:N2} - {2:N2} {3})",
					item.Description,
					item.PreviousReading,
					item.CurrentReading,
					item.Unit);
				// Lưu chỉ số mới để tháng sau lấy làm chỉ số cũ
				readings.Add(new ServiceReading
				{
					RoomId = calculated.RoomId,
					ServiceId = item.ServiceId!.Value,
					ReadingDate = readingDate,
					CurrentReading = submitted.CurrentReading.Value
				});
			}

			// Apply user-edited due date and status from form
			calculated.DueDate = input.DueDate;
			calculated.Status = input.Status;
			// Re-sum total after meter lines updated
			calculated.TotalAmount = calculated.Items.Sum(item => item.Amount);
			FormValidation.ValidateInvoice(calculated, true);
			// Map line DTOs → InvoiceItem entities for INSERT
			var invoiceItems = calculated.Items.Select(item => new InvoiceItem
			{
				Description = item.Description,
				Quantity = item.Quantity,
				UnitPrice = item.UnitPrice,
				Amount = item.Amount
			}).ToList();
			// Atomic create: invoice + items + meter readings
			return _repository.CreateInvoice(
				managerId,
				ToEntity(calculated),
				invoiceItems,
				readings).Id;
		}

		// Update invoice header fields only (due date, status, total) — not line recalc
		public void UpdateInvoice(int managerId, InvoiceFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			FormValidation.ValidateInvoice(input, false);
			_repository.UpdateInvoice(managerId, ToEntity(input));
		}

		// Delete invoice when no payments block removal
		public void DeleteInvoice(int managerId, int invoiceId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			_repository.DeleteInvoice(managerId, invoiceId);
		}

		// Map form → Invoice entity (header fields only)
		private static Invoice ToEntity(InvoiceFormDto input) => new()
		{
			Id = input.Id,
			ContractId = input.ContractId,
			InvoiceDate = input.InvoiceDate.Date,
			DueDate = input.DueDate,
			TotalAmount = input.TotalAmount,
			Status = input.Status
		};

		// Display status: Paid stays Paid; unpaid past due → Overdue; else Unpaid
		private static string FormatStatus(string status, DateOnly dueDate)
		{
			// Paid invoices never shown as overdue regardless of due date
			if (string.Equals(status, InvoiceStatus.Paid, StringComparison.OrdinalIgnoreCase))
			{
				return InvoiceStatus.Paid;
			}
			// Unpaid + due date before today → Overdue label for grid highlighting
			return dueDate < DateOnly.FromDateTime(DateTime.Today)
				? InvoiceStatus.Overdue
				: InvoiceStatus.Unpaid;
		}
	}
}
