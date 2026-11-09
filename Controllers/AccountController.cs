using Microsoft.AspNetCore.Mvc;
using LearnMVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LearnMVC.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookie")]
    public class AccountController : Controller
    {
        private readonly MyFinanceDbcontext _context;

        public AccountController(MyFinanceDbcontext context)
        {
            _context = context;
        }

        public async Task<IActionResult> AccountPage()
        {
            // Lấy ID của user đang đăng nhập từ Cookie/Claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized();
            }

            // Chỉ lấy đúng user đang đăng nhập
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ID == userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string fullName, string email)
        {
            // Lấy ID user đang đăng nhập
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized();
            }

            // Chỉ lấy đúng user đang đăng nhập
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ID == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Kiểm tra email có bị trùng với tài khoản khác không
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email && u.ID != user.ID);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email này đã được sử dụng bởi tài khoản khác."
                );

                user.FullName = fullName;
                user.Email = email;

                return View("AccountPage", user);
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email không được để trống."
                );

                user.FullName = fullName;

                return View("AccountPage", user);
            }

            user.FullName = fullName;
            user.Email = email;

            await _context.SaveChangesAsync();

            return RedirectToAction("AccountPage");
        }
    }
}   