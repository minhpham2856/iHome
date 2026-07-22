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
	// Hóa đơn Manager: danh sách, draft tính tiền, tạo/sửa/xóa trong phạm vi tòa được phân công
	public class InvoiceService
	{
		private readonly ManagerOperationsRepository _repository = new();

		// Lưới hóa đơn kèm số đã thu, còn phải thu và trạng thái hiển thị
		public List<InvoiceDto> GetInvoices(int managerId, int? buildingId = null)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			return _repository.GetInvoices(managerId, buildingId)
				.Select(invoice =>
				{
					decimal paid = invoice.Payments.Sum(payment => payment.Amount);
					bool isPaid = string.Equals(invoice.Status, InvoiceStatus.Paid, StringComparison.OrdinalIgnoreCase);
					return new InvoiceDto
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
						// Paid: Balance = 0 dù chưa có Payment (tránh KPI Còn phải thu cộng nhầm)
						Balance = isPaid ? 0m : Math.Max(0m, invoice.TotalAmount - paid),
						Status = invoice.Status,
						StatusDisplay = FormatStatus(invoice.Status, invoice.DueDate)
					};
				})
				.ToList();
		}

		// Xem/sửa hóa đơn đã lưu — dòng tính là Stored (không tính lại chỉ số)
		public InvoiceFormDto GetInvoice(int managerId, int invoiceId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
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

		// Draft tháng: tiền phòng + dịch vụ gán phòng (Fixed/PerPerson/Metered)
		public InvoiceFormDto GetInvoiceDraft(int managerId, int contractId, DateTime invoiceDate)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			if (contractId <= 0)
			{
				throw new ArgumentException("Vui lòng chọn hợp đồng.");
			}

			var contract = _repository.GetInvoiceBillingContract(managerId, contractId);
			DateOnly billingDate = DateOnly.FromDateTime(invoiceDate);
			if (!string.Equals(contract.Status, ContractStatus.Active, StringComparison.OrdinalIgnoreCase) ||
				billingDate < contract.StartDate ||
				billingDate > contract.EndDate)
			{
				throw new InvalidOperationException("Chỉ có thể lập hóa đơn trong thời hạn của hợp đồng đang hoạt động.");
			}

			// Mốc đầu tháng — cắt chỉ số cũ trước tháng lập HĐ
			DateTime monthStart = new(invoiceDate.Year, invoiceDate.Month, 1);
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

			// PerPerson: nhân số khách; Metered: chờ nhập chỉ số; còn lại: cố định theo phòng
			int tenantCount = contract.ContractTenants.Select(item => item.TenantId).Distinct().Count();
			foreach (var assignment in contract.Room.RoomServices
				.Where(item => item.IsActive && item.Service.IsActive)
				.OrderBy(item => item.Service.ServiceName))
			{
				var service = assignment.Service;
				if (string.Equals(service.CalculationMethod, CalculationMethod.Metered, StringComparison.OrdinalIgnoreCase))
				{
					// Chỉ số gần nhất trước tháng lập HĐ; null = chưa có → UI cho nhập chỉ số cũ
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
						CanEditPreviousReading = !storedPrevious.HasValue,
						Quantity = 0,
						Amount = 0
					});
					continue;
				}

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

		// Tạo hóa đơn — tính lại server-side từ draft + chỉ số đồng hồ
		public int CreateInvoice(int managerId, InvoiceFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			// Tính lại từ draft để không tin số tiền client gửi lên
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

				// Khách/phòng mới: ưu tiên chỉ số cũ user nhập; đã có lịch sử: dùng DB
				decimal previousReading = item.CanEditPreviousReading
					? submitted.PreviousReading ?? item.PreviousReading ?? 0m
					: item.PreviousReading ?? 0m;
				if (item.CanEditPreviousReading && !submitted.PreviousReading.HasValue)
				{
					throw new ArgumentException($"Vui lòng nhập chỉ số cũ cho dịch vụ {item.Description}.");
				}
				if (submitted.CurrentReading.Value < previousReading)
				{
					throw new ArgumentException($"Chỉ số mới của {item.Description} không được nhỏ hơn chỉ số cũ.");
				}

				item.PreviousReading = previousReading;
				item.CurrentReading = submitted.CurrentReading.Value;
				item.Quantity = submitted.CurrentReading.Value - previousReading;
				item.Amount = item.Quantity * item.UnitPrice;
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

			calculated.DueDate = input.DueDate;
			calculated.Status = input.Status;
			calculated.TotalAmount = calculated.Items.Sum(item => item.Amount);
			FormValidation.ValidateInvoice(calculated, true);
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

		// Chỉ cập nhật header (hạn/trạng thái/tổng) — không tính lại dòng
		public void UpdateInvoice(int managerId, InvoiceFormDto input)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			FormValidation.ValidateInvoice(input, false);
			_repository.UpdateInvoice(managerId, ToEntity(input));
		}

		public void DeleteInvoice(int managerId, int invoiceId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			_repository.DeleteInvoice(managerId, invoiceId);
		}

		private static Invoice ToEntity(InvoiceFormDto input) => new()
		{
			Id = input.Id,
			ContractId = input.ContractId,
			InvoiceDate = input.InvoiceDate.Date,
			DueDate = input.DueDate,
			TotalAmount = input.TotalAmount,
			Status = input.Status
		};

		// Paid giữ Paid; chưa thanh toán quá hạn → Overdue
		private static string FormatStatus(string status, DateOnly dueDate)
		{
			if (string.Equals(status, InvoiceStatus.Paid, StringComparison.OrdinalIgnoreCase))
			{
				return InvoiceStatus.Paid;
			}
			return dueDate < DateOnly.FromDateTime(DateTime.Today)
				? InvoiceStatus.Overdue
				: InvoiceStatus.Unpaid;
		}
	}
}
