using System.Collections.Generic;
using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using _25DH110266_MyStore.Models;
using _25DH110266_MyStore.Models.ViewModel;
using PagedList;

namespace _25DH110266_MyStore.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string searchTearm, int? page)
        {
            using (var db = new MyStoreEntities())
            {
                // Lấy danh sách sản phẩm
                var products = db.Products.AsQueryable();

                // Tìm kiếm theo tên, mô tả hoặc danh mục
                if (!string.IsNullOrEmpty(searchTearm))
                {
                    products = products.Where(p =>
                        p.ProductName.Contains(searchTearm) ||
                        p.ProductDescription.Contains(searchTearm) ||
                        p.Category.CategoryName.Contains(searchTearm));
                }

                // Sản phẩm nổi bật: 10 sản phẩm bán chạy nhất
                var featuredProducts = products
                    .Select(p => new
                    {
                        Product = p,
                        TotalSold = p.OrderDetails.Sum(od => (int?)od.Quantity) ?? 0
                    })
                    .OrderByDescending(p => p.TotalSold)
                    .Take(10)
                    .Select(p => p.Product)
                    .ToList();

                // Danh sách 20 sản phẩm ít người mua nhất
                var newProducts = products
                    .Select(p => new
                    {
                        Product = p,
                        TotalSold = p.OrderDetails.Sum(od => (int?)od.Quantity) ?? 0
                    })
                    .OrderBy(p => p.TotalSold)
                    .Take(20)
                    .Select(p => p.Product)
                    .ToList();

                // Tạo ViewModel
                var model = new HomeProductVM
                {
                    FeaturedProducts = featuredProducts,
                    NewProducts = newProducts.ToPagedList(page ?? 1, 6)
                };

                return View(model);
            }
        }

        public ActionResult ProductList(int? id)
        {
            using (var db = new MyStoreEntities())
            {
                if (id == null)
                {
                    return RedirectToAction("Index");
                }

                var products = db.Products
                    .Where(p => p.CategoryID == id)
                    .ToList();

                ViewBag.CategoryName = db.Categories
                    .Where(c => c.CategoryID == id)
                    .Select(c => c.CategoryName)
                    .FirstOrDefault();

                return View(products);
            }
        }

        public ActionResult ProductDetail(int? id, int? page)
        {
            using (var db = new MyStoreEntities())
            {
                if (id == null)
                {
                    return RedirectToAction("Index");
                }

                var product = db.Products
                    .Include("Category")
                    .Include("OrderDetails")
                    .FirstOrDefault(p => p.ProductID == id);

                if (product == null)
                {
                    return HttpNotFound();
                }

                // Lấy tối đa 8 sản phẩm cùng danh mục, không bao gồm sản phẩm hiện tại
                var relatedProducts = db.Products
                    .Where(p => p.CategoryID == product.CategoryID
                             && p.ProductID != product.ProductID)
                    .Take(8)
                    .ToList();

                // Lấy sản phẩm bán chạy nhất trong cùng danh mục
                var topProducts = db.Products
                    .Where(p => p.CategoryID == product.CategoryID
                             && p.ProductID != product.ProductID)
                    .Select(p => new
                    {
                        Product = p,
                        TotalSold = p.OrderDetails.Sum(od => (int?)od.Quantity) ?? 0
                    })
                    .OrderByDescending(p => p.TotalSold)
                    .Select(p => p.Product)
                    .ToList();

                var model = new ProductDetailVM
                {
                    Product = product,
                    RelatedProducts = relatedProducts,
                    TopProducts = topProducts,
                    Page = page ?? 1,
                    TotalPages = (int)Math.Ceiling(topProducts.Count / 3.0)
                };

                return View(model);
            }
        }

        [HttpPost]
        public ActionResult AddToCart(int id, int quantity)
        {
            // Kiểm tra đăng nhập khách hàng
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "CustomerAccount");
            }

            if (quantity < 1)
            {
                return RedirectToAction(
                    "ProductDetail",
                    new { id = id });
            }

            using (var db = new MyStoreEntities())
            {
                var product = db.Products.Find(id);

                if (product == null)
                {
                    return HttpNotFound();
                }

                // Lấy giỏ hàng từ Session
                var cart = Session["Cart"] as List<CartItem>;

                if (cart == null)
                {
                    cart = new List<CartItem>();
                }

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var item = cart.FirstOrDefault(
                    x => x.ProductID == id);

                if (item != null)
                {
                    item.Quantity += quantity;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName,
                        ProductImage = product.ProductImage,
                        ProductPrice = product.ProductPrice,
                        Quantity = quantity
                    });
                }

                // Lưu giỏ hàng vào Session
                Session["Cart"] = cart;
            }

            TempData["CartMessage"] =
                "Đã thêm sản phẩm vào giỏ hàng!";

            return RedirectToAction(
                "ProductDetail",
                new { id = id });
        }
    }
}
