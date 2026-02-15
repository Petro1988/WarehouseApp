using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
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


        // INDEX
        public async Task<IActionResult> Index()
        {
            return View();
        }


        // STOCK REPORT
        public async Task<IActionResult> StockReport(
            string? date,
            int? categoryId,
            int? productId,
            bool onlyBelowMin = false,
            bool export = false)
        {
            // Date for calculation
            DateTime calcDate;

            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsed))
            {
                calcDate = parsed.Date.AddDays(1).AddTicks(-1);
            }
            else
            {
                calcDate = DateTime.Today.AddDays(1).AddTicks(-1);
            }

            ViewBag.Date = date;

            // QUERY
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue && categoryId > 0)
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);

            if (productId.HasValue && productId > 0)
                productsQuery = productsQuery.Where(p => p.ProductId == productId);

            var products = await productsQuery
                .OrderBy(p => p.Name)
                .ToListAsync();

            // BALANCE
            foreach (var product in products)
            {
                var balance = await _context.Transactions
                    .Where(t => t.ProductId == product.ProductId && t.Date <= calcDate)
                    .SumAsync(t => t.TransactionType == "IN" ? t.Quantity : -t.Quantity);

                product.Quantity = balance;
            }

            // ONLY ITEMS BELOW MINIMUM
            if (onlyBelowMin)
            {
                products = products
                    .Where(p => p.MinimumStock > 0 && p.Quantity < p.MinimumStock)
                    .ToList();
            }

            // VIEW 
            ViewBag.PeriodText = $"Stand am {calcDate:dd.MM.yyyy}";
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedProduct = productId;
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            ViewBag.OnlyBelowMin = onlyBelowMin;

            if (export)
                return ExportStockExcel(products, calcDate);

            return View(products);
        }

        // INCOMING REPORT
        public async Task<IActionResult> IncomingReport(
             string? from,
             string? to,
             int? categoryId,
             int? productId,
             string? search,
             bool export = false,
             int page = 1,
             int pageSize = 1000)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .Where(t => t.TransactionType == "IN")
                .AsQueryable();

            var fromDate = ParseDate(from);
            var toDate = ParseDate(to);

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId);

            if (productId.HasValue && productId > 0)
                query = query.Where(t => t.ProductId == productId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.Comment != null &&
                    t.Comment.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var list = await query
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // BACK TO VIEW
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.PeriodText = BuildPeriodText(fromDate, toDate);
            ViewBag.Search = search;
            

            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedProduct = productId;
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();

            if (export)
                return ExportTransactionsExcel(list, "Incoming", fromDate, toDate);

            return View(list);
        }



        // OUTGOING REPORT
        public async Task<IActionResult> OutgoingReport(
            string? from,
            string? to,
            int? categoryId,
            int? productId,
            string? search,
            bool export = false,
            int page = 1,
            int pageSize = 1000)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .Where(t => t.TransactionType == "OUT")
                .AsQueryable();

            var fromDate = ParseDate(from);
            var toDate = ParseDate(to);

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            if (categoryId.HasValue)
                query = query.Where(t => t.Product.CategoryId == categoryId);

            if (productId.HasValue)
                query = query.Where(t => t.ProductId == productId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.Comment != null &&
                    t.Comment.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var list = await query
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.PeriodText = BuildPeriodText(fromDate, toDate);
            ViewBag.Search = search;


            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();


            if (export)
                return ExportTransactionsExcel(list, "Outgoing", fromDate, toDate);

            return View(list);
        }


        // HELPERS
        private string BuildPeriodText(DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue)
                return $"Zeitraum: {from:dd.MM.yyyy} – {to:dd.MM.yyyy}";
            if (from.HasValue)
                return $"Ab: {from:dd.MM.yyyy}";
            if (to.HasValue)
                return $"Bis: {to:dd.MM.yyyy}";
            return "Gesamter Zeitraum";
        }

        private FileResult ExportStockExcel(List<Product> data, DateTime date)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Stock");

            // HEADER
            ws.Cell(1, 1).Value = $"Stock am {date:dd.MM.yyyy}";
            ws.Cell(1, 1).Style.Font.Bold = true;

            ws.Cell(3, 1).Value = "Artikel";
            ws.Cell(3, 2).Value = "Kategorie";
            ws.Cell(3, 3).Value = "Menge";
            ws.Cell(3, 4).Value = "Mindestbestand";

            ws.Range("A3:D3").Style.Font.Bold = true;
            ws.Range("A3:D3").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // DATA
            int row = 4;
            foreach (var p in data)
            {
                ws.Cell(row, 1).Value = p.Name;
                ws.Cell(row, 2).Value = p.Category?.Name;
                ws.Cell(row, 3).Value = p.Quantity;
                ws.Cell(row, 4).Value = p.MinimumStock;

                //  highlight if below minimum
                if (p.MinimumStock > 0 && p.Quantity < p.MinimumStock)
                {
                    ws.Range(row, 1, row, 4)
                      .Style.Fill.BackgroundColor = XLColor.LightGoldenrodYellow;

                    ws.Cell(row, 3).Style.Font.FontColor = XLColor.DarkRed;
                }

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Stock_{date:yyyyMMdd}.xlsx"
            );
        }


        private FileResult ExportTransactionsExcel(
            List<Transaction> data,
            string name,
            DateTime? from,
            DateTime? to)
        {
            var today = DateTime.Today;

            string periodText;

            if (from.HasValue && to.HasValue)
                periodText = $"Von {from:dd.MM.yyyy} bis {to:dd.MM.yyyy}";
            else if (from.HasValue)
                periodText = $"Ab {from:dd.MM.yyyy}";
            else if (to.HasValue)
                periodText = $"Bis {to:dd.MM.yyyy}";
            else
                periodText = "Gesamter Zeitraum";

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet(name);

            ws.Cell(1, 1).Value = $"Export erstellt am {today:dd.MM.yyyy}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Value = periodText;
            ws.Cell(2, 1).Style.Font.Italic = true;

            ws.Cell(3, 1).Value = "Datum";
            ws.Cell(3, 2).Value = "Artikel";
            ws.Cell(3, 3).Value = "Kategorie";
            ws.Cell(3, 4).Value = "Menge";

            int row = 4;
            foreach (var t in data)
            {
                ws.Cell(row, 1).Value = t.Date.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 2).Value = t.Product?.Name;
                ws.Cell(row, 3).Value = t.Product?.Category?.Name;
                ws.Cell(row, 4).Value = t.Quantity;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{name}_Export_{today:yyyyMMdd}.xlsx"
            );
        }

        private DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var formats = new[]
            {
                "yyyy-MM-dd", // HTML date (ISO)
                "dd.MM.yyyy"  // German / UA format
            };

            if (DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
            {
                return result;
            }

            return null;
        }

    }
}
