
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using _25DH110266_MyStore.Models;

namespace _25DH110266_MyStore.Areas.Admin.Controllers
{
    public class CustomersController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Customers
        public ActionResult Index(string searchString, string sortOrder, int page = 1)
        {
            int pageSize = 5;

            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder)
                ? "name_desc"
                : "";
            ViewBag.CurrentFilter = searchString;

            var customers = db.Customers
                .Include(c => c.User)
                .AsQueryable();

            if (!String.IsNullOrWhiteSpace(searchString))
            {
                customers = customers.Where(c =>
                    c.CustomerName.Contains(searchString) ||
                    c.CustomerEmail.Contains(searchString) ||
                    c.CustomerPhone.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    customers = customers.OrderByDescending(c => c.CustomerName);
                    break;

                default:
                    customers = customers.OrderBy(c => c.CustomerName);
                    break;
            }

            int totalItems = customers.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0)
            {
                if (page < 1)
                    page = 1;

                if (page > totalPages)
                    page = totalPages;
            }
            else
            {
                page = 1;
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var result = customers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(result);
        }

        // GET: Admin/Customers/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Customer customer = db.Customers
                .Include(c => c.User)
                .FirstOrDefault(c => c.CustomerID == id);

            if (customer == null)
                return HttpNotFound();

            return View(customer);
        }

        // GET: Admin/Customers/Create
        public ActionResult Create()
        {
            LoadUsers();
            return View();
        }

        // POST: Admin/Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include = "CustomerID,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress,Username")]
            Customer customer)
        {
            // Username bat buoc phai co gia tri
            if (String.IsNullOrWhiteSpace(customer.Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Vui lòng chọn tài khoản cho khách hàng.");
            }
            else
            {
                // Dam bao tai khoan duoc chon ton tai trong bang Users
                bool userExists = db.Users.Any(
                    u => u.Username == customer.Username);

                if (!userExists)
                {
                    ModelState.AddModelError(
                        "Username",
                        "Tài khoản đã chọn không tồn tại. Vui lòng chọn lại.");
                }
            }

            if (ModelState.IsValid)
            {
                db.Customers.Add(customer);

                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException)
                {
                    ModelState.AddModelError(
                        "",
                        "Không thể lưu khách hàng. Vui lòng kiểm tra dữ liệu và tài khoản đã chọn.");
                }
            }

            LoadUsers(customer.Username);
            return View(customer);
        }

        // GET: Admin/Customers/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Customer customer = db.Customers.Find(id);

            if (customer == null)
                return HttpNotFound();

            LoadUsers(customer.Username);
            return View(customer);
        }

        // POST: Admin/Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include = "CustomerID,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress,Username")]
            Customer customer)
        {
            if (String.IsNullOrWhiteSpace(customer.Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Vui lòng chọn tài khoản cho khách hàng.");
            }
            else if (!db.Users.Any(u => u.Username == customer.Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Tài khoản đã chọn không tồn tại. Vui lòng chọn lại.");
            }

            if (ModelState.IsValid)
            {
                db.Entry(customer).State = EntityState.Modified;

                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException)
                {
                    ModelState.AddModelError(
                        "",
                        "Không thể cập nhật khách hàng. Vui lòng kiểm tra dữ liệu.");
                }
            }

            LoadUsers(customer.Username);
            return View(customer);
        }

        // GET: Admin/Customers/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Customer customer = db.Customers
                .Include(c => c.User)
                .FirstOrDefault(c => c.CustomerID == id);

            if (customer == null)
                return HttpNotFound();

            return View(customer);
        }

        // POST: Admin/Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Customer customer = db.Customers.Find(id);

            if (customer == null)
                return HttpNotFound();

            db.Customers.Remove(customer);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // Nap danh sach tai khoan vao dropdown
        private void LoadUsers(string selectedUsername = null)
        {
            var users = db.Users
                .OrderBy(u => u.Username)
                .ToList();

            ViewBag.Username = new SelectList(
                users,
                "Username",
                "Username",
                selectedUsername);

            ViewBag.HasUsers = users.Any();
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
