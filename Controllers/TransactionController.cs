
using LearnMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LearnMVC.Controllers;

public class TransactionController : Controller
{
    private readonly MyFinanceDbcontext _context;

    public TransactionController(MyFinanceDbcontext context)
    {
        _context = context;
    }

    // Lấy ID của người dùng đang đăng nhập từ Claims
    private bool TryGetUserId(out Guid userId)
    {
        var userIdString = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        return Guid.TryParse(userIdString, out userId)
            && User.Identity?.IsAuthenticated == true;
    }

    // Hiển thị trang giao dịch
    public async Task<IActionResult> TransactionPage()
    {
        // Guest: không lấy dữ liệu của bất kỳ tài khoản nào
        if (!TryGetUserId(out Guid userId))
        {
            ViewBag.InitialBalance = null;
            ViewBag.Transactions = new List<Transaction>();

            ViewBag.TotalIncome = 0m;
            ViewBag.TotalExpense = 0m;
            ViewBag.CurrentBalance = 0m;

            return View();
        }

        var setting = await _context.FinanceSettings
            .FirstOrDefaultAsync(x => x.UserId == userId);

        decimal initialBalance = setting?.InitialBalance ?? 0m;

        ViewBag.InitialBalance = setting?.InitialBalance;

        // Chỉ lấy giao dịch của tài khoản hiện tại
        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.DateTime)
            .ToListAsync();

        ViewBag.Transactions = transactions;

        var totalIncome = transactions
            .Where(t => t.Type == "Income")
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Type == "Expense")
            .Sum(t => t.Amount);

        var currentBalance = initialBalance + totalIncome - totalExpense;

        ViewBag.TotalIncome = totalIncome;
        ViewBag.TotalExpense = totalExpense;
        ViewBag.CurrentBalance = currentBalance;

        // Tổng thu trong tháng
        var startOfMonth = new DateTime(
            DateTime.Today.Year,
            DateTime.Today.Month,
            1
        );

        var startOfNextMonth = startOfMonth.AddMonths(1);

        ViewBag.TotalIncome = await _context.Transactions
            .Where(t => t.UserId == userId
                    && t.Type == "Income"
                    && t.DateTime >= startOfMonth
                    && t.DateTime < startOfNextMonth)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return View();
    }

    // Lưu số dư ban đầu
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBalance(int amount)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Challenge("MyCookie");
        }

        if (amount < 0)
        {
            ModelState.AddModelError(
                "amount",
                "Số dư không được nhỏ hơn 0."
            );

            return RedirectToAction(nameof(TransactionPage));
        }

        var setting = await _context.FinanceSettings
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (setting == null)
        {
            setting = new FinanceSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InitialBalance = amount
            };

            _context.FinanceSettings.Add(setting);
        }
        else
        {
            setting.InitialBalance = amount;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(TransactionPage));
    }

    // Thêm giao dịch
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTransaction(
        Transaction transaction,
        string PaymentMethod)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Challenge("MyCookie");
        }

        transaction.Id = Guid.NewGuid();

        // Không lấy UserId từ form gửi lên
        // Luôn gán theo người đang đăng nhập
        transaction.UserId = userId;
        transaction.Method = PaymentMethod;

        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(TransactionPage));
    }

    // Hiển thị giao dịch cần sửa
    [HttpGet]
    public async Task<IActionResult> EditTransaction(Guid id)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Challenge("MyCookie");
        }

        // Chỉ tìm giao dịch thuộc tài khoản hiện tại
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id && t.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    // Lưu thay đổi giao dịch
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTransaction(
        Guid id,
        Transaction updatedTransaction)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Challenge("MyCookie");
        }

        // Kiểm tra cả ID giao dịch và UserID
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id && t.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        transaction.Amount = updatedTransaction.Amount;
        transaction.Type = updatedTransaction.Type;
        transaction.Method = updatedTransaction.Method;
        transaction.Catalogue = updatedTransaction.Catalogue;
        transaction.DateTime = updatedTransaction.DateTime;
        transaction.Note = updatedTransaction.Note;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(TransactionPage));
    }

    // Xóa giao dịch
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Challenge("MyCookie");
        }

        // Chỉ xóa giao dịch của tài khoản hiện tại
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id && t.UserId == userId);

        if (transaction == null)
        {
            return NotFound();
        }

        _context.Transactions.Remove(transaction);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(TransactionPage));
    }
}
