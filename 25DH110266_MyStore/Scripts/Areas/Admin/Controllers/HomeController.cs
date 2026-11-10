
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using _25DH110266_MyStore.Models;

namespace _25DH110266_MyStore.Areas.Admin.Controllers
{
    public class HomeController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Home
        public ActionResult Index()
        {
            ViewBag.TotalProducts = db.Products.Count();
            ViewBag.TotalCategories = db.Categories.Count();
            ViewBag.TotalCustomers = db.Customers.Count();
            ViewBag.TotalOrders = db.Orders.Count();

            // Lay du lieu san pham kem danh muc
            var products = db.Products
                .Include(p => p.Category)
                .ToList();

            // Thong ke so luong va gia theo tung danh muc
            var statistics = products
                .GroupBy(p => new
                {
                    p.CategoryID,
                    CategoryName = p.Category != null
                        ? p.Category.CategoryName
                        : "Chưa phân loại"
                })
                .Select(group => new CategoryProductStatisticViewModel
                {
                    CategoryName = group.Key.CategoryName,
                    ProductCount = group.Count(),
                    MaxPrice = group.Max(p => p.ProductPrice),
                    MinPrice = group.Min(p => p.ProductPrice),
                    AveragePrice = group.Average(p => p.ProductPrice)
                })
                .OrderBy(s => s.CategoryName)
                .ToList();

            return View(statistics);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
