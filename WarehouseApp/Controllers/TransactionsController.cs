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
            string? toDate)
        {
            DateTime? from = null;
            DateTime? to = null;

            if (!string.IsNullOrWhiteSpace(fromDate))
            {
                if (DateTime.TryParse(fromDate, out var f))
                    from = f.Date;
            }

            if (!string.IsNullOrWhiteSpace(toDate))
            {
                if (DateTime.TryParse(toDate, out var t))
                    to = t.Date.AddDays(1).AddTicks(-1);
            }

            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // 🔍 ФІЛЬТРИ
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(t => t.TransactionType == type);

            if (productId.HasValue)
                query = query.Where(t => t.ProductId == productId);

            if (categoryId.HasValue)
                query = query.Where(t => t.Product.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Comment != null && t.Comment.Contains(search));

            if (from.HasValue)
                query = query.Where(t => t.Date >= from.Value);

            if (to.HasValue)
                query = query.Where(t => t.Date <= to.Value);

            var data = await query
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            // 🔧 ФІЛЬТРИ ДЛЯ VIEW
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", productId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", categoryId);

            ViewBag.Type = type;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            // 🟢 ПЕРІОД ВИБІРКИ
            if (from.HasValue || to.HasValue)
            {
                var fromText = from?.ToString("dd.MM.yyyy") ?? "–";
                var toText = to?.ToString("dd.MM.yyyy") ?? "–";
                ViewBag.Period = $"Zeitraum: {fromText} – {toText}";
            }

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
            // 🔐 системні поля
            transaction.CreatedBy = User.Identity?.Name ?? "System";
            transaction.Date = DateTime.Now;
            transaction.LastModifiedBy = null;
            transaction.LastModifiedAt = null;

            // ❗ кажемо MVC не валідовувати ці поля з форми
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

            // 🔁 склад
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

            // 🔁 Відкат старої транзакції
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

            // 🔄 Застосування нової транзакції
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

            // 📝 Оновлення полів
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

            // 🔁 ВІДКОТ СКЛАДУ
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
    }
}
