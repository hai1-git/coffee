using Coffee.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

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

        // =========================
        // 🔐 COOKIE AUTH (QUAN TRỌNG)
        // =========================
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/Login";
            });

        var app = builder.Build();

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

        app.Run();
    }
}