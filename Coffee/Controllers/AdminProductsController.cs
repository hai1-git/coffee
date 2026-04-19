using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Coffee.Data;
using Coffee.Models;

namespace Coffee.Controllers
{
    public class AdminProductsController : Controller
    {
        private readonly CoffeeShopDbContext _context;

        public AdminProductsController(CoffeeShopDbContext context)
        {
            _context = context;
        }

        // 📦 LIST
        public async Task<IActionResult> Index()
        {
            var products = _context.Products.Include(p => p.Category);
            return View(await products.ToListAsync());
        }

        // 🔍 DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // ➕ CREATE (GET)
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(
                _context.Categories, "CategoryId", "CategoryName");
            return View();
        }

        // ➕ CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? file)
        {
            // ===== XỬ LÝ ẢNH =====
            if (file != null && file.Length > 0)
            {
                // ✔ check định dạng
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("", "Chỉ cho phép file ảnh (.jpg, .png, ...)");
                }
                // ✔ check dung lượng (max 2MB)
                else if (file.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Ảnh phải nhỏ hơn 2MB");
                }
                else
                {
                    // ✔ đảm bảo thư mục tồn tại
                    var folder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "img",
                        "Products"
                    );

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    // ✔ tạo tên file random
                    var fileName = Guid.NewGuid().ToString() + ext;

                    var path = Path.Combine(folder, fileName);

                    // ✔ lưu file
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // ✔ lưu đường dẫn DB
                    product.ImageUrl = "/img/Products/" + fileName;
                }
            }

            // 👉 nếu không chọn ảnh → ảnh mặc định
            if (string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = "/img/default.png";
            }

            // ===== SAVE DB =====
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // ===== LOAD LẠI DROPDOWN =====
            ViewData["CategoryId"] = new SelectList(
                _context.Categories,
                "CategoryId",
                "CategoryName",
                product.CategoryId
            );

            return View(product);
        }
        // ✏️ EDIT (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(
                _context.Categories, "CategoryId", "CategoryName", product.CategoryId);

            return View(product);
        }

        // ✏️ EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? file)
        {
            if (id != product.ProductId)
                return NotFound();

            var oldProduct = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (oldProduct == null)
                return NotFound();

            // ===== XỬ LÝ ẢNH =====
            if (file != null && file.Length > 0)
            {
                // 👉 XÓA ẢNH CŨ
                if (!string.IsNullOrEmpty(oldProduct.ImageUrl))
                {
                    var oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        oldProduct.ImageUrl.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                // 👉 LƯU ẢNH MỚI
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                var newPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "img",
                    "Products",
                    fileName
                );

                using (var stream = new FileStream(newPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                product.ImageUrl = "/img/Products/" + fileName;
            }
            else
            {
                // 👉 GIỮ ẢNH CŨ
                product.ImageUrl = oldProduct.ImageUrl;
            }

            // ===== SAVE DB =====
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ProductId == product.ProductId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            // ===== LOAD LẠI DROPDOWN =====
            ViewData["CategoryId"] = new SelectList(
                _context.Categories,
                "CategoryId",
                "CategoryName",
                product.CategoryId
            );

            return View(product);
        }

        // ❌ DELETE (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // ❌ DELETE (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 🔥 check có trong giỏ hàng không
            var isInCart = _context.Carts.Any(c => c.ProductId == id);

            if (isInCart)
            {
                TempData["Error"] = "❌ Món này đang có trong giỏ hàng, không thể xoá!";
                return RedirectToAction(nameof(Index));
            }

            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}