using System.Collections.Generic;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using _25DH110266_MyStore.Models;
using _25DH110266_MyStore.Models.ViewModel;

namespace _25DH110266_MyStore.Controllers
{
    public class CustomerAccountController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        // GET: CustomerAccount/Login
        [HttpGet]
        public ActionResult Login()
        {
            if (Session["CustomerID"] != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: CustomerAccount/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            username = username.Trim();

            var user = db.Users.FirstOrDefault(
                u => u.Username == username);

            if (user == null || user.Password == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            if (user.Password.StartsWith(
                "PBKDF2$", StringComparison.Ordinal))
            {
                if (!PasswordHasher.VerifyPassword(password, user.Password))
                {
                    ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    return View();
                }
            }
            else
            {
                if (!string.Equals(
                    user.Password,
                    password,
                    StringComparison.Ordinal))
                {
                    ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    return View();
                }

                user.Password = PasswordHasher.HashPassword(password);
                db.SaveChanges();
            }

            if (!string.Equals(
                (user.UserRole ?? "").Trim(),
                "Customer",
                StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Tài khoản không có quyền khách hàng.";
                return View();
            }

            var customer = db.Customers.FirstOrDefault(
                c => c.Username == user.Username);

            if (customer == null)
            {
                ViewBag.Error = "Không tìm thấy thông tin khách hàng.";
                return View();
            }

            Session.Clear();
            Session["Username"] = user.Username;
            Session["UserRole"] = user.UserRole;
            Session["CustomerID"] = customer.CustomerID;

            return RedirectToAction("Index", "Home");
        }

        // GET: CustomerAccount/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login");
        }

        // GET: CustomerAccount/Register
        [HttpGet]
        public ActionResult Register()
        {
            if (Session["CustomerID"] != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // GET: CustomerAccount/ProfileInfo
        public ActionResult ProfileInfo()
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login");
            }

            int customerId = (int)Session["CustomerID"];

            var customer = db.Customers.Find(customerId);

            if (customer == null)
            {
                return RedirectToAction("Login");
            }

            return View(customer);
        }

        // GET: CustomerAccount/UpdateProfile
        public ActionResult UpdateProfile()
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login");
            }

            int customerId = (int)Session["CustomerID"];
            var customer = db.Customers.Find(customerId);

            if (customer == null)
            {
                return RedirectToAction("Login");
            }

            return View(customer);
        }

