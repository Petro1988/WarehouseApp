using Microsoft.AspNetCore.Mvc;
using WarehouseApp.Data;

namespace WarehouseApp.Controllers
{
    public class TestController : Controller
    {
        private readonly AppDbContext _db;
        public TestController(AppDbContext db) { _db = db; }
        public IActionResult Index()
        {
            var categories = _db.Categories.ToList();
            return Json(categories);
        }
    }
}
