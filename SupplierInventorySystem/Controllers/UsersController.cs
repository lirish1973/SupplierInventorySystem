using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using SupplierInventorySystem.Services;
using SupplierInventorySystem.ViewModels;

namespace SupplierInventorySystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ApplicationDbContext context,
            IAuthService authService,
            ILogger<UsersController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        // GET: Users
        public async Task<IActionResult> Index(string searchString, int? roleId, bool? activeOnly)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentRole"] = roleId;
            ViewData["ActiveOnly"] = activeOnly ?? false;

            // Load roles for filter dropdown
            ViewBag.Roles = new SelectList(
                await _context.Roles.OrderBy(r => r.Name).ToListAsync(),
                "Id",
                "Name"
            );

            var users = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    u.Username.Contains(searchString) ||
                    u.Email.Contains(searchString) ||
                    (u.FullName != null && u.FullName.Contains(searchString))
                );
            }

            // Filter by role
            if (roleId.HasValue)
            {
                users = users.Where(u => u.RoleId == roleId);
            }

            // Filter by active status
            if (activeOnly == true)
            {
                users = users.Where(u => u.IsActive);
            }

            var userList = await users
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

            return View(userList);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new UserDetailsViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                FailedLoginAttempts = user.FailedLoginAttempts,
                LockoutEnd = user.LockoutEnd
            };

            return View(viewModel);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            await LoadRolesDropdown();
            return View(new CreateUserViewModel());
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if username exists
                if (await _authService.IsUsernameTakenAsync(model.Username))
                {
                    ModelState.AddModelError("Username", "שם המשתמש כבר קיים במערכת");
                    await LoadRolesDropdown(model.RoleId);
                    return View(model);
                }

                // Check if email exists
                if (await _authService.IsEmailTakenAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "כתובת הדוא״ל כבר רשומה במערכת");
                    await LoadRolesDropdown(model.RoleId);
                    return View(model);
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

                _logger.LogInformation($"Admin created new user: {user.Username}");
                TempData["SuccessMessage"] = $"המשתמש '{user.Username}' נוצר בהצלחה!";

                return RedirectToAction(nameof(Index));
            }

            await LoadRolesDropdown(model.RoleId);
            return View(model);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };

            await LoadRolesDropdown(user.RoleId);
            return View(viewModel);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Check if username exists (for other users)
                if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.Id != id))
                {
                    ModelState.AddModelError("Username", "שם המשתמש כבר קיים במערכת");
                    await LoadRolesDropdown(model.RoleId);
                    return View(model);
                }

                // Check if email exists (for other users)
                if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != id))
                {
                    ModelState.AddModelError("Email", "כתובת הדוא״ל כבר רשומה במערכת");
                    await LoadRolesDropdown(model.RoleId);
                    return View(model);
                }

                user.Username = model.Username;
                user.Email = model.Email;
                user.FullName = model.FullName;
                user.RoleId = model.RoleId;
                user.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin updated user: {user.Username}");
                TempData["SuccessMessage"] = $"המשתמש '{user.Username}' עודכן בהצלחה!";

                return RedirectToAction(nameof(Index));
            }

            await LoadRolesDropdown(model.RoleId);
            return View(model);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting yourself
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "לא ניתן למחוק את המשתמש שלך!";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Admin deleted user: {user.Username}");
            TempData["SuccessMessage"] = $"המשתמש '{user.Username}' נמחק בהצלחה!";

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/ResetPassword/5
        public async Task<IActionResult> ResetPassword(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new AdminResetPasswordViewModel
            {
                UserId = user.Id,
                Username = user.Username
            };

            return View(viewModel);
        }

        // POST: Users/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(AdminResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                {
                    return NotFound();
                }

                user.PasswordHash = _authService.HashPassword(model.NewPassword);
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin reset password for user: {user.Username}");
                TempData["SuccessMessage"] = $"הסיסמה של '{user.Username}' אופסה בהצלחה!";

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: Users/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deactivating yourself
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId && user.IsActive)
            {
                TempData["ErrorMessage"] = "לא ניתן לבטל את המשתמש שלך!";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            var status = user.IsActive ? "הופעל" : "הושבת";
            _logger.LogInformation($"Admin toggled user {user.Username} to {status}");
            TempData["SuccessMessage"] = $"המשתמש '{user.Username}' {status} בהצלחה!";

            return RedirectToAction(nameof(Index));
        }

        // POST: Users/Unlock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            await _authService.UnlockUserAsync(id);

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                TempData["SuccessMessage"] = $"המשתמש '{user.Username}' שוחרר מחסימה!";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRolesDropdown(int? selectedRole = null)
        {
            ViewData["RoleId"] = new SelectList(
                await _context.Roles.OrderBy(r => r.Name).ToListAsync(),
                "Id",
                "Name",
                selectedRole
            );
        }
    }
}
