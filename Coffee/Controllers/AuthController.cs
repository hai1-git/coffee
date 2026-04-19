using Coffee.DTO;
using Coffee.Models;
using Coffee.Helper;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Coffee.Data;


namespace Coffee.Controllers
{
    public class AuthController : Controller
    {
        private readonly CoffeeShopDbContext db;
        private readonly PasswordHasher hasher = new PasswordHasher();

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

            // ❌ Email trùng
            if (db.Users.Any(x => x.Email == dto.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại!");
                return View(dto);
            }

            // ❌ Username trùng
            if (db.Users.Any(x => x.UserName == dto.Username))
            {
                ModelState.AddModelError("Username", "Username đã tồn tại!");
                return View(dto);
            }

            var user = new User
            {
                UserName = dto.Username,
                Email = dto.Email,
                Password = hasher.Hash(dto.Password),
                RoleId = 2, // user
                IsActive = true,
                IsLocked = false,
                CreatedAt = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            return RedirectToAction("Login");
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

            var hash = hasher.Hash(dto.Password);

            var user = db.Users.FirstOrDefault(x =>
                x.UserName == dto.Username &&
                x.Password == hash);

            if (user == null)
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
                principal
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