        // POST: CustomerAccount/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(Customer customer)
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login");
            }

            int customerId = (int)Session["CustomerID"];
            var existingCustomer = db.Customers.Find(customerId);

            if (existingCustomer == null)
            {
                return RedirectToAction("Login");
            }

            existingCustomer.CustomerName = customer.CustomerName;
            existingCustomer.CustomerPhone = customer.CustomerPhone;
            existingCustomer.CustomerEmail = customer.CustomerEmail;
            existingCustomer.CustomerAddress = customer.CustomerAddress;

            db.SaveChanges();

            TempData["Success"] = "Cập nhật thông tin thành công!";

            return RedirectToAction("ProfileInfo");
        }

        // GET: CustomerAccount/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // POST: CustomerAccount/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordVM model)
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string username = Session["Username"].ToString();

            var user = db.Users.Find(username);

            if (user == null || user.Password == null)
            {
                return RedirectToAction("Login");
            }

            bool isValidPassword;

            if (user.Password.StartsWith("PBKDF2$", StringComparison.Ordinal))
            {
                isValidPassword = PasswordHasher.VerifyPassword(
                    model.CurrentPassword,
                    user.Password);
            }
            else
            {
                isValidPassword = string.Equals(
                    user.Password,
                    model.CurrentPassword,
                    StringComparison.Ordinal);
            }

            if (!isValidPassword)
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "Mật khẩu hiện tại không chính xác");

                return View(model);
            }

            user.Password = PasswordHasher.HashPassword(model.NewPassword);

            db.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công!";

            return RedirectToAction("ProfileInfo");
        }

        // POST: CustomerAccount/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Username = model.Username.Trim();

            // Kiểm tra tên đăng nhập đã tồn tại
            var existingUser = db.Users.FirstOrDefault(
                u => u.Username == model.Username);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập đã tồn tại.");

                return View(model);
            }

            // Tạo tài khoản User
            var user = new User
            {
                Username = model.Username,
                Password = PasswordHasher.HashPassword(model.Password),
                UserRole = "Customer"
            };

            db.Users.Add(user);

            // Tạo thông tin Customer
            var customer = new Customer
            {
                CustomerName = model.CustomerName.Trim(),
                CustomerPhone = model.CustomerPhone.Trim(),
                CustomerEmail = model.CustomerEmail,
                CustomerAddress = model.CustomerAddress,
                Username = model.Username
            };

            db.Customers.Add(customer);

            try
            {
                db.SaveChanges();
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Đăng ký không thành công. Vui lòng kiểm tra lại thông tin.");

                return View(model);
            }

            TempData["Success"] = "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";

            return RedirectToAction("Login");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }



        public ActionResult MyOrder(string searchTerm, string showMoreStatus, string activeTab = "all")
        {
            if (Session["CustomerID"] == null)
                return RedirectToAction("Login");

            int customerId = (int)Session["CustomerID"];

            var orders = db.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .Where(o => o.CustomerID == customerId)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                int orderId;
                bool isOrderId = int.TryParse(searchTerm, out orderId);

                orders = orders.Where(o =>
                    (isOrderId && o.OrderID == orderId) ||
                    o.OrderDetails.Any(d =>
                        d.Product != null &&
                        d.Product.ProductName.Contains(searchTerm)));
            }

            var tatCaDon = orders.ToList();

            var model = new MyOrderVM
            {
                SearchTerm = searchTerm,
                TatCaDon = tatCaDon,

                ChoThanhToan = tatCaDon
                    .Where(o => o.OrderStatus == "Chờ thanh toán")
                    .ToList(),

                DangXuLy = tatCaDon
                    .Where(o => o.OrderStatus == "Đang xử lý")
                    .ToList(),

                DangVanChuyen = tatCaDon
                    .Where(o => o.OrderStatus == "Đang vận chuyển")
                    .ToList(),

                DaGiao = tatCaDon
                    .Where(o => o.OrderStatus == "Đã giao")
                    .ToList(),

                DaHuy = tatCaDon
                    .Where(o => o.OrderStatus == "Đã hủy")
                    .ToList()
            };

            ViewBag.SearchTerm = searchTerm;
            ViewBag.ShowMoreStatus = showMoreStatus;
            ViewBag.ActiveTab = activeTab;

            return View(model);
        }



        // Mua lại sản phẩm từ đơn hàng cũ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reorder(int productId)
        {
            if (Session["CustomerID"] == null)
                return RedirectToAction("Login");

            var product = db.Products.Find(productId);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("MyOrder");
            }

            // Kiểm tra tồn kho
            if (product.Quantity <= 0)
            {
                TempData["Error"] = "Sản phẩm đã hết hàng.";
                return RedirectToAction("MyOrder");
            }

            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null)
                cart = new List<CartItem>();

            var item = cart.FirstOrDefault(
                x => x.ProductID == productId);

            if (item != null)
            {
                // Kiểm tra tổng số lượng sau khi thêm
                if (item.Quantity + 1 > product.Quantity)
                {
                    TempData["Error"] =
                        "Số lượng vượt quá tồn kho! Hiện chỉ còn "
                        + product.Quantity + " sản phẩm.";

                    return RedirectToAction("MyOrder");
                }

                item.Quantity += 1;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    ProductImage = product.ProductImage,
                    ProductPrice = product.ProductPrice,
                    Quantity = 1
                });
            }

            Session["Cart"] = cart;

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";

            return RedirectToAction("Index", "Cart");
        }

    }
}
