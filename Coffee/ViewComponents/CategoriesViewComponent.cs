using Coffee.Data;
using Microsoft.AspNetCore.Mvc;

namespace Coffee.ViewComponents
{
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly CoffeeShopDbContext db;

        public CategoriesViewComponent(CoffeeShopDbContext context)
            {
             db = context;
        }
        public IViewComponentResult Invoke()
            {
                var categories = db.Categories.Select(c => new DTO.CategoryDTO
                {
                    Id = c.CategoryId,
                    Name = c.CategoryName,
                    Description = c.Description
                }).ToList();
                return View(categories);

        }
    }
}
