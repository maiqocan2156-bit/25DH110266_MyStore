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
        public ActionResult Index(string searchTearm, string sortOrder, int? page)
        {
            using (var db = new MyStoreEntities())
            {
                // Chỉ lấy sản phẩm còn hàng
                // Chỉ lấy sản phẩm còn hàng
                var products = db.Products
                    .Where(p => p.Quantity > 0)
                    .AsQueryable();

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
                var newProductsQuery = products
                    .Select(p => new
                    {
                        Product = p,
                        TotalSold = p.OrderDetails.Sum(od => (int?)od.Quantity) ?? 0
                    })
                    .OrderBy(p => p.TotalSold)
                    .Take(20)
                    .Select(p => p.Product);

                switch (sortOrder)
                {
                    case "price_asc":
                        newProductsQuery = newProductsQuery.OrderBy(p => p.ProductPrice);
                        break;

                    case "price_desc":
                        newProductsQuery = newProductsQuery.OrderByDescending(p => p.ProductPrice);
                        break;
                }

                var newProducts = newProductsQuery.ToList();

                ViewBag.SearchTerm = searchTearm;
                ViewBag.SortOrder = sortOrder;

                // Tạo ViewModel
                var model = new HomeProductVM
                {
                    FeaturedProducts = featuredProducts,
                    NewProducts = newProducts.ToPagedList(page ?? 1, 6)
                };

                return View(model);
            }
        }



        public ActionResult ProductList(int? id, string sortOrder, string searchTerm, int? page)
        {
            using (var db = new MyStoreEntities())
            {
                if (id == null)
                {
                    return RedirectToAction("Index");
                }

                var products = db.Products
                    .Where(p => p.CategoryID == id && p.Quantity > 0);

                // Tìm kiếm theo tên hoặc mô tả sản phẩm
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    products = products.Where(p =>
                        p.ProductName.Contains(searchTerm) ||
                        p.ProductDescription.Contains(searchTerm));
                }

                // Sắp xếp sản phẩm theo giá
                switch (sortOrder)
                {
                    case "price_asc":
                        products = products.OrderBy(p => p.ProductPrice);
                        break;

                    case "price_desc":
                        products = products.OrderByDescending(p => p.ProductPrice);
                        break;

                    default:
                        products = products.OrderBy(p => p.ProductID);
                        break;
                }

                ViewBag.CategoryName = db.Categories
                    .Where(c => c.CategoryID == id)
                    .Select(c => c.CategoryName)
                    .FirstOrDefault();

                ViewBag.CategoryID = id;
                ViewBag.SortOrder = sortOrder;
                ViewBag.SearchTerm = searchTerm;

                return View(products.ToPagedList(page ?? 1, 6));
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

            // Kiểm tra số lượng nhập vào
            if (quantity < 1)
            {
                TempData["CartMessage"] = "Số lượng phải lớn hơn hoặc bằng 1!";
                return RedirectToAction("ProductDetail", new { id = id });
            }

            using (var db = new MyStoreEntities())
            {
                var product = db.Products.Find(id);

                if (product == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra sản phẩm còn hàng không
                if (product.Quantity <= 0)
                {
                    TempData["CartMessage"] = "Sản phẩm đã hết hàng!";
                    return RedirectToAction("ProductDetail", new { id = id });
                }

                // Lấy giỏ hàng từ Session
                var cart = Session["Cart"] as List<CartItem>;

                if (cart == null)
                {
                    cart = new List<CartItem>();
                }

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var item = cart.FirstOrDefault(x => x.ProductID == id);

                // Tính tổng số lượng sau khi thêm
                int currentQuantity = item != null ? item.Quantity : 0;
                int totalQuantity = currentQuantity + quantity;

                // Không cho phép vượt quá số lượng tồn kho
                if (totalQuantity > product.Quantity)
                {
                    TempData["CartMessage"] =
                        "Số lượng vượt quá tồn kho! Hiện chỉ còn "
                        + product.Quantity + " sản phẩm.";

                    return RedirectToAction("ProductDetail", new { id = id });
                }

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

            TempData["CartMessage"] = "Đã thêm sản phẩm vào giỏ hàng!";

            return RedirectToAction("ProductDetail", new { id = id });
        }

        // Hiển thị danh mục sản phẩm trên menu Customer
        public PartialViewResult CategoryMenu()
        {
            using (var db = new MyStoreEntities())
            {
                var categories = db.Categories
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                return PartialView(
                    "~/Views/Shared/_CategoryMenu.cshtml",
                    categories
                );
            }
        }
    }
}
