using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using WarehouseApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace WarehouseApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? type, int? categoryId)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // ✅ Фільтрація за датами
            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value);

            // ✅ Фільтрація за типом (IN / OUT)
            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.TransactionType == type);

            // ✅ Фільтрація за категорією
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId.Value);

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Take(50)
                .ToListAsync();

            // Для вибору в UI
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedType = type;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Transactions = transactions;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Filter(DateTime? fromDate, DateTime? toDate, string? type, int? categoryId)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.TransactionType == type);
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId.Value);

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Take(50)
                .ToListAsync();

            return PartialView("_TransactionTable", transactions);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(DateTime? fromDate, DateTime? toDate, string? type, int? categoryId)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.TransactionType == type);
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId.Value);

            // Групування по даті
            var grouped = await query
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
                labels = grouped.Select(g => g.Date.ToString("yyyy-MM-dd")),
                incoming = grouped.Select(g => g.Incoming),
                outgoing = grouped.Select(g => g.Outgoing)
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryChartData(DateTime? fromDate, DateTime? toDate, string? type, int? categoryId)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.TransactionType == type);
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId.Value);

            var grouped = await query
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
                labels = grouped.Select(g => g.Category),
                values = grouped.Select(g => g.Total)
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(DateTime? fromDate, DateTime? toDate, string? type, int? categoryId)
        {
            var query = _context.Transactions
                .Include(t => t.Product)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // 📅 Фільтри
            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.Date <= toDate.Value);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.TransactionType == type);
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(t => t.Product.CategoryId == categoryId.Value);

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            // 🧾 Створюємо Excel-файл
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Transactions");

            // 🔹 Заголовки
            worksheet.Cell(1, 1).Value = "Datum";
            worksheet.Cell(1, 2).Value = "Artikel";
            worksheet.Cell(1, 3).Value = "Kategorie";
            worksheet.Cell(1, 4).Value = "Menge";
            worksheet.Cell(1, 5).Value = "Typ";

            // 🔹 Дані
            int row = 2;
            foreach (var t in transactions)
            {
                worksheet.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 2).Value = t.Product?.Name;
                worksheet.Cell(row, 3).Value = t.Product?.Category?.Name;
                worksheet.Cell(row, 4).Value = t.Quantity;
                worksheet.Cell(row, 5).Value = t.TransactionType;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            // 📤 Віддаємо як файл
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            string fileName = $"Warehouse_Transactions_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}

