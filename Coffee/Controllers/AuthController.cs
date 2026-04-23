using Coffee.Data;
using Coffee.DTO;
using Coffee.Helper;
using Coffee.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace Coffee.Controllers
{
    public class AuthController : Controller
    {
        private readonly CoffeeShopDbContext db;
        private readonly PasswordHasherHelper hasher = new PasswordHasherHelper();

        public AuthController(CoffeeShopDbContext context)
        {
            db = context;
        }

        // =========================
        // 📝 REGISTER
        // =========================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(RegisterDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                // check email
                if (db.Users.Any(x => x.Email == dto.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại!");
                    return View(dto);
                }

                // check username
                if (db.Users.Any(x => x.UserName == dto.Username))
                {
                    ModelState.AddModelError("Username", "Username đã tồn tại!");
                    return View(dto);
                }

                var user = new User
                {
                    UserName = dto.Username,
                    Email = dto.Email,
                    RoleId = 2,
                    IsActive = true,
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow
                };
                
                // hash password
                user.Password = hasher.Hash(user, dto.Password);

                db.Users.Add(user);
                db.SaveChanges();

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // 🔥 QUAN TRỌNG: show lỗi thật trên Render
                return Content($"ERROR REGISTER: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // =========================
        // 🔑 LOGIN (COOKIE)
        // =========================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = db.Users.AsNoTracking().FirstOrDefault(x => x.UserName == dto.Username);

            if (user == null || user.Password == null || !hasher.Verify(user, user.Password, dto.Password))
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu!");
                return View(dto);
            }

            if (user.IsLocked == true)
            {
                ModelState.AddModelError("", "Tài khoản bị khóa!");
                return View(dto);
            }

            if (user.IsActive == false)
            {
                ModelState.AddModelError("", "Tài khoản chưa kích hoạt!");
                return View(dto);
            }

            // =========================
            // 🔥 CLAIMS (QUAN TRỌNG)
            // =========================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.RoleId == 1 ? "Admin" : "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // =========================
            // 🍪 SIGN IN
            // =========================
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(10) // ⏱ 10 phút
                }
            );

            return RedirectToAction("Index", "Products");
        }

        // =========================
        // 🚪 LOGOUT
        // =========================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}