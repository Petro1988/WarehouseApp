using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;
using WarehouseApp.Models;

namespace WarehouseApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // 📊 MAIN DASHBOARD
        // =========================
        public async Task<IActionResult> Index(
            string? fromDate,
            string? toDate,
            string? type,
            int? categoryId)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // 🔎 DATE FILTER (STABLE)
            if (!string.IsNullOrWhiteSpace(fromDate) &&
                DateTime.TryParse(fromDate, out var from))
            {
                query = query.Where(t => t.Date.Date >= from.Date);
            }

            if (!string.IsNullOrWhiteSpace(toDate) &&
                DateTime.TryParse(toDate, out var to))
            {
                query = query.Where(t => t.Date.Date <= to.Date);
            }

            // 🔎 TYPE
            if (type == "IN" || type == "OUT")
                query = query.Where(t => t.TransactionType == type);

            // 🔎 CATEGORY
            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId);

            // 🧠 ViewBag (BACK TO VIEW)
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SelectedType = type;
            ViewBag.SelectedCategory = categoryId;

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Transactions = await query
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            DateTime? fromParsed = null;
            DateTime? toParsed = null;

            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var f))
                fromParsed = f;

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var t))
                toParsed = t;

            string periodText;

            if (fromParsed.HasValue && toParsed.HasValue)
            {
                periodText = $"Zeitraum: {fromParsed:dd.MM.yyyy} – {toParsed:dd.MM.yyyy}";
            }
            else if (fromParsed.HasValue)
            {
                periodText = $"Ab: {fromParsed:dd.MM.yyyy}";
            }
            else if (toParsed.HasValue)
            {
                periodText = $"Bis: {toParsed:dd.MM.yyyy}";
            }
            else
            {
                periodText = "Zeitraum: Alle Daten";
            }

            ViewBag.PeriodText = periodText;

            return View();
        }

        // =========================
        // 📈 LINE CHART
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetChartData(
            string? fromDate,
            string? toDate,
            string? type,
            int? categoryId)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(fromDate) &&
                DateTime.TryParse(fromDate, out var from))
            {
                query = query.Where(t => t.Date.Date >= from.Date);
            }

            if (!string.IsNullOrWhiteSpace(toDate) &&
                DateTime.TryParse(toDate, out var to))
            {
                query = query.Where(t => t.Date.Date <= to.Date);
            }

            if (type == "IN" || type == "OUT")
                query = query.Where(t => t.TransactionType == type);

            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId);

            var data = await query
                .GroupBy(t => t.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Incoming = g.Where(x => x.TransactionType == "IN").Sum(x => x.Quantity),
                    Outgoing = g.Where(x => x.TransactionType == "OUT").Sum(x => x.Quantity)
                })
                .OrderBy(g => g.Date)
                .ToListAsync();

            return Json(new
            {
                labels = data.Select(x => x.Date.ToString("yyyy-MM-dd")),
                incoming = data.Select(x => x.Incoming),
                outgoing = data.Select(x => x.Outgoing)
            });
        }

        // =========================
        // 🍩 CATEGORY CHART
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetCategoryChartData(
            string? fromDate,
            string? toDate,
            string? type,
            int? categoryId)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(fromDate) &&
                DateTime.TryParse(fromDate, out var from))
            {
                query = query.Where(t => t.Date.Date >= from.Date);
            }

            if (!string.IsNullOrWhiteSpace(toDate) &&
                DateTime.TryParse(toDate, out var to))
            {
                query = query.Where(t => t.Date.Date <= to.Date);
            }

            if (type == "IN" || type == "OUT")
                query = query.Where(t => t.TransactionType == type);

            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId);

            var data = await query
                .GroupBy(t => t.Product.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            return Json(new
            {
                labels = data.Select(x => x.Category),
                values = data.Select(x => x.Total)
            });
        }

        // =========================
        // 📤 EXCEL EXPORT
        // =========================
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(
            string? fromDate,
            string? toDate,
            string? type,
            int? categoryId)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(fromDate) &&
                DateTime.TryParse(fromDate, out var from))
            {
                query = query.Where(t => t.Date.Date >= from.Date);
            }

            if (!string.IsNullOrWhiteSpace(toDate) &&
                DateTime.TryParse(toDate, out var to))
            {
                query = query.Where(t => t.Date.Date <= to.Date);
            }

            if (type == "IN" || type == "OUT")
                query = query.Where(t => t.TransactionType == type);

            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId);

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Transactions");

            ws.Cell(1, 1).Value = "Datum";
            ws.Cell(1, 2).Value = "Artikel";
            ws.Cell(1, 3).Value = "Kategorie";
            ws.Cell(1, 4).Value = "Menge";
            ws.Cell(1, 5).Value = "Typ";

            int row = 2;
            foreach (var t in transactions)
            {
                ws.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd HH:mm");
                ws.Cell(row, 2).Value = t.Product?.Name;
                ws.Cell(row, 3).Value = t.Product?.Category?.Name;
                ws.Cell(row, 4).Value = t.Quantity;
                ws.Cell(row, 5).Value = t.TransactionType;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Warehouse_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            );
        }
    }
}
