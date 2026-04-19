using System.Linq;
using Coffee.Data;
using Coffee.Models;
using Coffee.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Coffee.Controllers
{
    [Authorize] // 🔥 bắt buộc login mới dùng giỏ hàng
    public class CartController : Controller
    {
        private readonly CoffeeShopDbContext db;

        public CartController(CoffeeShopDbContext context)
        {
            db = context;
        }

        // =========================
        // 🔥 LẤY USER ID TỪ COOKIE
        // =========================
        private int GetUserId()
        {
            return int.Parse(User.FindFirst("UserId").Value);
        }

        // =========================
        // 🛒 ADD TO CART
        // =========================
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity)
        {
            int userId = GetUserId();

            quantity = Math.Max(1, Math.Min(99, quantity));

            var cartItems = db.Carts.Where(x => x.UserId == userId);

            var item = cartItems.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
            {
                if (cartItems.Count() >= 99)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Giỏ hàng tối đa 99 loại sản phẩm!"
                    });
                }

                db.Carts.Add(new Cart
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity = Math.Min(99, (item.Quantity ?? 0) + quantity);
            }

            db.SaveChanges();

            return Json(new
            {
                success = true,
                cartCount = cartItems.Count()
            });
        }

        // =========================
        // 📦 CART PAGE
        // =========================
        public IActionResult Index()
        {
            int userId = GetUserId();

            var items = db.Carts
                .Where(x => x.UserId == userId)
                .Join(db.Products,
                    c => c.ProductId,
                    p => p.ProductId,
                    (c, p) => new CartItemViewModel
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price ?? 0,
                        Quantity = c.Quantity ?? 0
                    })
                .ToList();

            return View(new CartViewModel
            {
                Items = items
            });
        }

        // =========================
        // ➕ ➖ UPDATE
        // =========================
        [HttpPost]
        public IActionResult UpdateQty(int productId, bool isPlus)
        {
            int userId = GetUserId();

            var item = db.Carts
                .FirstOrDefault(x => x.UserId == userId && x.ProductId == productId);

            if (item == null)
                return Json(new { success = false });

            int qty = item.Quantity ?? 1;

            qty = isPlus
                ? Math.Min(99, qty + 1)
                : Math.Max(1, qty - 1);

            item.Quantity = qty;

            db.SaveChanges();

            return Json(BuildCartResponse(userId, productId, qty));
        }

        // =========================
        // 🔥 SET QTY
        // =========================
        [HttpPost]
        public IActionResult SetQty(int productId, int quantity)
        {
            int userId = GetUserId();

            var item = db.Carts
                .FirstOrDefault(x => x.UserId == userId && x.ProductId == productId);

            if (item == null)
                return Json(new { success = false });

            quantity = Math.Max(1, Math.Min(99, quantity));

            item.Quantity = quantity;

            db.SaveChanges();

            return Json(BuildCartResponse(userId, productId, quantity));
        }

        // =========================
        // 🗑 REMOVE
        // =========================
        [HttpPost]
        public IActionResult RemoveItem(int productId)
        {
            int userId = GetUserId();

            var item = db.Carts
                .FirstOrDefault(x => x.UserId == userId && x.ProductId == productId);

            if (item == null)
                return Json(new { success = false });

            db.Carts.Remove(item);
            db.SaveChanges();

            return Json(new
            {
                success = true,
                cartCount = db.Carts.Count(x => x.UserId == userId),
                total = GetCartTotal(userId)
            });
        }

        // =========================
        // 🔢 CART COUNT
        // =========================
        [HttpGet]
        public IActionResult GetCartCount()
        {
            int userId = GetUserId();

            int count = db.Carts.Count(x => x.UserId == userId);

            return Json(new { count });
        }

        // =========================
        // 💰 TOTAL
        // =========================
        private decimal GetCartTotal(int userId)
        {
            return db.Carts
                .Where(x => x.UserId == userId)
                .Join(db.Products,
                    c => c.ProductId,
                    p => p.ProductId,
                    (c, p) => (p.Price ?? 0) * (c.Quantity ?? 0))
                .Sum();
        }

        // =========================
        // 🔥 RESPONSE
        // =========================
        private object BuildCartResponse(int userId, int productId, int quantity)
        {
            var price = db.Products
                .Where(x => x.ProductId == productId)
                .Select(x => x.Price ?? 0)
                .FirstOrDefault();

            return new
            {
                quantity = quantity,
                subTotal = price * quantity,
                cartCount = db.Carts.Count(x => x.UserId == userId),
                total = GetCartTotal(userId)
            };
        }
    }
}