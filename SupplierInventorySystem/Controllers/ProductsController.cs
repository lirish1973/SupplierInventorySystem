using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using System;
using System.IO;
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
                .Include(p => p.ProductImages)
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
                .Include(p => p.ProductImages!.OrderBy(pi => pi.DisplayOrder))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            await _context.Entry(product)
                .Collection(p => p.SupplierProducts!)
                .Query()
                .Include(sp => sp.Supplier)
                .LoadAsync();

            await _context.Entry(product)
                .Collection(p => p.PriceHistories!)
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

            var product = await _context.Products
                .Include(p => p.ProductImages!.OrderBy(pi => pi.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);
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

        // GET: Products/AdjustStock/5
        public async Task<IActionResult> AdjustStock(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.DefaultUnit)
                .Include(p => p.StockAdjustmentLogs!.OrderByDescending(l => l.AdjustedAt).Take(10))
                    .ThenInclude(l => l.AdjustedBy)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/AdjustStock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int id, decimal quantityChange, string reason)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (quantityChange == 0)
            {
                TempData["ErrorMessage"] = "כמות השינוי חייבת להיות שונה מאפס";
                return RedirectToAction(nameof(AdjustStock), new { id });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "יש להזין סיבה לעדכון המלאי";
                return RedirectToAction(nameof(AdjustStock), new { id });
            }

            var qtyBefore = product.StockQuantity;
            var qtyAfter = product.StockQuantity + quantityChange;

            if (qtyAfter < 0)
            {
                TempData["ErrorMessage"] = $"לא ניתן להוריד מלאי לערך שלילי (מלאי נוכחי: {qtyBefore})";
                return RedirectToAction(nameof(AdjustStock), new { id });
            }

            // עדכון המלאי
            product.StockQuantity = qtyAfter;
            product.UpdatedAt = DateTime.Now;

            // רישום בלוג
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? userId = int.TryParse(userIdClaim, out int uid) ? uid : null;

            _context.StockAdjustmentLogs.Add(new StockAdjustmentLog
            {
                ProductId = id,
                QuantityChange = quantityChange,
                QuantityBefore = qtyBefore,
                QuantityAfter = qtyAfter,
                Reason = reason.Trim(),
                AdjustedById = userId,
                AdjustedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            var direction = quantityChange > 0 ? "נוסף" : "הורד";
            TempData["SuccessMessage"] = $"המלאי עודכן! {Math.Abs(quantityChange)} {direction} - מלאי חדש: {qtyAfter}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Products/ExportExcel
        public async Task<IActionResult> ExportExcel(bool? lowStockOnly)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.DefaultUnit)
                .Where(p => p.Active)
                .AsQueryable();

            if (lowStockOnly == true)
                query = query.Where(p => !p.IsService && p.ReorderPoint > 0 && p.StockQuantity <= p.ReorderPoint);

            var products = await query
                .OrderBy(p => p.Category != null ? p.Category.Name : "")
                .ThenBy(p => p.Name)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("דוח מלאי");

            // כותרת ראשית
            ws.Cell(1, 1).Value = "דוח מלאי - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            ws.Range(1, 1, 1, 8).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2c3e50");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;

            // כותרות עמודות
            var headers = new[] { "SKU", "שם מוצר", "קטגוריה", "יחידה", "מלאי נוכחי", "נקודת הזמנה", "כמות הזמנה", "סטטוס" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(2, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3498db");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // נתונים
            int row = 3;
            foreach (var p in products)
            {
                bool isLow = !p.IsService && p.ReorderPoint > 0 && p.StockQuantity <= p.ReorderPoint;
                string status = p.IsService ? "שירות" : (isLow ? "⚠ מלאי נמוך" : "✓ תקין");

                ws.Cell(row, 1).Value = p.Sku ?? "";
                ws.Cell(row, 2).Value = p.Name;
                ws.Cell(row, 3).Value = p.Category?.Name ?? "";
                ws.Cell(row, 4).Value = p.DefaultUnit?.Code ?? "";
                ws.Cell(row, 5).Value = (double)p.StockQuantity;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 6).Value = (double)p.ReorderPoint;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 7).Value = (double)p.ReorderQty;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.##";
                ws.Cell(row, 8).Value = status;

                // צביעה לשורות מלאי נמוך
                if (isLow)
                {
                    ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#fde8e8");
                    ws.Cell(row, 8).Style.Font.FontColor = XLColor.FromHtml("#c0392b");
                    ws.Cell(row, 8).Style.Font.Bold = true;
                }
                else if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
                }

                // גבולות
                ws.Range(row, 1, row, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, 8).Style.Border.InsideBorder = XLBorderStyleValues.Hair;

                row++;
            }

            // שורת סיכום
            ws.Cell(row, 1).Value = "סה\"כ מוצרים:";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = products.Count;
            ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#ecf0f1");
            ws.Range(row, 1, row, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            // רוחב עמודות אוטומטי
            ws.Columns().AdjustToContents();
            ws.Column(2).Width = Math.Max(ws.Column(2).Width, 25); // שם מוצר - מינימום

            // כיוון גיליון RTL
            ws.RightToLeft = true;

            // שם קובץ
            string suffix = lowStockOnly == true ? "_מלאי_נמוך" : "_מלאי_מלא";
            string filename = $"stock_report{suffix}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
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