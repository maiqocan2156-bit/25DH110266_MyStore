using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using _25DH110266_MyStore.Models;
using _25DH110266_MyStore.Areas.Admin.Filters;

namespace _25DH110266_MyStore.Areas.Admin.Controllers
{
    [AdminSessionAuthorize]
    public class CategoriesController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Categories
        // GET: lấy dữ liệu category từ database và hiển thị lên
        public ActionResult Index()
        {
            var categories = db.Categories
                .Include(c => c.Products)
                .ToList();

            return View(categories);
        }

        // GET: Admin/Categories/Details/5
        // Details: lấy chi tiết một bản ghi có CategoryID = id
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Category category = db.Categories
                .Include(c => c.Products)
                .FirstOrDefault(c => c.CategoryID == id);

            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // GET: Admin/Categories/Create
        //[HttpGet] phương thức mặc định, ko cần khai báo từ khóa
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CategoryID,CategoryName")] Category category)
        {
            if (ModelState.IsValid)
            {
                db.Categories.Add(category);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CategoryID,CategoryName")] Category category)
        {
            if (ModelState.IsValid)
            {
                db.Entry(category).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // GET: Admin/Categories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Category category = db.Categories.Find(id);

            if (category == null)
            {
                return HttpNotFound();
            }

            // Dem so san pham thuoc danh muc
            ViewBag.ProductCount = db.Products.Count(
                p => p.CategoryID == id);

            return View(category);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Category category = db.Categories.Find(id);

            if (category == null)
            {
                return HttpNotFound();
            }

            // Kiem tra san pham dang thuoc danh muc
            bool hasProducts = db.Products.Any(
                p => p.CategoryID == id);

            if (hasProducts)
            {
                TempData["DeleteError"] =
                    "Không thể xóa danh mục vì vẫn còn sản phẩm thuộc danh mục này. Vui lòng chuyển hoặc xóa các sản phẩm liên quan trước.";

                return RedirectToAction("Delete", new { id = id });
            }

            db.Categories.Remove(category);
            db.SaveChanges();

            TempData["DeleteSuccess"] = "Xóa danh mục thành công.";

            return RedirectToAction("Index");
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
