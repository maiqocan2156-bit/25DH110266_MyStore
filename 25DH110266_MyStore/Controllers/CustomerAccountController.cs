using _25DH110266_MyStore.Models.ViewModel;
using System;
using System.Linq;
using System.Web.Mvc;
using _25DH110266_MyStore.Models;

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

            if (user == null ||
                !string.Equals(
                    user.Password,
                    password,
                    StringComparison.Ordinal))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
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

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (user.Password != model.CurrentPassword)
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "Mật khẩu hiện tại không chính xác"
                );

                return View(model);
            }

            user.Password = model.NewPassword;

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
                Password = model.Password,
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
    }
}
