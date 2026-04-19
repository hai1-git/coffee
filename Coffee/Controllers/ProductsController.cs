using Coffee.Data;
using Coffee.DTO;
using Microsoft.AspNetCore.Mvc;

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
        // 📦 PRODUCT LIST + PAGINATION + FILTER
        // =========================
        public IActionResult Index(int page = 1, int? loai = null)
        {
            int pageSize = 8;

            var query = db.Products.AsQueryable();

            // 🔥 filter theo loại
            if (loai.HasValue)
            {
                query = query.Where(p => p.CategoryId == loai.Value);
            }

            int totalItems = query.Count();

            var products = query
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price ?? 0,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl ?? ""
                })
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Loai = loai;

            return View(products);
        }


        // =========================
        // 🔍 SEARCH + PAGINATION
        // =========================
        public IActionResult Search(string? query, int page = 1)
        {
            int pageSize = 8;

            var productsQuery = db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery
                    .Where(p => p.ProductName.Contains(query));
            }

            int totalItems = productsQuery.Count();

            var result = productsQuery
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price ?? 0,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl ?? ""
                })
                .ToList();

            ViewBag.Query = query;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(result);
        }


        // =========================
        // 🔍 DETAIL
        // =========================
        public IActionResult Details(int id)
        {
            var product = db.Products
                .Where(p => p.ProductId == id)
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price ?? 0,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl ?? ""
                })
                .FirstOrDefault();

            if (product == null)
                return NotFound();

            return View(product);
        }
    }
}