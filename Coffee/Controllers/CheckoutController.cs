using Coffee.Data;
using Coffee.DTO;
using Coffee.Models;
using Coffee.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Coffee.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private const string CashOnDelivery = "COD";

        private readonly CoffeeShopDbContext db;

        public CheckoutController(CoffeeShopDbContext context)
        {
            db = context;
        }

        private int GetUserId()
        {
            return int.TryParse(User.FindFirst("UserId")?.Value, out var userId) ? userId : 0;
        }

        private User? GetCurrentUser()
        {
            var userId = GetUserId();
            return userId > 0 ? db.Users.FirstOrDefault(x => x.UserId == userId) : null;
        }

        [HttpGet]
        public IActionResult Index(string items)
        {
            var model = BuildCheckoutViewModel(items);

            if (model == null)
            {
                TempData["CheckoutError"] = "Khong tim thay san pham hop le de thanh toan.";
                return RedirectToAction("Index", "Cart");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult StartCheckout(List<int>? selectedProductIds, Dictionary<int, int>? quantities)
        {
            var selectedItems = BuildSelectedItems(selectedProductIds, quantities);
            var model = BuildCheckoutViewModel(selectedItems);

            if (model == null)
            {
                TempData["CheckoutError"] = "Vui long chon it nhat mot san pham hop le de thanh toan.";
                return RedirectToAction("Index", "Cart");
            }

            return View("Index", model);
        }

        [HttpGet]
        public IActionResult BuyNow(int productId, int quantity)
        {
            var selectedItems = new List<CartItemDTO>
            {
                new CartItemDTO
                {
                    ProductId = productId,
                    Quantity = quantity
                }
            };

            var model = BuildCheckoutViewModel(selectedItems);

            if (model == null)
            {
                TempData["CheckoutError"] = "Khong the mo trang thanh toan cho san pham nay.";
                return RedirectToAction("Index", "Products");
            }

            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(CheckoutViewModel model)
        {
            var checkoutModel = BuildCheckoutViewModel(model.SelectedItemsJson);

            if (checkoutModel == null)
            {
                TempData["CheckoutError"] = "Khong tim thay san pham hop le de thanh toan.";
                return RedirectToAction("Index", "Cart");
            }

            checkoutModel.PaymentMethod = CashOnDelivery;
            checkoutModel.ReceiverName = (model.ReceiverName ?? string.Empty).Trim();
            checkoutModel.ReceiverPhone = (model.ReceiverPhone ?? string.Empty).Trim();
            checkoutModel.ShippingAddress = (model.ShippingAddress ?? string.Empty).Trim();

            var userId = GetUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            TryValidateModel(checkoutModel);
            if (!ModelState.IsValid)
            {
                return View(checkoutModel);
            }

            using var transaction = db.Database.BeginTransaction();

            try
            {
                var user = GetCurrentUser();
                if (user == null)
                {
                    transaction.Rollback();
                    return RedirectToAction("Login", "Auth");
                }

                user.Phone = checkoutModel.ReceiverPhone;
                user.Address = checkoutModel.ShippingAddress;

                var order = new Order
                {
                    UserId = userId,
                    ReceiverName = checkoutModel.ReceiverName,
                    ReceiverPhone = checkoutModel.ReceiverPhone,
                    ShippingAddress = checkoutModel.ShippingAddress,
                    TotalAmount = checkoutModel.Total,
                    Status = "Cho xac nhan",
                    OrderDate = DateTime.UtcNow
                };

                db.Orders.Add(order);
                db.SaveChanges();

                var orderDetails = checkoutModel.Items.Select(item => new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });

                db.OrderDetails.AddRange(orderDetails);

                db.Payments.Add(new Payment
                {
                    OrderId = order.OrderId,
                    PaymentMethod = CashOnDelivery,
                    PaymentStatus = "Thanh toan khi nhan hang",
                    TransactionId = Guid.NewGuid().ToString("N").ToUpperInvariant()
                });

                db.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                ModelState.AddModelError(string.Empty, "Khong the xu ly thanh toan luc nay. Vui long thu lai.");
                return View(checkoutModel);
            }

            TempData["CheckoutSuccess"] = "Dat hang COD thanh cong. Shop se giao den nha va ban thanh toan khi nhan hang.";
            return RedirectToAction("Index", "Cart");
        }

        private CheckoutViewModel? BuildCheckoutViewModel(string? items)
        {
            return BuildCheckoutViewModel(ParseSelectedItems(items));
        }

        private CheckoutViewModel? BuildCheckoutViewModel(IEnumerable<CartItemDTO> selectedItems)
        {
            var userId = GetUserId();
            if (userId <= 0)
            {
                return null;
            }

            var normalizedItems = selectedItems
                .Where(x => x.ProductId > 0)
                .GroupBy(x => x.ProductId)
                .Select(group => new CartItemDTO
                {
                    ProductId = group.Key,
                    Quantity = Math.Max(1, Math.Min(99, group.Last().Quantity))
                })
                .ToList();

            if (!normalizedItems.Any())
            {
                return null;
            }

            var selectedIndexes = normalizedItems
                .Select((item, index) => new { item.ProductId, index })
                .ToDictionary(x => x.ProductId, x => x.index);

            var selectedQuantityMap = normalizedItems.ToDictionary(x => x.ProductId, x => x.Quantity);
            var productIds = normalizedItems.Select(x => x.ProductId).ToList();

            var products = db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.ProductId))
                .ToList();

            var items = products
                .Select(product => new CartItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName ?? "San pham",
                    Price = product.Price,
                    Quantity = selectedQuantityMap[product.ProductId]
                })
                .ToList()
                .OrderBy(x => selectedIndexes[x.ProductId])
                .ToList();

            if (!items.Any())
            {
                return null;
            }

            var user = GetCurrentUser();

            return new CheckoutViewModel
            {
                Items = items,
                PaymentMethod = CashOnDelivery,
                ReceiverName = user?.UserName ?? User.Identity?.Name ?? string.Empty,
                ReceiverPhone = user?.Phone ?? string.Empty,
                ShippingAddress = user?.Address ?? string.Empty,
                SelectedItemsJson = JsonSerializer.Serialize(items.Select(x => new CartItemDTO
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity
                }))
            };
        }

        private static List<CartItemDTO> ParseSelectedItems(string? items)
        {
            if (string.IsNullOrWhiteSpace(items))
            {
                return new List<CartItemDTO>();
            }

            try
            {
                var decodedItems = Uri.UnescapeDataString(items);
                return JsonSerializer.Deserialize<List<CartItemDTO>>(decodedItems) ?? new List<CartItemDTO>();
            }
            catch
            {
                return new List<CartItemDTO>();
            }
        }

        private static List<CartItemDTO> BuildSelectedItems(List<int>? selectedProductIds, Dictionary<int, int>? quantities)
        {
            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                return new List<CartItemDTO>();
            }

            quantities ??= new Dictionary<int, int>();

            return selectedProductIds
                .Distinct()
                .Select(productId => new CartItemDTO
                {
                    ProductId = productId,
                    Quantity = quantities.TryGetValue(productId, out var quantity) ? quantity : 1
                })
                .ToList();
        }

    }
}
