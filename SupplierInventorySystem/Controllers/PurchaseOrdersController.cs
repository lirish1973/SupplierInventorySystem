using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using SupplierInventorySystem.ViewModels;
using System.Security.Claims;

namespace SupplierInventorySystem.Controllers
{
    [Authorize]
    public class PurchaseOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PurchaseOrdersController> _logger;

        public PurchaseOrdersController(ApplicationDbContext context, ILogger<PurchaseOrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: PurchaseOrders
        public async Task<IActionResult> Index(string searchString, int? supplierId, string status, DateTime? fromDate, DateTime? toDate)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSupplier"] = supplierId;
            ViewData["CurrentStatus"] = status;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

            // Load suppliers for filter
            ViewBag.Suppliers = new SelectList(
                await _context.Suppliers.Where(s => s.Status == "active").OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name");

            // Status options
            ViewBag.Statuses = PurchaseOrderStatus.GetStatusDisplayNames()
                .Select(s => new SelectListItem { Value = s.Key, Text = s.Value });

            var orders = _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.CreatedBy)
                .Include(po => po.Items)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(po =>
                    po.OrderNumber.Contains(searchString) ||
                    po.Supplier!.Name.Contains(searchString));
            }

            if (supplierId.HasValue)
            {
                orders = orders.Where(po => po.SupplierId == supplierId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(po => po.Status == status);
            }

            if (fromDate.HasValue)
            {
                orders = orders.Where(po => po.OrderDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                orders = orders.Where(po => po.OrderDate <= toDate.Value);
            }

            var orderList = await orders
                .OrderByDescending(po => po.OrderDate)
                .ThenByDescending(po => po.Id)
                .Select(po => new PurchaseOrderListViewModel
                {
                    Id = po.Id,
                    OrderNumber = po.OrderNumber,
                    SupplierName = po.Supplier!.Name,
                    OrderDate = po.OrderDate,
                    ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                    Status = po.Status,
                    TotalAmount = po.TotalAmount,
                    Currency = po.Currency,
                    ItemCount = po.Items!.Count,
                    CreatedByName = po.CreatedBy != null ? po.CreatedBy.FullName ?? po.CreatedBy.Username : null
                })
                .ToListAsync();

            return View(orderList);
        }

        // GET: PurchaseOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.CreatedBy)
                .Include(po => po.Items!)
                    .ThenInclude(i => i.Product)
                .Include(po => po.Items!)
                    .ThenInclude(i => i.Unit)
                .FirstOrDefaultAsync(po => po.Id == id);

            if (order == null) return NotFound();

            var viewModel = new PurchaseOrderDetailsViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Supplier = order.Supplier,
                OrderDate = order.OrderDate,
                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
                ActualDeliveryDate = order.ActualDeliveryDate,
                Status = order.Status,
                Subtotal = order.Subtotal,
                TaxAmount = order.TaxAmount,
                DiscountAmount = order.DiscountAmount,
                ShippingCost = order.ShippingCost,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                PaymentTerms = order.PaymentTerms,
                Notes = order.Notes,
                InternalNotes = order.InternalNotes,
                CreatedBy = order.CreatedBy,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.Items?.ToList() ?? new List<PurchaseOrderItem>()
            };

