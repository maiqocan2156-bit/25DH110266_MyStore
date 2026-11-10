
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using _25DH110266_MyStore.Areas.Admin.Filters;
using _25DH110266_MyStore.Models;

namespace _25DH110266_MyStore.Areas.Admin.Controllers
{
    [AdminSessionAuthorize]
    public class AdminUsersController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/AdminUsers
        public ActionResult Index(string searchString, int page = 1)
        {
            const int pageSize = 5;

            if (page < 1)
                page = 1;

            var query = db.Users
                .Where(u => u.UserRole == "Admin");

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(
                    u => u.Username.Contains(searchString));
            }

            query = query.OrderBy(u => u.Username);

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var admins = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(admins);
        }

        // GET: Admin/AdminUsers/Details/admin
        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = FindAdmin(id);

            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // GET: Admin/AdminUsers/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/AdminUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include = "Username,Password")] User user)
        {
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Vui lòng nhập tên đăng nhập.");
            }
            else
            {
                user.Username = user.Username.Trim();

                if (db.Users.Any(u => u.Username == user.Username))
                {
                    ModelState.AddModelError(
                        "Username",
                        "Tên đăng nhập đã tồn tại.");
                }
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Vui lòng nhập mật khẩu.");
            }

            user.UserRole = "Admin";

            if (!ModelState.IsValid)
                return View(user);

            db.Users.Add(user);

            try
            {
                db.SaveChanges();

                TempData["Success"] =
                    "Thêm tài khoản Admin thành công.";

                return RedirectToAction("Index");
            }
            catch (DbEntityValidationException ex)
            {
                // Lay loi validation chi tiet tu Entity Framework
                var validationErrors = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => e.PropertyName + ": " + e.ErrorMessage);

                ModelState.AddModelError(
                    "",
                    "Dữ liệu không hợp lệ: " +
                    string.Join(" | ", validationErrors));

                return View(user);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể lưu tài khoản vào cơ sở dữ liệu. " +
                    "Có thể dữ liệu vi phạm ràng buộc database.");

                return View(user);
            }
        }

        // GET: Admin/AdminUsers/Edit/admin
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = FindAdmin(id);

            if (user == null)
                return HttpNotFound();

            // Khong dua mat khau hien tai ra form
            user.Password = null;

            return View(user);
        }

        // POST: Admin/AdminUsers/Edit/admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, string password)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = FindAdmin(id);

            if (user == null)
                return HttpNotFound();

            // De trong thi giu nguyen mat khau hien tai
            if (!string.IsNullOrWhiteSpace(password))
                user.Password = password;

            try
            {
                db.SaveChanges();

                TempData["Success"] =
                    "Cập nhật tài khoản Admin thành công.";

                return RedirectToAction("Index");
            }
            catch (DbEntityValidationException ex)
            {
                var errors = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => e.PropertyName + ": " + e.ErrorMessage);

                ModelState.AddModelError(
                    "",
                    "Dữ liệu không hợp lệ: " +
                    string.Join(" | ", errors));

                user.Password = null;
                return View(user);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật tài khoản.");

                user.Password = null;
                return View(user);
            }
        }

        // GET: Admin/AdminUsers/Delete/admin
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = FindAdmin(id);

            if (user == null)
                return HttpNotFound();

            string currentUsername =
                Convert.ToString(Session["Username"]);

            ViewBag.IsCurrentUser = string.Equals(
                id,
                currentUsername,
                StringComparison.OrdinalIgnoreCase);

            return View(user);
        }

        // POST: Admin/AdminUsers/Delete/admin
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string currentUsername =
                Convert.ToString(Session["Username"]);

            if (string.Equals(
                id,
                currentUsername,
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Không thể xóa tài khoản Admin đang đăng nhập.";

                return RedirectToAction("Index");
            }

            User user = FindAdmin(id);

            if (user == null)
                return HttpNotFound();

            // Khong xoa user neu dang duoc Customer tham chieu
            bool hasCustomers = db.Customers.Any(
                c => c.Username == id);

            if (hasCustomers)
            {
                TempData["Error"] =
                    "Không thể xóa tài khoản này vì đang được khách hàng sử dụng.";

                return RedirectToAction("Index");
            }

            db.Users.Remove(user);

            try
            {
                db.SaveChanges();

                TempData["Success"] =
                    "Xóa tài khoản Admin thành công.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể xóa tài khoản. Hãy kiểm tra các dữ liệu liên quan.";
            }

            return RedirectToAction("Index");
        }

        private User FindAdmin(string username)
        {
            return db.Users.FirstOrDefault(
                u => u.Username == username
                     && u.UserRole == "Admin");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
