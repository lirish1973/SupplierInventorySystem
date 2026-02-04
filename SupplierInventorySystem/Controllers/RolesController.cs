using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using SupplierInventorySystem.ViewModels;

namespace SupplierInventorySystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RolesController> _logger;

        public RolesController(ApplicationDbContext context, ILogger<RolesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Roles
        public async Task<IActionResult> Index()
        {
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

            return View(roles);
        }

        // GET: Roles/Create
        public IActionResult Create()
        {
            return View(new RoleViewModel());
        }

        // POST: Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if role name exists
                if (await _context.Roles.AnyAsync(r => r.Name == model.Name))
                {
                    ModelState.AddModelError("Name", "תפקיד עם שם זה כבר קיים במערכת");
                    return View(model);
                }

                var role = new Role
                {
                    Name = model.Name,
                    Description = model.Description
                };

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin created new role: {role.Name}");
                TempData["SuccessMessage"] = $"התפקיד '{role.Name}' נוצר בהצלחה!";

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Roles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var viewModel = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            };

            return View(viewModel);
        }

        // POST: Roles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoleViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound();
                }

                // Check if role name exists (for other roles)
                if (await _context.Roles.AnyAsync(r => r.Name == model.Name && r.Id != id))
                {
                    ModelState.AddModelError("Name", "תפקיד עם שם זה כבר קיים במערכת");
                    return View(model);
                }

                role.Name = model.Name;
                role.Description = model.Description;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin updated role: {role.Name}");
                TempData["SuccessMessage"] = $"התפקיד '{role.Name}' עודכן בהצלחה!";

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Roles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            var viewModel = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                UserCount = role.Users?.Count ?? 0
            };

            return View(viewModel);
        }

        // POST: Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            // Prevent deleting if users are assigned
            if (role.Users != null && role.Users.Any())
            {
                TempData["ErrorMessage"] = $"לא ניתן למחוק את התפקיד '{role.Name}' כי יש {role.Users.Count} משתמשים משויכים אליו!";
                return RedirectToAction(nameof(Index));
            }

            // Prevent deleting Admin role
            if (role.Name == "Admin")
            {
                TempData["ErrorMessage"] = "לא ניתן למחוק את תפקיד המנהל הראשי!";
                return RedirectToAction(nameof(Index));
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Admin deleted role: {role.Name}");
            TempData["SuccessMessage"] = $"התפקיד '{role.Name}' נמחק בהצלחה!";

            return RedirectToAction(nameof(Index));
        }
    }
}