            return View(viewModel);
        }

        // GET: PurchaseOrders/Create
        public async Task<IActionResult> Create(int? supplierId)
        {
            await LoadDropdowns(supplierId);

            var viewModel = new PurchaseOrderFormViewModel
            {
                OrderDate = DateTime.Now,
                SupplierId = supplierId ?? 0,
                Currency = "ILS"
            };

            // If supplier selected, load their default payment terms
            if (supplierId.HasValue)
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId.Value);
                if (supplier != null)
                {
                    viewModel.PaymentTerms = supplier.DefaultPaymentTerms;
                    viewModel.Currency = supplier.DefaultCurrency;
                    if (supplier.LeadTimeDays.HasValue)
                    {
                        viewModel.ExpectedDeliveryDate = DateTime.Now.AddDays(supplier.LeadTimeDays.Value);
                    }
                }
            }

            return View(viewModel);
        }

        // POST: PurchaseOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var order = new PurchaseOrder
                {
                    OrderNumber = await GenerateOrderNumber(),
                    SupplierId = model.SupplierId,
                    OrderDate = model.OrderDate,
                    ExpectedDeliveryDate = model.ExpectedDeliveryDate,
                    PaymentTerms = model.PaymentTerms,
                    Currency = model.Currency,
                    Notes = model.Notes,
                    InternalNotes = model.InternalNotes,
                    Status = PurchaseOrderStatus.Draft,
                    CreatedById = GetCurrentUserId(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.PurchaseOrders.Add(order);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"הזמנה {order.OrderNumber} נוצרה בהצלחה! הוסף פריטים להזמנה.";
                return RedirectToAction(nameof(Edit), new { id = order.Id });
            }

            await LoadDropdowns(model.SupplierId);
            return View(model);
        }

        // GET: PurchaseOrders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.PurchaseOrders
                .Include(po => po.Items!)
                    .ThenInclude(i => i.Product)
                .Include(po => po.Items!)
                    .ThenInclude(i => i.Unit)
                .FirstOrDefaultAsync(po => po.Id == id);

            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Draft)
            {
                TempData["ErrorMessage"] = "ניתן לערוך רק הזמנות בסטטוס טיוטה";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new PurchaseOrderFormViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                SupplierId = order.SupplierId,
                OrderDate = order.OrderDate,
                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
                PaymentTerms = order.PaymentTerms,
                Currency = order.Currency,
                Notes = order.Notes,
                InternalNotes = order.InternalNotes,
                Subtotal = order.Subtotal,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                ShippingCost = order.ShippingCost,
                TotalAmount = order.TotalAmount,
                Items = order.Items?.Select(i => new PurchaseOrderItemFormViewModel
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name,
                    ProductSku = i.Product?.Sku,
                    Description = i.Description,
                    UnitId = i.UnitId,
                    UnitCode = i.Unit?.Code,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountPercent = i.DiscountPercent,
                    LineTotal = i.LineTotal,
                    Notes = i.Notes
                }).ToList() ?? new List<PurchaseOrderItemFormViewModel>()
            };

            await LoadDropdowns(order.SupplierId);
            return View(viewModel);
        }

        // POST: PurchaseOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseOrderFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var order = await _context.PurchaseOrders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Draft)
            {
                TempData["ErrorMessage"] = "ניתן לערוך רק הזמנות בסטטוס טיוטה";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (ModelState.IsValid)
            {
                order.SupplierId = model.SupplierId;
                order.OrderDate = model.OrderDate;
                order.ExpectedDeliveryDate = model.ExpectedDeliveryDate;
                order.PaymentTerms = model.PaymentTerms;
                order.Currency = model.Currency;
                order.Notes = model.Notes;
                order.InternalNotes = model.InternalNotes;
                order.ShippingCost = model.ShippingCost;
                order.UpdatedAt = DateTime.Now;

                await RecalculateOrderTotals(order.Id);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "ההזמנה עודכנה בהצלחה!";
                return RedirectToAction(nameof(Edit), new { id });
            }

            await LoadDropdowns(model.SupplierId);
            return View(model);
        }

        // POST: PurchaseOrders/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int orderId, int productId, decimal quantity, decimal unitPrice, decimal discountPercent, string? notes)
        {
            var order = await _context.PurchaseOrders.FindAsync(orderId);
            if (order == null || order.Status != PurchaseOrderStatus.Draft)
            {
                return Json(new { success = false, message = "לא ניתן להוסיף פריטים להזמנה זו" });
            }

            var product = await _context.Products.Include(p => p.DefaultUnit).FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "המוצר לא נמצא" });
            }

            var item = new PurchaseOrderItem
            {
                PurchaseOrderId = orderId,
                ProductId = productId,
                Description = product.Name,
                UnitId = product.DefaultUnitId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                DiscountPercent = discountPercent,
                Notes = notes
            };
            item.CalculateLineTotal();

            _context.PurchaseOrderItems.Add(item);
            await _context.SaveChangesAsync();
            await RecalculateOrderTotals(orderId);

            return Json(new { success = true, message = "הפריט נוסף בהצלחה" });
        }

        // POST: PurchaseOrders/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int itemId)
        {
            var item = await _context.PurchaseOrderItems
                .Include(i => i.PurchaseOrder)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                return Json(new { success = false, message = "הפריט לא נמצא" });
            }

            if (item.PurchaseOrder?.Status != PurchaseOrderStatus.Draft)
            {
                return Json(new { success = false, message = "לא ניתן למחוק פריטים מהזמנה שאינה בטיוטה" });
            }

            var orderId = item.PurchaseOrderId;
            _context.PurchaseOrderItems.Remove(item);
            await _context.SaveChangesAsync();
            await RecalculateOrderTotals(orderId);

            return Json(new { success = true, message = "הפריט הוסר בהצלחה" });
        }

        // POST: PurchaseOrders/Send/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id)
        {
            var order = await _context.PurchaseOrders
                .Include(po => po.Items)
                .FirstOrDefaultAsync(po => po.Id == id);

            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Draft)
            {
                TempData["ErrorMessage"] = "ניתן לשלוח רק הזמנות בסטטוס טיוטה";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (order.Items == null || !order.Items.Any())
            {
                TempData["ErrorMessage"] = "לא ניתן לשלוח הזמנה ללא פריטים";
                return RedirectToAction(nameof(Edit), new { id });
            }

            order.Status = PurchaseOrderStatus.Sent;
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Purchase order {order.OrderNumber} sent to supplier");
            TempData["SuccessMessage"] = $"ההזמנה {order.OrderNumber} נשלחה לספק בהצלחה!";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: PurchaseOrders/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var order = await _context.PurchaseOrders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Sent)
            {
                TempData["ErrorMessage"] = "ניתן לאשר רק הזמנות שנשלחו";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = PurchaseOrderStatus.Confirmed;
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "ההזמנה אושרה על ידי הספק!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: PurchaseOrders/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var order = await _context.PurchaseOrders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status == PurchaseOrderStatus.Received || order.Status == PurchaseOrderStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "לא ניתן לבטל הזמנה זו";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = PurchaseOrderStatus.Cancelled;
            order.InternalNotes = string.IsNullOrEmpty(order.InternalNotes)
                ? $"בוטל: {reason}"
                : $"{order.InternalNotes}\n\nבוטל: {reason}";
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Purchase order {order.OrderNumber} cancelled. Reason: {reason}");
            TempData["SuccessMessage"] = "ההזמנה בוטלה!";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: PurchaseOrders/Receive/5
        public async Task<IActionResult> Receive(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Items!)
                    .ThenInclude(i => i.Product)
                .Include(po => po.Items!)
                    .ThenInclude(i => i.Unit)
                .FirstOrDefaultAsync(po => po.Id == id);

            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Confirmed &&
                order.Status != PurchaseOrderStatus.Shipped &&
                order.Status != PurchaseOrderStatus.PartiallyReceived)
            {
                TempData["ErrorMessage"] = "לא ניתן לקבל סחורה להזמנה זו";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new ReceiveGoodsViewModel
            {
                PurchaseOrderId = order.Id,
                OrderNumber = order.OrderNumber,
                SupplierName = order.Supplier?.Name ?? "",
                ReceiveDate = DateTime.Now,
                Items = order.Items?.Select(i => new ReceiveItemViewModel
                {
                    ItemId = i.Id,
                    ProductName = i.Product?.Name ?? "",
                    ProductSku = i.Product?.Sku ?? "",
                    UnitCode = i.Unit?.Code,
                    OrderedQuantity = i.Quantity,
                    PreviouslyReceived = i.QuantityReceived,
                    RemainingQuantity = i.Quantity - i.QuantityReceived,
                    QuantityToReceive = i.Quantity - i.QuantityReceived
                }).Where(i => i.RemainingQuantity > 0).ToList() ?? new List<ReceiveItemViewModel>()
            };

            return View(viewModel);
        }

        // POST: PurchaseOrders/Receive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(ReceiveGoodsViewModel model)
        {
            var order = await _context.PurchaseOrders
                .Include(po => po.Items)
                .FirstOrDefaultAsync(po => po.Id == model.PurchaseOrderId);

            if (order == null) return NotFound();

            bool allReceived = true;
            foreach (var receiveItem in model.Items)
            {
                if (receiveItem.QuantityToReceive > 0)
                {
                    var item = order.Items?.FirstOrDefault(i => i.Id == receiveItem.ItemId);
                    if (item != null)
                    {
                        item.QuantityReceived += receiveItem.QuantityToReceive;
                        if (item.QuantityReceived < item.Quantity)
                        {
                            allReceived = false;
                        }
                    }
                }
                else
                {
                    var item = order.Items?.FirstOrDefault(i => i.Id == receiveItem.ItemId);
                    if (item != null && item.QuantityReceived < item.Quantity)
                    {
                        allReceived = false;
                    }
                }
            }

            order.Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
            if (allReceived)
            {
                order.ActualDeliveryDate = model.ReceiveDate;
            }
            order.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrEmpty(model.Notes))
            {
                order.InternalNotes = string.IsNullOrEmpty(order.InternalNotes)
                    ? $"קבלה ({model.ReceiveDate:dd/MM/yyyy}): {model.Notes}"
                    : $"{order.InternalNotes}\n\nקבלה ({model.ReceiveDate:dd/MM/yyyy}): {model.Notes}";
            }

            await _context.SaveChangesAsync();

            var message = allReceived ? "כל הסחורה התקבלה בהצלחה!" : "הסחורה התקבלה חלקית";
            TempData["SuccessMessage"] = message;

            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        // GET: PurchaseOrders/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .FirstOrDefaultAsync(po => po.Id == id);

            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Draft && order.Status != PurchaseOrderStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "ניתן למחוק רק הזמנות בסטטוס טיוטה או מבוטל";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(order);
        }

        // POST: PurchaseOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.PurchaseOrders
                .Include(po => po.Items)
                .FirstOrDefaultAsync(po => po.Id == id);

            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Draft && order.Status != PurchaseOrderStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "ניתן למחוק רק הזמנות בסטטוס טיוטה או מבוטל";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Remove items first
            if (order.Items != null)
            {
                _context.PurchaseOrderItems.RemoveRange(order.Items);
            }

            _context.PurchaseOrders.Remove(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Purchase order {order.OrderNumber} deleted");
            TempData["SuccessMessage"] = $"ההזמנה {order.OrderNumber} נמחקה!";

            return RedirectToAction(nameof(Index));
        }

        // API: Get products for autocomplete
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term)
        {
            var products = await _context.Products
                .Include(p => p.DefaultUnit)
                .Where(p => p.Active && (p.Name.Contains(term) || p.Sku.Contains(term)))
                .Take(20)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    sku = p.Sku,
                    unitId = p.DefaultUnitId,
                    unitCode = p.DefaultUnit != null ? p.DefaultUnit.Code : null
                })
                .ToListAsync();

            return Json(products);
        }

        // API: Get supplier products with prices
        [HttpGet]
        public async Task<IActionResult> GetSupplierProducts(int supplierId, string term)
        {
            var products = await _context.SupplierProducts
                .Include(sp => sp.Product)
                    .ThenInclude(p => p!.DefaultUnit)
                .Where(sp => sp.SupplierId == supplierId &&
                             sp.Product!.Active &&
                             (sp.Product.Name.Contains(term) || sp.Product.Sku.Contains(term)))
                .Take(20)
                .Select(sp => new
                {
                    id = sp.ProductId,
                    name = sp.Product!.Name,
                    sku = sp.Product.Sku,
                    unitId = sp.Product.DefaultUnitId,
                    unitCode = sp.Product.DefaultUnit != null ? sp.Product.DefaultUnit.Code : null,
                    price = sp.Price,
                    supplierSku = sp.SupplierSku
                })
                .ToListAsync();

            return Json(products);
        }

        #region Helper Methods

        private async Task<string> GenerateOrderNumber()
        {
            var today = DateTime.Now;
            var prefix = $"PO-{today:yyyyMM}";

            var lastOrder = await _context.PurchaseOrders
                .Where(po => po.OrderNumber.StartsWith(prefix))
                .OrderByDescending(po => po.OrderNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastOrder != null)
            {
                var lastNumberStr = lastOrder.OrderNumber.Replace(prefix + "-", "");
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}-{nextNumber:D4}";
        }

        private async Task RecalculateOrderTotals(int orderId)
        {
            var order = await _context.PurchaseOrders
                .Include(po => po.Items)
                .FirstOrDefaultAsync(po => po.Id == orderId);

            if (order?.Items != null)
            {
                order.Subtotal = order.Items.Sum(i => i.LineTotal);
                order.TaxAmount = Math.Round(order.Subtotal * 0.17m, 2); // 17% VAT
                order.TotalAmount = order.Subtotal + order.TaxAmount + order.ShippingCost - order.DiscountAmount;
                order.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }

        private async Task LoadDropdowns(int? selectedSupplier = null)
        {
            ViewData["SupplierId"] = new SelectList(
                await _context.Suppliers.Where(s => s.Status == "active").OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name", selectedSupplier);

            ViewData["Currencies"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "ILS", Text = "₪ שקל" },
                new SelectListItem { Value = "USD", Text = "$ דולר" },
                new SelectListItem { Value = "EUR", Text = "€ יורו" }
            };
        }

        #endregion
    }
}
