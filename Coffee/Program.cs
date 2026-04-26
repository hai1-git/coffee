using Coffee.Data;
using Coffee.Models;
using Coffee.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
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
        if (string.IsNullOrWhiteSpace(myConnectionString))
        {
            throw new InvalidOperationException("Connection string 'MyConnectString' was not found.");
        }

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

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var app = builder.Build();

        //using (var scope = app.Services.CreateScope())
        //{
        //    var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();
        //    db.Database.Migrate();
        //}

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

        app.UseForwardedHeaders();
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


        // những đường link nào của trang k tồn tại sẽ bay zo đây
        app.MapFallbackToController("NotFound", "Home");
        // 👉 AUTO MIGRATE
        //using (var scope = app.Services.CreateScope())
        //{
        //    var db = scope.ServiceProvider.GetRequiredService<CoffeeShopDbContext>();
        //    db.Database.Migrate();
        //}

        app.Run();
    }
}
