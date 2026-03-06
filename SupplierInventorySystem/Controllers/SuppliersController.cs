using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using SupplierInventorySystem.ViewModels;
using System.IO;


namespace SupplierInventorySystem.Controllers
{
    [Authorize]
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Suppliers
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CodeSortParm"] = sortOrder == "Code" ? "code_desc" : "Code";
            ViewData["CurrentFilter"] = searchString;

            var suppliers = from s in _context.Suppliers
                            select s;

            // חיפוש
            if (!String.IsNullOrEmpty(searchString))
            {
                suppliers = suppliers.Where(s => s.Name.Contains(searchString)
                                       || s.Code!.Contains(searchString)
                                       || s.TaxId!.Contains(searchString));
            }

            // מיון
            switch (sortOrder)
            {
                case "name_desc":
                    suppliers = suppliers.OrderByDescending(s => s.Name);
                    break;
                case "Code":
                    suppliers = suppliers.OrderBy(s => s.Code);
                    break;
                case "code_desc":
                    suppliers = suppliers.OrderByDescending(s => s.Code);
                    break;
                default:
                    suppliers = suppliers.OrderBy(s => s.Name);
                    break;
            }

            return View(await suppliers.AsNoTracking().ToListAsync());
        }

        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .Include(s => s.SupplierContacts)
                .Include(s => s.SupplierAddresses)
                .Include(s => s.SupplierMetrics.OrderByDescending(m => m.MetricDate).Take(5))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: Suppliers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Code,Name,LegalName,TaxId,DefaultCurrency,DefaultPaymentTerms,LeadTimeDays,Rating,Status")] Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                supplier.CreatedAt = DateTime.Now;
                supplier.UpdatedAt = DateTime.Now;
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"הספק '{supplier.Name}' נוסף בהצלחה!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        // POST: Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Name,LegalName,TaxId,DefaultCurrency,DefaultPaymentTerms,LeadTimeDays,Rating,Status,CreatedAt")] Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    supplier.UpdatedAt = DateTime.Now;
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"הספק '{supplier.Name}' עודכן בהצלחה!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
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
            return View(supplier);
        }

        // GET: Suppliers/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"הספק '{supplier.Name}' נמחק בהצלחה!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Suppliers/Performance
        public async Task<IActionResult> Performance()
        {
            // לכל ספק - חשב נתוני ביצועים מהזמנות רכש
            var suppliers = await _context.Suppliers
                .Where(s => s.Status == "active")
                .ToListAsync();

            var allOrders = await _context.PurchaseOrders
                .Include(po => po.Items)
                .Where(po => po.Status != "Draft" && po.Status != "Cancelled")
                .ToListAsync();

            var perfList = suppliers.Select(s =>
            {
                var sOrders = allOrders.Where(po => po.SupplierId == s.Id).ToList();
                var receivedOrders = sOrders.Where(po => po.Status == "Received" || po.Status == "PartiallyReceived").ToList();

                // אחוז אספקה בזמן
                int onTimeCount = receivedOrders.Count(po =>
                    po.ExpectedDeliveryDate.HasValue &&
                    po.ActualDeliveryDate.HasValue &&
                    po.ActualDeliveryDate.Value <= po.ExpectedDeliveryDate.Value);

                decimal onTimeRate = receivedOrders.Count > 0
                    ? Math.Round((decimal)onTimeCount / receivedOrders.Count * 100, 1)
                    : 0;

                // ממוצע ימי עיכוב
                var delays = receivedOrders
                    .Where(po => po.ExpectedDeliveryDate.HasValue && po.ActualDeliveryDate.HasValue)
                    .Select(po => (po.ActualDeliveryDate!.Value - po.ExpectedDeliveryDate!.Value).TotalDays)
                    .ToList();
                double avgDelay = delays.Count > 0 ? delays.Average() : 0;

                // סה"כ קניות
                decimal totalPurchased = sOrders.Sum(po => po.TotalAmount);

                // הזמנה ממוצעת
                decimal avgOrder = sOrders.Count > 0 ? totalPurchased / sOrders.Count : 0;

                // מטריקות ידניות (אם יש)
                var latestMetric = _context.SupplierMetrics
                    .Where(m => m.SupplierId == s.Id)
                    .OrderByDescending(m => m.MetricDate)
                    .FirstOrDefault();

                return new SupplierPerformanceDto
                {
                    SupplierId = s.Id,
                    SupplierName = s.Name,
                    SupplierCode = s.Code,
                    Rating = s.Rating,
                    TotalOrders = sOrders.Count,
                    ReceivedOrders = receivedOrders.Count,
                    OnTimeRate = onTimeRate,
                    AvgDelayDays = Math.Round(avgDelay, 1),
                    TotalPurchased = totalPurchased,
                    AvgOrderValue = Math.Round(avgOrder, 0),
                    ManualOnTimeRate = latestMetric?.OnTimePercentage,
                    ManualDefectRate = latestMetric?.DefectRate,
                    LastMetricDate = latestMetric?.MetricDate
                };
            })
            .OrderByDescending(p => p.TotalPurchased)
            .ToList();

            return View(perfList);
        }

        // GET: Suppliers/ExportPerformance
        public async Task<IActionResult> ExportPerformance()
        {
            var suppliers = await _context.Suppliers.Where(s => s.Status == "active").ToListAsync();
            var allOrders = await _context.PurchaseOrders
                .Where(po => po.Status != "Draft" && po.Status != "Cancelled")
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ביצועי ספקים");

            ws.Cell(1, 1).Value = "דוח ביצועי ספקים - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            ws.Range(1, 1, 1, 9).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2c3e50");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;

            var headers = new[] { "שם ספק", "קוד", "דירוג", "סה\"כ הזמנות", "הזמנות שהתקבלו", "% בזמן", "עיכוב ממוצע (ימים)", "סה\"כ רכישות", "ערך הזמנה ממוצע" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(2, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#16a085");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int row = 3;
            foreach (var s in suppliers.OrderBy(s => s.Name))
            {
                var sOrders = allOrders.Where(po => po.SupplierId == s.Id).ToList();
                var receivedOrders = sOrders.Where(po => po.Status == "Received" || po.Status == "PartiallyReceived").ToList();

                int onTimeCount = receivedOrders.Count(po =>
                    po.ExpectedDeliveryDate.HasValue && po.ActualDeliveryDate.HasValue &&
                    po.ActualDeliveryDate.Value <= po.ExpectedDeliveryDate.Value);

                decimal onTimeRate = receivedOrders.Count > 0
                    ? Math.Round((decimal)onTimeCount / receivedOrders.Count * 100, 1) : 0;

                var delays = receivedOrders
                    .Where(po => po.ExpectedDeliveryDate.HasValue && po.ActualDeliveryDate.HasValue)
                    .Select(po => (po.ActualDeliveryDate!.Value - po.ExpectedDeliveryDate!.Value).TotalDays).ToList();
                double avgDelay = delays.Count > 0 ? Math.Round(delays.Average(), 1) : 0;

                decimal totalPurchased = sOrders.Sum(po => po.TotalAmount);
                decimal avgOrder = sOrders.Count > 0 ? Math.Round(totalPurchased / sOrders.Count, 0) : 0;

                ws.Cell(row, 1).Value = s.Name;
                ws.Cell(row, 2).Value = s.Code ?? "";
                ws.Cell(row, 3).Value = s.Rating.HasValue ? (double)s.Rating.Value : 0;
                ws.Cell(row, 3).Style.NumberFormat.Format = "0.0";
                ws.Cell(row, 4).Value = sOrders.Count;
                ws.Cell(row, 5).Value = receivedOrders.Count;
                ws.Cell(row, 6).Value = (double)onTimeRate;
                ws.Cell(row, 6).Style.NumberFormat.Format = "0.0\"%\"";
                ws.Cell(row, 7).Value = avgDelay;
                ws.Cell(row, 7).Style.NumberFormat.Format = "0.0";
                ws.Cell(row, 8).Value = (double)totalPurchased;
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 9).Value = (double)avgOrder;
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0";

                // צביעה לפי ביצוע
                string rowColor = onTimeRate >= 90 ? "#d5f5e3" : onTimeRate >= 70 ? "#fef9e7" : receivedOrders.Count > 0 ? "#fadbd8" : "#f8f9fa";
                ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml(rowColor);
                ws.Range(row, 1, row, 9).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
                ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                row++;
            }

            ws.Columns().AdjustToContents();
            ws.RightToLeft = true;

            string filename = $"supplier_performance_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}