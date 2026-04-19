using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Coffee.Data;
using Coffee.Models;
using Coffee.Services;

namespace Coffee.Controllers
{
    public class AdminProductsController : Controller
    {
        private readonly CoffeeShopDbContext _context;
        private readonly CloudinaryService _cloudinary;

        public AdminProductsController(
            CoffeeShopDbContext context,
            CloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        // =========================
        // 📦 LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            var products = _context.Products.Include(p => p.Category);
            return View(await products.ToListAsync());
        }

        // =========================
        // 🔍 DETAILS
        // =========================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // =========================
        // ➕ CREATE (GET)
        // =========================
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(
                _context.Categories, "CategoryId", "CategoryName");
            return View();
        }

        // =========================
        // ➕ CREATE (POST - CLOUDINARY)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? file)
        {
            if (file != null && file.Length > 0)
            {
                var url = await _cloudinary.UploadImageAsync(file);
                product.ImageUrl = url;
            }

            if (string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = "/img/default.png";
            }

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(
                _context.Categories,
                "CategoryId",
                "CategoryName",
                product.CategoryId
            );

            return View(product);
        }

        // =========================
        // ✏️ EDIT (GET)
        // =========================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(
                _context.Categories,
                "CategoryId",
                "CategoryName",
                product.CategoryId);

            return View(product);
        }

        // =========================
        // ✏️ EDIT (POST - CLOUDINARY)
        // =========================
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

            // ===== UPLOAD NEW IMAGE =====
            if (file != null && file.Length > 0)
            {
                var url = await _cloudinary.UploadImageAsync(file);
                product.ImageUrl = url;
            }
            else
            {
                product.ImageUrl = oldProduct.ImageUrl;
            }

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

            ViewData["CategoryId"] = new SelectList(
                _context.Categories,
                "CategoryId",
                "CategoryName",
                product.CategoryId
            );

            return View(product);
        }

        // =========================
        // ❌ DELETE (GET)
        // =========================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // =========================
        // ❌ DELETE (POST)
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var isInCart = _context.Carts.Any(c => c.ProductId == id);

            if (isInCart)
            {
                TempData["Error"] = "❌ Sản phẩm đang có trong giỏ hàng!";
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