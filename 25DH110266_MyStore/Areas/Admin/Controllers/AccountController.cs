
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using _25DH110266_MyStore.Models;

namespace _25DH110266_MyStore.Areas.Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        // Hiển thị trang đăng nhập
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login()
        {
            if (Session["Username"] != null &&
                string.Equals(
                    Convert.ToString(Session["UserRole"]).Trim(),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "Admin" });
            }

            return View();
        }

        // Tài khoản thử nghiệm: admin / 123456
        // Chỉ đăng nhập được nếu tài khoản này tồn tại trong database
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrEmpty(password))
            {
                ViewBag.Error =
                    "Vui lòng nhập tên đăng nhập và mật khẩu.";

                return View();
            }

            username = username.Trim();

            try
            {
                // Truy vấn tài khoản trong database
                var user = db.Users.FirstOrDefault(
                    u => u.Username == username);

                if (user == null)
                {
                    ViewBag.Error =
                        "Không tìm thấy tài khoản trong cơ sở dữ liệu.";

                    return View();
                }

                System.Diagnostics.Debug.WriteLine(
                    "USERNAME: [" + user.Username + "]");

                System.Diagnostics.Debug.WriteLine(
                    "USERROLE: [" + user.UserRole + "]");

                System.Diagnostics.Debug.WriteLine(
                    "ROLE LENGTH: " + (user.UserRole ?? "").Length);

                // Kiểm tra và tự động nâng cấp mật khẩu cũ
                if (user.Password == null)
                {
                    ViewBag.Error = "Mật khẩu không chính xác.";
                    return View();
                }

                if (user.Password.StartsWith(
                    "PBKDF2$", StringComparison.Ordinal))
                {
                    // Tài khoản đã mã hóa
                    if (!PasswordHasher.VerifyPassword(password, user.Password))
                    {
                        ViewBag.Error = "Mật khẩu không chính xác.";
                        return View();
                    }
                }
                else
                {
                    // Tài khoản đang dùng mật khẩu cũ
                    if (!string.Equals(
                        user.Password, password, StringComparison.Ordinal))
                    {
                        ViewBag.Error = "Mật khẩu không chính xác.";
                        return View();
                    }

                    // Mã hóa lại và lưu vào database
                    user.Password = PasswordHasher.HashPassword(password);
                    db.SaveChanges();
                }

                // Kiểm tra quyền quản trị
                if (!string.Equals(
                    (user.UserRole ?? "").Trim(),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error =
                        "Tài khoản này không có quyền quản trị.";

                    return View();
                }

                // Đăng nhập thành công
                Session.Clear();
                Session["Username"] = user.Username;
                Session["UserRole"] = user.UserRole;

                System.Diagnostics.Debug.WriteLine(
                    "LOGIN SUCCESS: " + Session["Username"]);

                System.Diagnostics.Debug.WriteLine(
                    "SESSION ROLE: [" + Session["UserRole"] + "]");

                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "Admin" });
            }
            catch (Exception ex)
            {
                // Ghi toàn bộ lỗi để xem trong Visual Studio
                System.Diagnostics.Debug.WriteLine(
                    "========== LOI DANG NHAP MYSTORE ==========");

                System.Diagnostics.Debug.WriteLine(ex.ToString());

                // Hiển thị lỗi chi tiết trong môi trường phát triển
                ViewBag.Error =
                    "Không thể truy vấn cơ sở dữ liệu: " +
                    ex.GetBaseException().Message;

                return View();
            }
        }

        // Đăng xuất
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();

            return RedirectToAction(
                "Login",
                "Account",
                new { area = "Admin" });
        }

        // Giải phóng tài nguyên
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
