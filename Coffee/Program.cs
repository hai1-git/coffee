using Coffee.Data;
using Coffee.Models;
using Coffee.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // =========================
        // 🔥 ADD SERVICES
        // =========================
        builder.Services.AddControllersWithViews();

        // 👉 DB
        var myConnectionString = builder.Configuration.GetConnectionString("MyConnectString");
        builder.Services.AddDbContext<CoffeeShopDbContext>(options =>
            options.UseNpgsql(myConnectionString));
        //options.UseSqlServer(myConnectionString));


        // 🔥 ADD CLOUDINARY SERVICE Ở ĐÂY
        builder.Services.AddSingleton<CloudinaryService>();

        // =========================
        // 🔐 COOKIE AUTH (QUAN TRỌNG)
        // =========================
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/Login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10); // ⏱ 10 phút
                //options.SlidingExpiration = false; // ❌ không tự gia hạn
                options.SlidingExpiration = true;  //Nếu muốn user không bị out khi đang dùng
            });

        var app = builder.Build();

        // =========================
        // 🔥 SEED DATA (THÊM Ở ĐÂY)
        // =========================
        //using (var scope = app.Services.CreateScope())
        //{
        //    var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();

        //    db.Database.Migrate(); // đảm bảo tạo DB trước

        //    if (!db.Roles.Any())
        //    {
        //        db.Roles.AddRange(
        //            new Role { RoleName = "Admin" },
        //            new Role { RoleName = "User" }
        //        );

        //        db.SaveChanges();
        //    }
        //}

        // =========================
        // ⚙️ PIPELINE
        // =========================
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        // =========================
        // 🔥 THỨ TỰ QUAN TRỌNG
        // =========================
        app.UseAuthentication();   // ❗ PHẢI CÓ
        app.UseAuthorization();

        // =========================
        // ROUTE
        // =========================
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // 👉 AUTO MIGRATE
        //using (var scope = app.Services.CreateScope())
        //{
        //    var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();
        //    db.Database.Migrate();
        //}

        app.Run();
    }
}