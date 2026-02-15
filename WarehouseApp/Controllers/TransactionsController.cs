using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WarehouseApp.Data;
using WarehouseApp.Models;

namespace WarehouseApp.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class TransactionsController : Controller
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Transactions
        public async Task<IActionResult> Index(
    string? type,
    int? productId,
    int? categoryId,
    string? search,
    string? fromDate,
    string? toDate,
    int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 100;

            DateTime? from = null;
            DateTime? to = null;

            if (!string.IsNullOrWhiteSpace(fromDate))
                if (DateTime.TryParse(fromDate, out var f))
                    from = f.Date;

            if (!string.IsNullOrWhiteSpace(toDate))
                if (DateTime.TryParse(toDate, out var t))
                    to = t.Date.AddDays(1).AddTicks(-1);

            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // FILTERS
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(t => t.TransactionType == type);

            if (productId.HasValue)
                query = query.Where(t => t.ProductId == productId);

            if (categoryId.HasValue)
                query = query.Where(t => t.Product != null && t.Product.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Comment != null && t.Comment.Contains(search));

            if (from.HasValue)
                query = query.Where(t => t.Date >= from.Value);

            if (to.HasValue)
                query = query.Where(t => t.Date <= to.Value);

            var totalItems = await query.CountAsync();

            query = query.OrderByDescending(t => t.Date);

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // filters back to view
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", productId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", categoryId);

            ViewBag.Type = type;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(data);
        }


        // GET: /Transactions/Create
        public IActionResult Create()
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name");
            return View();
        }

        // POST: /Transactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Transaction transaction)
        {
            // system fields
            transaction.CreatedBy = User.Identity?.Name ?? "System";
            transaction.Date = DateTime.Now;
            transaction.LastModifiedBy = null;
            transaction.LastModifiedAt = null;

            //Let's say MVC shouldn't validate these fields from the form
            ModelState.Remove(nameof(transaction.CreatedBy));
            ModelState.Remove(nameof(transaction.LastModifiedBy));
            ModelState.Remove(nameof(transaction.LastModifiedAt));

            if (!ModelState.IsValid)
            {
                ViewData["ProductId"] = new SelectList(
                    _context.Products,
                    "ProductId",
                    "Name",
                    transaction.ProductId
                );
                return View(transaction);
            }

            var product = await _context.Products.FindAsync(transaction.ProductId);
            if (product == null)
            {
                ModelState.AddModelError("", "Artikel nicht gefunden.");
                ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name");
                return View(transaction);
            }

            // stock
            if (transaction.TransactionType == "IN")
            {
                product.Quantity += transaction.Quantity;
            }
            else if (transaction.TransactionType == "OUT")
            {
                if (transaction.Quantity > product.Quantity)
                {
                    ModelState.AddModelError(nameof(transaction.Quantity),
                        $"Nur {product.Quantity} Artikel auf Lager verfügbar.");

                    ViewData["ProductId"] = new SelectList(
                        _context.Products,
                        "ProductId",
                        "Name",
                        transaction.ProductId
                    );
                    return View(transaction);
                }
                product.Quantity -= transaction.Quantity;
            }

            _context.Transactions.Add(transaction);
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Buchung wurde erfolgreich gespeichert.";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Transaction model)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (transaction == null)
                return NotFound();

            var oldProduct = await _context.Products.FindAsync(transaction.ProductId);
            if (oldProduct == null)
                return NotFound();

            // Rollback of the old transaction
            if (transaction.TransactionType == "IN")
                oldProduct.Quantity -= transaction.Quantity;
            else
                oldProduct.Quantity += transaction.Quantity;

            var newProduct = await _context.Products.FindAsync(model.ProductId);
            if (newProduct == null)
            {
                ModelState.AddModelError("", "Ausgewähltes Produkt wurde nicht gefunden.");
                goto ReturnView;
            }

            // Applying a new transaction
            if (model.TransactionType == "IN")
            {
                newProduct.Quantity += model.Quantity;
            }
            else if (model.TransactionType == "OUT")
            {
                if (model.Quantity > newProduct.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Nicht genügend Bestand für dieses Produkt.");
                    goto ReturnView;
                }
                newProduct.Quantity -= model.Quantity;
            }

            // Field Update
            transaction.ProductId = model.ProductId;
            transaction.Quantity = model.Quantity;
            transaction.TransactionType = model.TransactionType;
            transaction.Comment = model.Comment;
            transaction.LastModifiedBy = User.Identity?.Name ?? "System";
            transaction.LastModifiedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Buchung wurde erfolgreich aktualisiert";
            return RedirectToAction(nameof(Index));

        ReturnView:
            ViewData["ProductId"] = new SelectList(
                _context.Products.OrderBy(p => p.Name),
                "ProductId",
                "Name",
                model.ProductId
            );
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (transaction == null)
                return NotFound();

            ViewData["ProductId"] = new SelectList(
                _context.Products.OrderBy(p => p.Name),
                "ProductId",
                "Name",
                transaction.ProductId
            );

            return View(transaction);
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (transaction == null)
                return NotFound();

            return View(transaction);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (transaction == null)
                return NotFound();

            var product = transaction.Product;

            if (product == null)
                return BadRequest();

            // Stock Rollback
            if (transaction.TransactionType == "IN")
            {
                product.Quantity -= transaction.Quantity;
            }
            else if (transaction.TransactionType == "OUT")
            {
                product.Quantity += transaction.Quantity;
            }

            _context.Transactions.Remove(transaction);
            _context.Products.Update(product);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Buchung wurde gelöscht.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportTransactions(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File not selected");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet(1);

            int row = 2;

            while (!ws.Cell(row, 1).IsEmpty())
            {
                var dateCell = ws.Cell(row, 1);
                DateTime date;

                // 1️⃣ спочатку пробуємо як Excel date
                if (dateCell.TryGetValue<DateTime>(out date))
                {
                    // OK
                }
                else
                {
                    var dateText = dateCell.GetString();

                    // 2️⃣ пробуємо німецький формат
                    if (!DateTime.TryParse(
                            dateText,
                            new CultureInfo("de-DE"),
                            DateTimeStyles.None,
                            out date))
                    {
                        // ❗ якщо не вдалося — пропускаємо рядок
                        row++;
                        continue;
                    }
                }
                var productName = ws.Cell(row, 2).GetString().Trim();
                var categoryName = ws.Cell(row, 3).GetString().Trim();
                var typeRaw = ws.Cell(row, 4).GetString().Trim().ToUpper();

                string type;

                if (typeRaw.StartsWith("IN") || typeRaw.Contains("EING"))
                    type = "IN";
                else if (typeRaw.StartsWith("OUT") || typeRaw.Contains("AUS"))
                    type = "OUT";
                else
                {
                    // невідомий тип — пропускаємо рядок
                    row++;
                    continue;
                }
                var qty = ws.Cell(row, 5).GetValue<int>();
                var comment = ws.Cell(row, 6).GetString();

                // 🔹 category
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == categoryName);

                if (category == null)
                {
                    category = new Category { Name = categoryName };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }

                // 🔹 product
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Name == productName);

                if (product == null)
                {
                    product = new Product
                    {
                        Name = productName,
                        CategoryId = category.CategoryId,
                        MinimumStock = 0
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                }

                // 🔹 transaction
                var transaction = new Transaction
                {
                    ProductId = product.ProductId,
                    Date = date,
                    Quantity = qty,
                    TransactionType = type,
                    Comment = comment,
                    CreatedBy = User.Identity!.Name ?? "Import"
                };

                // update stock
                if (type == "IN")
                    product.Quantity += qty;
                else if (type == "OUT")
                    product.Quantity -= qty;

                _context.Products.Update(product);
                _context.Transactions.Add(transaction);

                row++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Import completed";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult OpeningBalances()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> OpeningBalances(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "Datei nicht ausgewählt";
                return View();
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet(1);

            int row = 2;

            while (!ws.Cell(row, 1).IsEmpty())
            {
                var dateCell = ws.Cell(row, 1);
                DateTime date;

                // 1️⃣ спочатку пробуємо як Excel date
                if (dateCell.TryGetValue<DateTime>(out date))
                {
                    // OK
                }
                else
                {
                    var dateText = dateCell.GetString();

                    // 2️⃣ пробуємо німецький формат
                    if (!DateTime.TryParse(
                            dateText,
                            new CultureInfo("de-DE"),
                            DateTimeStyles.None,
                            out date))
                    {
                        // ❗ якщо не вдалося — пропускаємо рядок
                        row++;
                        continue;
                    }
                }
                var productName = ws.Cell(row, 2).GetString().Trim();
                var categoryName = ws.Cell(row, 3).GetString().Trim();
                var typeRaw = ws.Cell(row, 4).GetString().Trim().ToUpper();

                string type;

                if (typeRaw.StartsWith("IN") || typeRaw.Contains("EING"))
                    type = "IN";
                else if (typeRaw.StartsWith("OUT") || typeRaw.Contains("AUS"))
                    type = "OUT";
                else
                {
                    // невідомий тип — пропускаємо рядок
                    row++;
                    continue;
                }
                var qty = ws.Cell(row, 5).GetValue<int>();
                var comment = ws.Cell(row, 6).GetString();

                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == categoryName);

                if (category == null)
                {
                    category = new Category { Name = categoryName };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Name == productName);

                if (product == null)
                {
                    product = new Product
                    {
                        Name = productName,
                        CategoryId = category.CategoryId,
                        Quantity = 0,
                        MinimumStock = 0
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                }

                // update stock according to type
                if (type == "IN")
                {
                    product.Quantity += qty;
                }
                else if (type == "OUT")
                {
                    product.Quantity -= qty;
                }

                var transaction = new Transaction
                {
                    ProductId = product.ProductId,
                    Date = date,
                    Quantity = qty,
                    TransactionType = type, // ⭐ важливо!
                    Comment = comment,
                    CreatedBy = User.Identity?.Name ?? "Import"
                };

                _context.Transactions.Add(transaction);
                _context.Products.Update(product);

                row++;
            }

            await _context.SaveChangesAsync();

            ViewBag.Message = "Import erfolgreich abgeschlossen";
            return View();
        }
    }
}
