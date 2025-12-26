using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using WarehouseApp.Data;
using WarehouseApp.Models;

namespace WarehouseApp.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // ===========================
        //          INDEX
        // ===========================
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View();
        }


        // ======================================================
        // 1️⃣ STOCK REPORT (залишки на дату + фільтри + пошук)
        // ======================================================
        public IActionResult StockReport(DateTime? date, int? categoryId, int? productId, string? search, bool export = false)
        {
            if (!date.HasValue)
                date = DateTime.Today;

            // всі транзакції до вибраної дати
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .Where(t => t.Date <= date.Value)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(t => t.Product.CategoryId == categoryId.Value);

            if (productId.HasValue)
                query = query.Where(t => t.ProductId == productId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Comment.Contains(search));

            var list = query.ToList();

            if (export)
                return ExportStockToExcel(list, date.Value);

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Products = _context.Products.ToList();

            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedProduct = productId;
            ViewBag.Search = search;
            ViewBag.Date = date.Value.ToString("yyyy-MM-dd");

            return View(list);
        }


        // ======================================================
        // 2️⃣ INCOMING REPORT (фільтр від-до + категорія + експорт)
        // ======================================================
        public async Task<IActionResult> IncomingReport(DateTime? fromDate, DateTime? toDate, int? categoryId, int? productId, string? search, bool export = false)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .Where(t => t.TransactionType == "IN")
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value);
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId.Value);
            if (productId.HasValue && productId.Value > 0)
                query = query.Where(t => t.ProductId == productId.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Comment.Contains(search));

            var list = await query.OrderByDescending(t => t.Date).ToListAsync();

            // Передаємо ViewBag для Razor
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedProduct = productId;
            ViewBag.Search = search;

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();

            if (export)
                return ExportGenericExcel(list, "IncomingReport");

            return View(list);
        }



        // ======================================================
        // 3️⃣ OUTGOING REPORT (фільтр від-до + категорія + експорт)
        // ======================================================
        public async Task<IActionResult> OutgoingReport(DateTime? fromDate, DateTime? toDate, int? categoryId, int? productId, string? search, bool export = false)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .Where(t => t.TransactionType == "OUT")
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value);
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId.Value);
            if (productId.HasValue && productId.Value > 0)
                query = query.Where(t => t.ProductId == productId.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Comment.Contains(search));

            var list = await query.OrderByDescending(t => t.Date).ToListAsync();

            // Передаємо ViewBag для Razor
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedProduct = productId;
            ViewBag.Search = search;

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync(); // <= Ось тут

            if (export)
                return ExportGenericExcel(list, "OutgoingReport");

            return View(list);
        }


        // ======================================================
        // 📌 EXPORT: STOCK
        // ======================================================
        public FileResult ExportStockToExcel(List<Transaction> data, DateTime date)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Stock Report");

            ws.Cell(1, 1).Value = "Stock Report on:";
            ws.Cell(1, 2).Value = date.ToString("yyyy-MM-dd");

            ws.Cell(3, 1).Value = "Date";
            ws.Cell(3, 2).Value = "Product";
            ws.Cell(3, 3).Value = "Category";
            ws.Cell(3, 4).Value = "Quantity";
            ws.Cell(3, 5).Value = "Comment";

            int row = 4;
            foreach (var t in data)
            {
                ws.Cell(row, 1).Value = t.Date;
                ws.Cell(row, 2).Value = t.Product?.Name;
                ws.Cell(row, 3).Value = t.Product?.Category?.Name;
                ws.Cell(row, 4).Value = t.Quantity;
                ws.Cell(row, 5).Value = t.Comment;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"StockReport_{date:yyyyMMdd}.xlsx");
        }


        // ======================================================
        // 📌 EXPORT: Incoming / Outgoing (спільний)
        // ======================================================
        public FileResult ExportGenericExcel(List<Transaction> data, string title)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet(title);

            ws.Cell(1, 1).Value = "Date";
            ws.Cell(1, 2).Value = "Product";
            ws.Cell(1, 3).Value = "Category";
            ws.Cell(1, 4).Value = "Qty";
            ws.Cell(1, 5).Value = "Comment";

            int row = 2;

            foreach (var t in data)
            {
                ws.Cell(row, 1).Value = t.Date;
                ws.Cell(row, 2).Value = t.Product?.Name;
                ws.Cell(row, 3).Value = t.Product?.Category?.Name;
                ws.Cell(row, 4).Value = t.Quantity;
                ws.Cell(row, 5).Value = t.Comment;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{title}.xlsx");
        }
    }
}
