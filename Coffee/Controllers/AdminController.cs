using Coffee.Data;
using Coffee.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly CoffeeShopDbContext db;

    public AdminController(CoffeeShopDbContext context)
    {
        db = context;
    }

    public IActionResult Index()
    {
        var now = DateTime.UtcNow;
        var firstMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

        var totalProducts = db.Products.AsNoTracking().Count();
        var totalCategories = db.Categories.AsNoTracking().Count();
        var totalOrders = db.Orders.AsNoTracking().Count();
        var totalCustomers = db.Users.AsNoTracking().Count(x => x.RoleId != 1);
        var totalRevenue = db.Orders.AsNoTracking().Sum(x => x.TotalAmount) ?? 0;
        var pendingOrders = db.Orders.AsNoTracking().Count(x => (x.Status ?? "Cho xac nhan") == "Cho xac nhan");

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
                Status = x.Status ?? "Cho xac nhan",
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
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Status) ? "Cho xac nhan" : x.Status.Trim())
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

        var model = new AdminDashboardViewModel
        {
            TotalProducts = totalProducts,
            TotalCategories = totalCategories,
            TotalOrders = totalOrders,
            TotalCustomers = totalCustomers,
            TotalRevenue = totalRevenue,
            AverageOrderValue = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 0) : 0,
            PendingOrders = pendingOrders,
            ProductsByCategory = categoryStats,
            MonthlyStats = monthlyStats,
            OrderStatusStats = orderStatusStats,
            TopProducts = topProducts,
            RecentOrders = recentOrders
        };

        return View(model);
    }
}
