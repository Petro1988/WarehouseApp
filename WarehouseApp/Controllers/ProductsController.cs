using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;
using WarehouseApp.Models;


namespace WarehouseApp.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        
        // GET: /Products
        public async Task<IActionResult> Index(
            string search,
            int? categoryId,
            string sortOrder,
            int pageNumber = 1,
            int? pageSize = null)
        {
            // 🍪 Якщо не передано pageSize — беремо з cookie
            if (pageSize == null)
            {
                if (Request.Cookies.TryGetValue("PageSize", out string? savedSize))
                {
                    if (int.TryParse(savedSize, out int parsedSize))
                        pageSize = parsedSize;
                }
            }

            // 🔢 Якщо й cookie немає — дефолт 10
            pageSize ??= 10;

            // 🍪 Зберігаємо вибір у cookie (на 30 днів)
            Response.Cookies.Append("PageSize", pageSize.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            // 🔍 Пошук
            if (!string.IsNullOrEmpty(search))
                productsQuery = productsQuery.Where(p => p.Name.Contains(search));

            // 🏷️ Фільтр
            if (categoryId.HasValue)
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);

            // 📊 Сортування
            if (sortOrder == "low")
            {
                productsQuery = productsQuery.OrderBy(p => p.Quantity - p.MinimumStock);
                ViewBag.SortByStock = "";
            }
            else
            {
                productsQuery = productsQuery.OrderBy(p => p.Name);
                ViewBag.SortByStock = "low";
            }

            ViewBag.CurrentSort = sortOrder;
            ViewBag.PageSize = pageSize;
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");

            // 📄 Пагінація
            var paginatedList = await PaginatedList<Product>.CreateAsync(productsQuery, pageNumber, pageSize.Value);
            return View(paginatedList);
        }

        // GET: /Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: /Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductId) return NotFound();

            if (product.Quantity < 0 || product.MinimumStock < 0)
            {
                ModelState.AddModelError("", "Menge und Mindestbestand dürfen nicht negativ sein.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: /Products/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hasTransactions = await _context.Transactions
                .AnyAsync(t => t.ProductId == id);

            if (hasTransactions)
            {
                TempData["Error"] = "❌ Das Produkt kann nicht gelöscht werden, da es eine Buchungshistorie hat.";
                return RedirectToAction(nameof(Index));
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "❌ Das Produkt kann nicht gelöscht werden, da es in Buchungen verwendet wird.";
                return RedirectToAction(nameof(Index));
            }


            TempData["Success"] = "✅ Produkt wurde erfolgreich gelöscht.";
            return RedirectToAction(nameof(Index));
        }

    }
}
