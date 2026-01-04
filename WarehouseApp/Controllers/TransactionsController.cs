using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
            string? type, int? productId, int? categoryId,
            string? search, DateTime? fromDate, DateTime? toDate,
            int pageNumber = 1, int pageSize = 20)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // 🔍 Фільтри
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(t => t.TransactionType == type);

            if (productId.HasValue)
                query = query.Where(t => t.ProductId == productId);

            if (categoryId.HasValue)
                query = query.Where(t => t.Product.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Comment != null && t.Comment.Contains(search));

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value);

            // 🔽 Сортування
            query = query.OrderByDescending(t => t.Date);

            // 📊 Пагінація
            var paginatedTransactions = await PaginatedList<Transaction>.CreateAsync(query, pageNumber, pageSize);

            // 🔧 Дані для фільтрів
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", productId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", categoryId);

            ViewBag.Type = type;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.PageSize = pageSize;

            return View(paginatedTransactions);
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
            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(transaction.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", "Product not found.");
                    return View(transaction);
                }

                // 🔁 Оновлення кількості на складі
                if (transaction.TransactionType.ToUpper() == "IN")
                {
                    product.Quantity += transaction.Quantity;
                }
                else if (transaction.TransactionType.ToUpper() == "OUT")
                {
                    if (transaction.Quantity > product.Quantity)
                    {
                        ModelState.AddModelError(nameof(transaction.Quantity), $"Only {product.Quantity} items available in stock.");
                        ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
                        return View(transaction);
                    }
                    product.Quantity -= transaction.Quantity;
                }
                else
                {
                    ModelState.AddModelError("", "Invalid transaction type (use IN or OUT).");
                    return View(transaction);
                }

                transaction.Date = DateTime.Now;
                _context.Add(transaction);
                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Transaction saved successfully.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                foreach (var error in errors)
                    Console.WriteLine("MODEL ERROR: " + error);

                ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
                return View(transaction);
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
            return View(transaction);
        }
    }
}
