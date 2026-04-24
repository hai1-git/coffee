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

            var user = db.Users.AsNoTracking().FirstOrDefault(x => x.UserName == dto.Username || x.Email == dto.Username);

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

        // đổi mật khẩu (chỉ user đã login mới vào được)
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                // 🔥 lấy user hiện tại từ cookie
                var userId = User.FindFirst("UserId")?.Value;

                if (userId == null)
                    return RedirectToAction("Login");

                var user = db.Users.FirstOrDefault(x => x.UserId.ToString() == userId);

                if (user == null)
                    return RedirectToAction("Login");

                // 🔥 kiểm tra mật khẩu cũ
                if (!hasher.Verify(user, user.Password, dto.OldPassword))
                {
                    ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng!");
                    return View(dto);
                }

                // 🔥 2. Check mật khẩu mới KHÔNG trùng
                if (hasher.Verify(user, user.Password, dto.NewPassword))
                {
                    ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng mật khẩu cũ!");
                    return View(dto);
                }

                // 🔥 hash mật khẩu mới
                user.Password = hasher.Hash(user, dto.NewPassword);

                db.SaveChanges();

                ViewBag.Success = "Đổi mật khẩu thành công!";
                return View();
            }
            catch (Exception ex)
            {
                return Content($"ERROR CHANGE PASSWORD: {ex.InnerException?.Message ?? ex.Message}");
            }
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