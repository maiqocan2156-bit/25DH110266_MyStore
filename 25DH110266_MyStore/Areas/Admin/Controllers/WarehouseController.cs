
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using _25DH110266_MyStore.Areas.Admin.Filters;
using _25DH110266_MyStore.Models;

namespace _25DH110266_MyStore.Areas.Admin.Controllers
{
    [AdminSessionAuthorize]
    public class WarehouseController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Warehouse

        public ActionResult Index(string searchString, string filter)
        {
            var products = db.Products.Include(p => p.Category);

            // Thống kê tổng số sản phẩm
            ViewBag.TotalProducts = products.Count();
            ViewBag.InStock = products.Count(p => p.Quantity > 0);
            ViewBag.OutOfStock = products.Count(p => p.Quantity == 0);

            // Tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString));
            }

            // Lọc theo tình trạng tồn kho
            if (filter == "in")
            {
                products = products.Where(p => p.Quantity > 0);
            }
            else if (filter == "out")
            {
                products = products.Where(p => p.Quantity == 0);
            }

            var inStock = products
                .Where(p => p.Quantity > 0)
                .OrderBy(p => p.ProductName)
                .ToList();

            var outOfStock = products
                .Where(p => p.Quantity == 0)
                .OrderBy(p => p.ProductName)
                .ToList();

            ViewBag.SearchString = searchString;
            ViewBag.Filter = filter;

            return View(Tuple.Create(inStock, outOfStock));
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
