using Coffee.Data;
using Coffee.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Controllers
{
    public class ProductsController : Controller
    {
        private readonly CoffeeShopDbContext db;

        public ProductsController(CoffeeShopDbContext context)
        {
            db = context;
        }

        // =========================
        // 📦 PRODUCT LIST + FILTER + PAGINATION
        // =========================
        public async Task<IActionResult> Index(int page = 1, int? loai = null)
        {
            int pageSize = 8;

            var query = db.Products.AsQueryable();

            // 🔥 FILTER CATEGORY
            if (loai.HasValue)
            {
                query = query.Where(p => p.CategoryId == loai.Value);
            }

            var totalItems = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl ?? string.Empty
                })
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Loai = loai;

            return View(products);
        }

        // =========================
        // 🔍 SEARCH + PAGINATION
        // =========================
        public async Task<IActionResult> Search(string? query, int page = 1)
        {
            int pageSize = 8;

            var productsQuery = db.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();

                productsQuery = productsQuery.Where(p =>
                    p.ProductName.ToLower().Contains(query));
            }

            var totalItems = await productsQuery.CountAsync();

            var result = await productsQuery
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl ?? string.Empty
                })
                .ToListAsync();

            ViewBag.Query = query;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(result);
        }

        // =========================
        // 🔍 DETAIL PRODUCT
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var product = await db.Products
                .Where(p => p.ProductId == id)
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl ?? string.Empty
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound();

            return View(product);
        }
    }
}