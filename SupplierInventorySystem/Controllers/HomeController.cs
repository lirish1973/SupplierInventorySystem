using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using SupplierInventorySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SupplierInventorySystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardData = new DashboardViewModel
            {
                // סטטיסטיקות ספקים
                TotalSuppliers = await _context.Suppliers.CountAsync(),
                ActiveSuppliers = await _context.Suppliers.CountAsync(s => s.Status == "active"),
                InactiveSuppliers = await _context.Suppliers.CountAsync(s => s.Status == "inactive"),
                BlockedSuppliers = await _context.Suppliers.CountAsync(s => s.Status == "blocked"),

                // סטטיסטיקות מוצרים
                TotalProducts = await _context.Products.CountAsync(),
                ActiveProducts = await _context.Products.CountAsync(p => p.Active),
                InactiveProducts = await _context.Products.CountAsync(p => !p.Active),
                ServiceProducts = await _context.Products.CountAsync(p => p.IsService),

                // סטטיסטיקות כלליות
                TotalCategories = await _context.ProductCategories.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),

                // מוצרים מתחת לנקודת הזמנה
                LowStockProducts = await _context.Products
                    .Where(p => p.Active && !p.IsService && p.ReorderPoint > 0)
                    .OrderBy(p => p.Name)
                    .Take(10)
                    .Select(p => new LowStockProductDto
                    {
                        Id = p.Id,
                        Sku = p.Sku,
                        Name = p.Name,
                        ReorderPoint = p.ReorderPoint,
                        ReorderQty = p.ReorderQty,
                        Unit = p.DefaultUnit != null ? p.DefaultUnit.Code : ""
                    })
                    .ToListAsync(),

                // ספקים מובילים לפי דירוג
                TopSuppliers = await _context.Suppliers
                    .Where(s => s.Status == "active")
                    .OrderByDescending(s => s.Rating)
                    .Take(5)
                    .Select(s => new TopSupplierDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Code = s.Code ?? "",
                        Rating = s.Rating ?? 0,
                        ProductCount = s.SupplierProducts != null ? s.SupplierProducts.Count : 0
                    })
                    .ToListAsync(),

                // מוצרים אחרונים שנוספו
                RecentProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.DefaultUnit)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                // ספקים אחרונים שנוספו
                RecentSuppliers = await _context.Suppliers
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                // פילוח מוצרים לפי קטגוריה
                ProductsByCategory = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Active && p.CategoryId != null)
                    .GroupBy(p => p.Category!.Name)
                    .Select(g => new CategoryStatDto
                    {
                        CategoryName = g.Key,
                        ProductCount = g.Count()
                    })
                    .OrderByDescending(x => x.ProductCount)
                    .Take(7)
                    .ToListAsync(),

                // רשימת התראות
                Alerts = new List<AlertDto>()
            };

            // יצירת התראות דינמיות
            var lowStockCount = dashboardData.LowStockProducts.Count;
            if (lowStockCount > 0)
            {
                dashboardData.Alerts.Add(new AlertDto
                {
                    Type = "warning",
                    Icon = "fa-exclamation-triangle",
                    Title = "מלאי נמוך",
                    Message = $"יש {lowStockCount} מוצרים מתחת לנקודת ההזמנה",
                    Link = "/Products?activeOnly=true"
                });
            }

            if (dashboardData.InactiveProducts > 0)
            {
                dashboardData.Alerts.Add(new AlertDto
                {
                    Type = "info",
                    Icon = "fa-info-circle",
                    Title = "מוצרים לא פעילים",
                    Message = $"יש {dashboardData.InactiveProducts} מוצרים לא פעילים במערכת",
                    Link = "/Products?activeOnly=false"
                });
            }

            if (dashboardData.BlockedSuppliers > 0)
            {
                dashboardData.Alerts.Add(new AlertDto
                {
                    Type = "danger",
                    Icon = "fa-ban",
                    Title = "ספקים חסומים",
                    Message = $"יש {dashboardData.BlockedSuppliers} ספקים חסומים",
                    Link = "/Suppliers"
                });
            }

            // הודעת ברכה לפי שעה
            ViewBag.WelcomeMessage = GetWelcomeMessage();
            ViewBag.CurrentUser = "lirish1973";

            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetWelcomeMessage()
        {
            var hour = DateTime.Now.Hour;
            if (hour < 12)
                return "בוקר טוב";
            else if (hour < 17)
                return "צהריים טובים";
            else if (hour < 21)
                return "ערב טוב";
            else
                return "לילה טוב";
        }
    }
}