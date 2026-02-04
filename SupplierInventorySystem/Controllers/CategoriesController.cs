using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using SupplierInventorySystem.ViewModels;

namespace SupplierInventorySystem.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.ProductCategories
                .Include(c => c.ParentCategory)
                .Include(c => c.Products)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.ParentId == null ? 0 : 1)
                .ThenBy(c => c.ParentCategory != null ? c.ParentCategory.Name : "")
                .ThenBy(c => c.Name)
                .ToListAsync();

            var viewModels = categories.Select(c => new CategoryListViewModel
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId,
                ParentName = c.ParentCategory?.Name,
                ProductCount = c.Products?.Count ?? 0,
                SubCategoryCount = c.SubCategories?.Count ?? 0,
                Level = c.ParentId == null ? 0 : 1
            }).ToList();

            return View(viewModels);
        }

        // GET: Categories/Tree
        public async Task<IActionResult> Tree()
        {
            var categories = await _context.ProductCategories
                .Include(c => c.Products)
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var tree = new List<CategoryTreeViewModel>();
            foreach (var category in categories)
            {
                tree.Add(await BuildTreeNode(category));
            }

            return View(tree);
        }

        private async Task<CategoryTreeViewModel> BuildTreeNode(ProductCategory category)
        {
            var node = new CategoryTreeViewModel
            {
                Id = category.Id,
                Name = category.Name,
                ProductCount = category.Products?.Count ?? 0
            };

            var children = await _context.ProductCategories
                .Include(c => c.Products)
                .Where(c => c.ParentId == category.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            foreach (var child in children)
            {
                node.Children.Add(await BuildTreeNode(child));
            }

            return node;
        }

        // GET: Categories/Create
        public async Task<IActionResult> Create()
        {
            await LoadParentCategories();
            return View(new CategoryFormViewModel());
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if name exists at same level
                if (await _context.ProductCategories.AnyAsync(c =>
                    c.Name == model.Name && c.ParentId == model.ParentId))
                {
                    ModelState.AddModelError("Name", "קטגוריה עם שם זה כבר קיימת");
                    await LoadParentCategories(model.ParentId);
                    return View(model);
                }

                // Prevent circular reference
                if (model.ParentId.HasValue)
                {
                    var parent = await _context.ProductCategories.FindAsync(model.ParentId.Value);
                    if (parent == null)
                    {
                        ModelState.AddModelError("ParentId", "קטגוריית האב לא נמצאה");
                        await LoadParentCategories(model.ParentId);
                        return View(model);
                    }
                }

                var category = new ProductCategory
                {
                    Name = model.Name,
                    ParentId = model.ParentId
                };

                _context.ProductCategories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Category created: {category.Name}");
                TempData["SuccessMessage"] = $"הקטגוריה '{category.Name}' נוצרה בהצלחה!";

                return RedirectToAction(nameof(Index));
            }

            await LoadParentCategories(model.ParentId);
            return View(model);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();

            var viewModel = new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                ParentId = category.ParentId
            };

            await LoadParentCategories(category.ParentId, category.Id);
            return View(viewModel);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var category = await _context.ProductCategories.FindAsync(id);
                if (category == null) return NotFound();

                // Check for duplicate name at same level
                if (await _context.ProductCategories.AnyAsync(c =>
                    c.Name == model.Name && c.ParentId == model.ParentId && c.Id != id))
                {
                    ModelState.AddModelError("Name", "קטגוריה עם שם זה כבר קיימת");
                    await LoadParentCategories(model.ParentId, id);
                    return View(model);
                }

                // Prevent setting itself as parent
                if (model.ParentId == id)
                {
                    ModelState.AddModelError("ParentId", "קטגוריה לא יכולה להיות אב של עצמה");
                    await LoadParentCategories(model.ParentId, id);
                    return View(model);
                }

                // Prevent circular reference (child becoming parent)
                if (model.ParentId.HasValue)
                {
                    if (await IsDescendant(model.ParentId.Value, id))
                    {
                        ModelState.AddModelError("ParentId", "לא ניתן להגדיר תת-קטגוריה כאב");
                        await LoadParentCategories(model.ParentId, id);
                        return View(model);
                    }
                }

                category.Name = model.Name;
                category.ParentId = model.ParentId;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Category updated: {category.Name}");
                TempData["SuccessMessage"] = $"הקטגוריה '{category.Name}' עודכנה בהצלחה!";

                return RedirectToAction(nameof(Index));
            }

            await LoadParentCategories(model.ParentId, id);
            return View(model);
        }

        // GET: Categories/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.ProductCategories
                .Include(c => c.ParentCategory)
                .Include(c => c.Products)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            var viewModel = new CategoryListViewModel
            {
                Id = category.Id,
                Name = category.Name,
                ParentName = category.ParentCategory?.Name,
                ProductCount = category.Products?.Count ?? 0,
                SubCategoryCount = category.SubCategories?.Count ?? 0
            };

            return View(viewModel);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.ProductCategories
                .Include(c => c.Products)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            // Check if has products
            if (category.Products != null && category.Products.Any())
            {
                TempData["ErrorMessage"] = $"לא ניתן למחוק את הקטגוריה '{category.Name}' כי יש {category.Products.Count} מוצרים משויכים";
                return RedirectToAction(nameof(Index));
            }

            // Check if has subcategories
            if (category.SubCategories != null && category.SubCategories.Any())
            {
                TempData["ErrorMessage"] = $"לא ניתן למחוק את הקטגוריה '{category.Name}' כי יש {category.SubCategories.Count} תתי-קטגוריות";
                return RedirectToAction(nameof(Index));
            }

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Category deleted: {category.Name}");
            TempData["SuccessMessage"] = $"הקטגוריה '{category.Name}' נמחקה בהצלחה!";

            return RedirectToAction(nameof(Index));
        }

        // Helper: Check if potentialDescendantId is a descendant of categoryId
        private async Task<bool> IsDescendant(int potentialDescendantId, int categoryId)
        {
            var current = await _context.ProductCategories.FindAsync(potentialDescendantId);
            while (current != null)
            {
                if (current.ParentId == categoryId)
                    return true;

                if (current.ParentId == null)
                    break;

                current = await _context.ProductCategories.FindAsync(current.ParentId);
            }
            return false;
        }

        private async Task LoadParentCategories(int? selectedId = null, int? excludeId = null)
        {
            var categories = await _context.ProductCategories
                .Where(c => excludeId == null || c.Id != excludeId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewData["ParentId"] = new SelectList(categories, "Id", "Name", selectedId);
        }
    }
}
