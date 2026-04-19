using Coffee.Data;
using Coffee.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coffee.ViewComponents
{
    public class ProductsViewComponent : ViewComponent
    {
        private readonly CoffeeShopDbContext db;

        public ProductsViewComponent(CoffeeShopDbContext context)
        {
            db = context;
        }
        public IViewComponentResult Invoke(int count =4)
        {
            var products = db.Products
                .OrderByDescending(p => p.Price) // Sắp xếp giá cao → thấp
                .Take(count) // Lấy 4 sản phẩm
                .Select(p => new ProductDTO
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName,
                    Price = (decimal)(p.Price ?? 0),
                    Description = p.Description,
                    ImageUrl = p.ImageUrl ?? string.Empty
                }).ToList();
                

            return View(products);
        }
    }
}
