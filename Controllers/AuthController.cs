using LearnMVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace LearnMVC.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly MyFinanceDbcontext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(
            MyFinanceDbcontext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }
        public IActionResult Login()
        {
            return View();
        }

        // GET: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        // 1. Tìm tài khoản theo Email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        // 2. Kiểm tra tài khoản
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
            return View();
        }

        // 3. Kiểm tra mật khẩu
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
            return View();
        }

        // 4. Tạo Claims lưu thông tin người dùng
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        var identity = new ClaimsIdentity(claims, "MyCookie");
        var principal = new ClaimsPrincipal(identity);

        // 5. Tạo Cookie đăng nhập
        await HttpContext.SignInAsync(
            "MyCookie",
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true
            }
        );

        // 6. Chuyển về Dashboard
        return RedirectToAction("Index", "Home");
    }
    
    public IActionResult Register()
    {
        return View();
    }
    // POST: /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string fullName,
        string email,
        string password,
        string confirmPassword)
    {
        // 1. Kiểm tra dữ liệu đầu vào
        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
            return View();
        }

        fullName = fullName.Trim();
        email = email.Trim();

        // 2. Kiểm tra độ dài
        if (fullName.Length > 256 || email.Length > 256)
        {
            ViewBag.Error = "Họ tên hoặc email quá dài.";
            return View();
        }

        // 3. Kiểm tra mật khẩu
        if (password.Length < 8)
        {
            ViewBag.Error = "Mật khẩu phải có ít nhất 8 ký tự.";
            return View();
        }

        if (password != confirmPassword)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp.";
            return View();
        }

        // 4. Kiểm tra Email đã tồn tại chưa
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == email);

        if (emailExists)
        {
            ViewBag.Error = "Email này đã được sử dụng.";
            return View();
        }

        // 5. Tạo User mới
        var user = new User
        {
            ID = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            CreateAt = DateTime.Today
        };

        // 6. Hash mật khẩu
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        // 7. Lưu vào Database
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 8. Chuyển sang Login
        TempData["Success"] = "Đăng ký thành công! Hãy đăng nhập.";

        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookie");

        return RedirectToAction("Index", "Home");
    }
}
}