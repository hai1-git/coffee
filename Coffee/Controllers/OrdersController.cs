using Coffee.Data;
using Coffee.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly CoffeeShopDbContext db;

        public OrdersController(CoffeeShopDbContext context)
        {
            db = context;
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirst("UserId")?.Value, out var userId) ? userId : 0;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var orders = db.Orders
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.OrderDate)
                .Select(order => new OrderHistoryItemViewModel
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate ?? DateTime.UtcNow,
                    ReceiverName = order.ReceiverName ?? (order.User != null ? order.User.UserName : string.Empty),
                    ReceiverPhone = order.ReceiverPhone ?? (order.User != null ? order.User.Phone ?? string.Empty : string.Empty),
                    ShippingAddress = order.ShippingAddress ?? (order.User != null ? order.User.Address ?? string.Empty : string.Empty),
                    Status = order.Status ?? "Cho xac nhan",
                    PaymentMethod = order.Payments
                        .Select(payment => payment.PaymentMethod)
                        .FirstOrDefault() ?? "COD",
                    PaymentStatus = order.Payments
                        .Select(payment => payment.PaymentStatus)
                        .FirstOrDefault() ?? "Chua thanh toan",
                    TotalAmount = order.TotalAmount ?? 0,
                    TotalItems = order.OrderDetails.Sum(detail => detail.Quantity ?? 0)
                })
                .ToList();

            return View(new OrderHistoryViewModel
            {
                Orders = orders
            });
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var order = db.Orders
                .AsNoTracking()
                .Where(x => x.OrderId == id && x.UserId == userId)
                .Select(order => new OrderDetailViewModel
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate ?? DateTime.UtcNow,
                    ReceiverName = order.ReceiverName ?? (order.User != null ? order.User.UserName : string.Empty),
                    ReceiverPhone = order.ReceiverPhone ?? (order.User != null ? order.User.Phone ?? string.Empty : string.Empty),
                    ShippingAddress = order.ShippingAddress ?? (order.User != null ? order.User.Address ?? string.Empty : string.Empty),
                    Status = order.Status ?? "Cho xac nhan",
                    PaymentMethod = order.Payments
                        .Select(payment => payment.PaymentMethod)
                        .FirstOrDefault() ?? "COD",
                    PaymentStatus = order.Payments
                        .Select(payment => payment.PaymentStatus)
                        .FirstOrDefault() ?? "Chua thanh toan",
                    TransactionId = order.Payments
                        .Select(payment => payment.TransactionId)
                        .FirstOrDefault() ?? string.Empty,
                    TotalAmount = order.TotalAmount ?? 0,
                    Items = order.OrderDetails
                        .Select(detail => new OrderDetailItemViewModel
                        {
                            ProductId = detail.ProductId ?? 0,
                            ProductName = detail.Product != null
                                ? detail.Product.ProductName ?? "San pham"
                                : "San pham",
                            Price = detail.Price ?? 0,
                            Quantity = detail.Quantity ?? 0
                        })
                        .ToList()
                })
                .FirstOrDefault();

            if (order == null)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }
    }
}
