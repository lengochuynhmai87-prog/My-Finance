using LearnMVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
var connectString = builder.Configuration.GetConnectionString("cnn");
// Add services to the container.
builder.Services.AddDbContext<MyFinanceDbcontext>(options=>
 options.UseSqlServer(connectString));
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IPasswordHasher<User>,PasswordHasher<User>>();
// Cookie Authentication
builder.Services.AddAuthentication("MyCookie")
    .AddCookie("MyCookie", options =>
    {
        // Nếu chưa đăng nhập mà truy cập trang yêu cầu đăng nhập
        options.LoginPath = "/Auth/Login";
        // Cookie hết hạn sau 30 ngày không hoạt động
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        // Có hoạt động thì gia hạn lại thời gian
        options.SlidingExpiration = true;
    });
var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
