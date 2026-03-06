using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using SupplierInventorySystem.Services;
using SupplierInventorySystem.ViewModels;

namespace SupplierInventorySystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public AdminController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Username)
                .Select(u => new UserListViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FullName = u.FullName,
                    RoleName = u.Role != null ? u.Role.Name : null,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin,
                    IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTime.Now
                })
                .ToListAsync();

            var roles = await _context.Roles
                .Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    UserCount = r.Users != null ? r.Users.Count : 0
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                ActiveUsers = users.Count(u => u.IsActive),
                InactiveUsers = users.Count(u => !u.IsActive),
                LockedUsers = users.Count(u => u.IsLocked),
                TotalRoles = roles.Count,
                Users = users,
                Roles = roles
            };

            ViewBag.AllRoles = roles;
            return View(vm);
        }

        // POST: Admin/QuickAddUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddUser(QuickAddUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = "שגיאה: " + string.Join(", ", errors);
                return RedirectToAction(nameof(Dashboard));
            }

            if (await _authService.IsUsernameTakenAsync(model.Username))
            {
                TempData["ErrorMessage"] = $"שם המשתמש '{model.Username}' כבר קיים במערכת";
                return RedirectToAction(nameof(Dashboard));
            }

            if (await _authService.IsEmailTakenAsync(model.Email))
            {
                TempData["ErrorMessage"] = $"הדוא\"ל '{model.Email}' כבר רשום במערכת";
                return RedirectToAction(nameof(Dashboard));
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                PasswordHash = _authService.HashPassword(model.Password),
                RoleId = model.RoleId,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"המשתמש '{user.Username}' נוצר בהצלחה!";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/QuickChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickChangeRole(int userId, int? roleId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.RoleId = roleId;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"התפקיד של '{user.Username}' עודכן!";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/QuickToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickToggleActive(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId && user.IsActive)
            {
                TempData["ErrorMessage"] = "לא ניתן לבטל את המשתמש שלך!";
                return RedirectToAction(nameof(Dashboard));
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            var status = user.IsActive ? "הופעל" : "הושבת";
            TempData["SuccessMessage"] = $"המשתמש '{user.Username}' {status}";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/QuickUnlock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUnlock(int userId)
        {
            await _authService.UnlockUserAsync(userId);
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
                TempData["SuccessMessage"] = $"המשתמש '{user.Username}' שוחרר מחסימה!";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/QuickAddRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddRole(string roleName, string? roleDescription)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["ErrorMessage"] = "שם תפקיד הוא שדה חובה";
                return RedirectToAction(nameof(Dashboard));
            }

            if (await _context.Roles.AnyAsync(r => r.Name == roleName))
            {
                TempData["ErrorMessage"] = $"תפקיד בשם '{roleName}' כבר קיים";
                return RedirectToAction(nameof(Dashboard));
            }

            _context.Roles.Add(new Role { Name = roleName.Trim(), Description = roleDescription?.Trim() });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"התפקיד '{roleName}' נוצר בהצלחה!";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
