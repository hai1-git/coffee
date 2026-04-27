using Coffee.Data;
using Coffee.Helper;
using Coffee.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private const string CashOnDelivery = OrderStatusHelper.CodPaymentMethod;
    private const string MomoPaymentMethod = "MOMO";

    private readonly CoffeeShopDbContext db;

    public AdminController(CoffeeShopDbContext context)
    {
        db = context;
    }

    public IActionResult Index()
    {
        var now = DateTime.UtcNow;
        var firstMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        var unpaidStatusNormalized = OrderStatusHelper.UnpaidStatus.ToUpperInvariant();
        var paidStatusNormalized = OrderStatusHelper.PaidStatus.ToUpperInvariant();

        var totalProducts = db.Products.AsNoTracking().Count();
        var totalCategories = db.Categories.AsNoTracking().Count();
        var totalOrders = db.Orders.AsNoTracking().Count();
        var totalCustomers = db.Users.AsNoTracking().Count(x => x.RoleId != 1);
        var totalRevenue = db.Orders.AsNoTracking().Sum(x => x.TotalAmount) ?? 0;
        var pendingOrders = db.Orders.AsNoTracking()
            .Count(x => (x.Status ?? OrderStatusHelper.UnpaidStatus).ToUpper() == unpaidStatusNormalized);
        var pendingCodOrders = db.Orders
            .AsNoTracking()
            .Where(x => (x.Status ?? OrderStatusHelper.UnpaidStatus).ToUpper() == unpaidStatusNormalized)
            .Count(x => x.Payments.Any(p => (p.PaymentMethod ?? string.Empty).ToUpper() == CashOnDelivery));
        var paidMomoOrders = db.Payments
            .AsNoTracking()
            .Count(x => (x.PaymentMethod ?? string.Empty).ToUpper() == MomoPaymentMethod
                && (x.PaymentStatus ?? string.Empty).ToUpper() == paidStatusNormalized);

        var categoryStats = db.Categories
            .AsNoTracking()
            .Select(category => new AdminChartItemViewModel
            {
                Label = category.CategoryName ?? "Chua dat ten",
                Value = category.Products.Count()
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var uncategorizedProducts = db.Products.AsNoTracking().Count(x => x.CategoryId == null);
        if (uncategorizedProducts > 0)
        {
            categoryStats.Add(new AdminChartItemViewModel
            {
                Label = "Chua phan loai",
                Value = uncategorizedProducts
            });
        }

        var orderRows = db.Orders
            .AsNoTracking()
            .Where(x => x.OrderDate != null)
            .Select(x => new
            {
                x.OrderId,
                OrderDate = x.OrderDate ?? DateTime.UtcNow,
                Status = x.Status ?? OrderStatusHelper.UnpaidStatus,
                TotalAmount = x.TotalAmount ?? 0,
                CustomerName = !string.IsNullOrEmpty(x.ReceiverName)
                    ? x.ReceiverName
                    : x.User != null ? x.User.UserName ?? "Khach hang" : "Khach hang"
            })
            .ToList();

        var monthlyStats = Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var month = firstMonth.AddMonths(offset);
                var monthOrders = orderRows
                    .Where(x => x.OrderDate.Year == month.Year && x.OrderDate.Month == month.Month)
                    .ToList();

                return new AdminMonthlyStatViewModel
                {
                    Label = $"T{month.Month:00}/{month.Year}",
                    OrderCount = monthOrders.Count,
                    Revenue = monthOrders.Sum(x => x.TotalAmount)
                };
            })
            .ToList();

        var orderStatusStats = orderRows
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Status) ? OrderStatusHelper.UnpaidStatus : x.Status.Trim())
            .Select(group => new AdminChartItemViewModel
            {
                Label = group.Key,
                Value = group.Count()
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var topProducts = db.OrderDetails
            .AsNoTracking()
            .Select(detail => new
            {
                ProductName = detail.Product != null
                    ? detail.Product.ProductName ?? "San pham"
                    : "San pham",
                Quantity = detail.Quantity ?? 0,
                Revenue = (detail.Price ?? 0) * (detail.Quantity ?? 0)
            })
            .ToList()
            .GroupBy(x => x.ProductName)
            .Select(group => new AdminTopProductViewModel
            {
                ProductName = group.Key,
                QuantitySold = group.Sum(x => x.Quantity),
                Revenue = group.Sum(x => x.Revenue)
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.Revenue)
            .Take(6)
            .ToList();

        var recentOrders = orderRows
            .OrderByDescending(x => x.OrderDate)
            .Take(6)
            .Select(x => new AdminRecentOrderViewModel
            {
                OrderId = x.OrderId,
                CustomerName = x.CustomerName,
                Status = x.Status,
                TotalAmount = x.TotalAmount,
                OrderDate = x.OrderDate
            })
            .ToList();

        var pendingCodOrderList = db.Orders
            .AsNoTracking()
            .Where(x => (x.Status ?? OrderStatusHelper.UnpaidStatus).ToUpper() == unpaidStatusNormalized)
            .Where(x => x.Payments.Any(p => (p.PaymentMethod ?? string.Empty).ToUpper() == CashOnDelivery))
            .OrderByDescending(x => x.OrderDate)
            .Select(x => new AdminPendingCodOrderViewModel
            {
                OrderId = x.OrderId,
                CustomerName = !string.IsNullOrEmpty(x.ReceiverName)
                    ? x.ReceiverName
                    : x.User != null ? x.User.UserName ?? "Khach hang" : "Khach hang",
                ReceiverPhone = x.ReceiverPhone ?? (x.User != null ? x.User.Phone ?? string.Empty : string.Empty),
                ShippingAddress = x.ShippingAddress ?? (x.User != null ? x.User.Address ?? string.Empty : string.Empty),
                Status = x.Status ?? OrderStatusHelper.UnpaidStatus,
                PaymentStatus = x.Payments
                    .OrderBy(p => p.PaymentId)
                    .Select(p => p.PaymentStatus)
                    .FirstOrDefault() ?? OrderStatusHelper.UnpaidStatus,
                TotalAmount = x.TotalAmount ?? 0,
                OrderDate = x.OrderDate ?? DateTime.UtcNow
            })
            .Take(8)
            .ToList();

        var model = new AdminDashboardViewModel
        {
            TotalProducts = totalProducts,
            TotalCategories = totalCategories,
            TotalOrders = totalOrders,
            TotalCustomers = totalCustomers,
            TotalRevenue = totalRevenue,
            AverageOrderValue = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 0) : 0,
            PendingOrders = pendingOrders,
            PendingCodOrders = pendingCodOrders,
            PaidMomoOrders = paidMomoOrders,
            ProductsByCategory = categoryStats,
            MonthlyStats = monthlyStats,
            OrderStatusStats = orderStatusStats,
            TopProducts = topProducts,
            RecentOrders = recentOrders,
            PendingCodOrderList = pendingCodOrderList
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveCodOrder(int id)
    {
        var order = db.Orders
            .Include(x => x.Payments)
            .FirstOrDefault(x => x.OrderId == id);

        if (order == null)
        {
            TempData["Error"] = $"Khong tim thay don COD #{id}.";
            return RedirectToAction(nameof(Index));
        }

        var payment = order.Payments.FirstOrDefault(x =>
            string.Equals(x.PaymentMethod, CashOnDelivery, StringComparison.OrdinalIgnoreCase));

        if (payment == null)
        {
            TempData["Error"] = $"Don #{id} khong phai thanh toan COD.";
            return RedirectToAction(nameof(Index));
        }

        if (OrderStatusHelper.IsCancelled(order.Status))
        {
            TempData["Error"] = $"Don COD #{id} da huy, khong the duyet thanh toan.";
            return RedirectToAction(nameof(Index));
        }

        if (OrderStatusHelper.IsPaid(order.Status))
        {
            TempData["Success"] = $"Don COD #{id} da o trang thai {order.Status}.";
            return RedirectToAction(nameof(Index));
        }

        order.Status = OrderStatusHelper.PaidStatus;
        payment.PaymentStatus = OrderStatusHelper.PaidStatus;
        db.SaveChanges();

        TempData["Success"] = $"Da cap nhat don COD #{id} sang da thanh toan.";
        return RedirectToAction(nameof(Index));
    }
}
