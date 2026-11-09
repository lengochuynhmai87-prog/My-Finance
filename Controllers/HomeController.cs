using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LearnMVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LearnMVC.Controllers;

public class HomeController : Controller
{
    private readonly MyFinanceDbcontext _context;

    public HomeController(MyFinanceDbcontext context)
    {
        _context = context;
    }
    public async Task<IActionResult> Index(string period = "month",
    DateTime? startDate = null,
    DateTime? endDate = null)
    {
       // 1. Kiểm tra trạng thái đăng nhập và lấy UserID từ Claims
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 2. Nếu chưa đăng nhập hoặc không có UserID hợp lệ
        if (User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(userIdString, out Guid userId))
        {
            ViewBag.CurrentBalance = 0m;
            ViewBag.TotalIncome = 0m;
            ViewBag.TotalExpense = 0m;

            ViewBag.RecentTransactions = new List<Transaction>();
            ViewBag.CategoryLabels = new List<string>();
            ViewBag.CategoryValues = new List<decimal>();

            ViewBag.MonthLabels = new List<string>();
            ViewBag.MonthIncome = new List<decimal>();
            ViewBag.MonthExpense = new List<decimal>();

            ViewBag.Period = period;
            ViewBag.FilterLabel = "";
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View();
        }

        // 3. Lấy số dư ban đầu của đúng người đang đăng nhập
        var setting = await _context.FinanceSettings
            .FirstOrDefaultAsync(x => x.UserId == userId);

        decimal initialBalance = setting?.InitialBalance ?? 0m;

        // 4. Lấy giao dịch của đúng người đang đăng nhập
        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();
        var totalIncomeAll = transactions
            .Where(t => t.Type == "Income")
            .Sum(t => t.Amount);

        var totalExpenseAll = transactions
            .Where(t => t.Type == "Expense")
            .Sum(t => t.Amount);

        ViewBag.CurrentBalance =
            initialBalance + totalIncomeAll - totalExpenseAll;

        // 5. Lọc giao dịch trong tháng hiện tại
        var startOfMonth = new DateTime(
            DateTime.Today.Year,
            DateTime.Today.Month,
            1
        );

        var startOfNextMonth = startOfMonth.AddMonths(1);

        var monthlyTransactions = transactions
            .Where(t => t.DateTime >= startOfMonth
                     && t.DateTime < startOfNextMonth)
            .ToList();

        ViewBag.TotalIncome = monthlyTransactions
            .Where(t => t.Type == "Income")
            .Sum(t => t.Amount);
        ViewBag.TotalExpense = monthlyTransactions
            .Where(t => t.Type == "Expense")
            .Sum(t => t.Amount);

        //----Biểu đồ--------------------------------------------
       
        // ---- Xác định khoảng thời gian cho biểu đồ
        var today = DateTime.Today;

        var thisWeekStart = today.AddDays(
            -(((int)today.DayOfWeek + 6) % 7)
        );

        DateTime filterStart;
        DateTime filterEnd;

        switch (period)
        {
            case "week":
                filterStart = thisWeekStart;
                filterEnd = today.AddDays(1);
                break;

            case "lastWeek":
                filterStart = thisWeekStart.AddDays(-7);
                filterEnd = thisWeekStart;
                break;

            case "lastMonth":
                filterStart = startOfMonth.AddMonths(-1);
                filterEnd = startOfMonth;
                break;

            case "3months":
                filterStart = startOfMonth.AddMonths(-2);
                filterEnd = today.AddDays(1);
                break;

            case "6months":
                filterStart = startOfMonth.AddMonths(-5);
                filterEnd = today.AddDays(1);
                break;

            case "custom":
                if (startDate.HasValue &&
                    endDate.HasValue &&
                    startDate.Value.Date <= endDate.Value.Date)
                {
                    filterStart = startDate.Value.Date;
                    filterEnd = endDate.Value.Date.AddDays(1);
                }
                else
                {
                    period = "month";
                    filterStart = startOfMonth;
                    filterEnd = today.AddDays(1);
                }
                break;

            default:
                period = "month";
                filterStart = startOfMonth;
                filterEnd = today.AddDays(1);
                break;
        }
        ViewBag.FilterLabel = $"{filterStart:dd/MM/yyyy} - {filterEnd.AddDays(-1):dd/MM/yyyy}"; 

        // 7. Lọc giao dịch theo khoảng thời gian
        var filteredTransactions = transactions
            .Where(t => t.DateTime >= filterStart
                    && t.DateTime < filterEnd)
            .ToList();

        // 8. Thống kê chi tiêu theo danh mục
        var categoryExpenses = filteredTransactions
            .Where(t => t.Type == "Expense")
            .GroupBy(t => t.Catalogue)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Sum(t => t.Amount)
            })
            .OrderByDescending(g => g.Total)
            .ToList();

        ViewBag.CategoryLabels = categoryExpenses
            .Select(x => x.Category ?? "Khác")
            .ToList();

        ViewBag.CategoryValues = categoryExpenses
            .Select(x => x.Total)
            .ToList();

        // 9. Lưu trạng thái bộ lọc
        ViewBag.Period = period;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        // 10. Thống kê thu chi theo tháng
        var monthlyStats = filteredTransactions
            .GroupBy(t => new
            {
                t.DateTime.Year,
                t.DateTime.Month
            })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Income = g.Where(t => t.Type == "Income")
                        .Sum(t => t.Amount),
                Expense = g.Where(t => t.Type == "Expense")
                        .Sum(t => t.Amount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        ViewBag.MonthLabels = monthlyStats
            .Select(x => $"{x.Month:00}/{x.Year}")
            .ToList();

        ViewBag.MonthIncome = monthlyStats
            .Select(x => x.Income)
            .ToList();

        ViewBag.MonthExpense = monthlyStats
            .Select(x => x.Expense)
            .ToList();

        // ------ Lấy 5 giao dịch gần đây nhất
        ViewBag.RecentTransactions = transactions
            .OrderByDescending(t => t.DateTime)
            .Take(5)
            .ToList();


        return View();
    }

    
}
