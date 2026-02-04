using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SupplierInventorySystem.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(string searchString, int? categoryId, string sortOrder, bool? activeOnly)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["SkuSortParm"] = sortOrder == "Sku" ? "sku_desc" : "Sku";
            ViewData["CategorySortParm"] = sortOrder == "Category" ? "category_desc" : "Category";
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = categoryId;

            // אם הפרמטר לא נשלח (null) נניח ברירת מחדל שמציגה את כל המוצרים (כולל לא פעילים).
            // כלומר רק כאשר activeOnly==true - נציג רק מוצרים פעילים.
            bool showActiveOnly = activeOnly == true;
            ViewData["ActiveOnly"] = showActiveOnly;

            // טעינת קטגוריות לסינון
            ViewBag.Categories = new SelectList(
                await _context.ProductCategories.OrderBy(c => c.Name).ToListAsync(),
                "Id",
                "Name"
            );

            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.DefaultUnit)
                .AsQueryable();

            // סינון לפי סטטוס — רק כאשר המשתמש ביקש במפורש 'רק פעילים'
            if (showActiveOnly)
            {
                products = products.Where(p => p.Active);
            }

            // חיפוש
            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchString) ||
                    p.Sku.Contains(searchString) ||
                    (p.Description != null && p.Description.Contains(searchString))
                );
            }

            // סינון לפי קטגוריה
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            // מיון
            switch (sortOrder)
            {
                case "name_desc":
                    products = products.OrderByDescending(p => p.Name);
                    break;
                case "Sku":
                    products = products.OrderBy(p => p.Sku);
                    break;
                case "sku_desc":
                    products = products.OrderByDescending(p => p.Sku);
                    break;
                case "Category":
                    products = products.OrderBy(p => p.Category!.Name);
                    break;
                case "category_desc":
                    products = products.OrderByDescending(p => p.Category!.Name);
                    break;
                default:
                    products = products.OrderBy(p => p.Name);
                    break;
            }

            return View(await products.AsNoTracking().ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.DefaultUnit)
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            await _context.Entry(product)
                .Collection(p => p.SupplierProducts)
                .Query()
                .Include(sp => sp.Supplier)
                .LoadAsync();

            await _context.Entry(product)
                .Collection(p => p.PriceHistories)
                .Query()
                .Include(ph => ph.Supplier)
                .OrderByDescending(ph => ph.EffectiveFrom ?? DateTime.MinValue)
                .Take(10)
                .LoadAsync();

            return View(product);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropDownLists();
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Sku,Name,Description,CategoryId,DefaultUnitId,IsService,TrackSerials,TrackLots,ReorderPoint,ReorderQty,Active")] Product product)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Products.AnyAsync(p => p.Sku == product.Sku))
                {
                    ModelState.AddModelError("Sku", "מק\"ט זה כבר קיים במערכת");
                    await LoadDropDownLists(product.CategoryId, product.DefaultUnitId);
                    return View(product);
                }

                product.CreatedAt = DateTime.Now;
                product.UpdatedAt = DateTime.Now;
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"המוצר '{product.Name}' נוסף בהצלחה!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropDownLists(product.CategoryId, product.DefaultUnitId);
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            await LoadDropDownLists(product.CategoryId, product.DefaultUnitId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Sku,Name,Description,CategoryId,DefaultUnitId,IsService,TrackSerials,TrackLots,ReorderPoint,ReorderQty,Active,CreatedAt")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Products.AnyAsync(p => p.Sku == product.Sku && p.Id != product.Id))
                    {
                        ModelState.AddModelError("Sku", "מק\"ט זה כבר קיים במערכת");
                        await LoadDropDownLists(product.CategoryId, product.DefaultUnitId);
                        return View(product);
                    }

                    product.UpdatedAt = DateTime.Now;
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"המוצר '{product.Name}' עודכן בהצלחה!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await LoadDropDownLists(product.CategoryId, product.DefaultUnitId);
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.DefaultUnit)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"המוצר '{product.Name}' נמחק בהצלחה!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Deactivate/5
        public async Task<IActionResult> Deactivate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Active = false;
            product.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"המוצר '{product.Name}' הועבר לסטטוס לא פעיל!";

            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Activate/5
        public async Task<IActionResult> Activate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Active = true;
            product.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"המוצר '{product.Name}' הועבר לסטטוס פעיל!";

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        private async Task LoadDropDownLists(int? selectedCategory = null, int? selectedUnit = null)
        {
            ViewData["CategoryId"] = new SelectList(
                await _context.ProductCategories.OrderBy(c => c.Name).ToListAsync(),
                "Id",
                "Name",
                selectedCategory
            );

            ViewData["DefaultUnitId"] = new SelectList(
                await _context.Units.OrderBy(u => u.Code).ToListAsync(),
                "Id",
                "Code",
                selectedUnit
            );
        }
    }
}