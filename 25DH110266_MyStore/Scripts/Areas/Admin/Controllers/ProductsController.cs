
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using _25DH110266_MyStore.Models;

namespace _25DH110266_MyStore.Areas.Admin.Controllers
{
    public class ProductsController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Products
        public ActionResult Index(
            string searchString,
            string sortOrder,
            decimal? minPrice,
            decimal? maxPrice,
            int page = 1)
        {
            int pageSize = 5;

            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm =
                String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.PriceSortParm =
                sortOrder == "price_asc" ? "price_desc" : "price_asc";

            ViewBag.CurrentFilter = searchString;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            var products = db.Products
                .Include(p => p.Category)
                .AsQueryable();

            // Tìm theo tên sản phẩm, mô tả hoặc tên danh mục
            if (!String.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                products = products.Where(p =>
                    (p.ProductName != null &&
                     p.ProductName.Contains(searchString))
                    ||
                    (p.ProductDescription != null &&
                     p.ProductDescription.Contains(searchString))
                    ||
                    (p.Category != null &&
                     p.Category.CategoryName.Contains(searchString))
                );
            }

            // Kiểm tra khoảng giá
            bool validPriceRange =
                !minPrice.HasValue ||
                !maxPrice.HasValue ||
                minPrice.Value <= maxPrice.Value;

            if (!validPriceRange)
            {
                ModelState.AddModelError(
                    "",
                    "Giá tối thiểu không được lớn hơn giá tối đa."
                );
            }
            else
            {
                if (minPrice.HasValue)
                {
                    products = products.Where(
                        p => p.ProductPrice >= minPrice.Value
                    );
                }

                if (maxPrice.HasValue)
                {
                    products = products.Where(
                        p => p.ProductPrice <= maxPrice.Value
                    );
                }
            }

            // Sắp xếp kết quả
            switch (sortOrder)
            {
                case "name_desc":
                    products = products.OrderByDescending(
                        p => p.ProductName
                    );
                    break;

                case "price_asc":
                    products = products.OrderBy(
                        p => p.ProductPrice
                    );
                    break;

                case "price_desc":
                    products = products.OrderByDescending(
                        p => p.ProductPrice
                    );
                    break;

                default:
                    products = products.OrderBy(
                        p => p.ProductName
                    );
                    break;
            }

            // Phân trang
            int totalItems = products.Count();
            int totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize
            );

            if (totalPages > 0)
            {
                if (page < 1)
                {
                    page = 1;
                }

                if (page > totalPages)
                {
                    page = totalPages;
                }
            }
            else
            {
                page = 1;
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var result = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(result);
        }

        // GET: Admin/Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(
                db.Categories,
                "CategoryID",
                "CategoryName"
            );

            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include =
                "ProductID,CategoryID,ProductName,ProductPrice,ProductDescription")]
            Product product,
            HttpPostedFileBase uploadImage)
        {
            if (ModelState.IsValid)
            {
                if (uploadImage == null ||
                    uploadImage.ContentLength <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Vui lòng chọn hình ảnh sản phẩm."
                    );
                }
                else
                {
                    string extension =
                        Path.GetExtension(uploadImage.FileName);

                    string[] allowedExtensions =
                    {
                        ".jpg", ".jpeg", ".png", ".gif", ".webp"
                    };

                    if (String.IsNullOrEmpty(extension) ||
                        !allowedExtensions.Contains(
                            extension.ToLowerInvariant()))
                    {
                        ModelState.AddModelError(
                            "",
                            "Chỉ chấp nhận ảnh JPG, JPEG, PNG, GIF hoặc WEBP."
                        );
                    }
                    else
                    {
                        string folderPath =
                            Server.MapPath("~/Content/images");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string fileName =
                            Guid.NewGuid().ToString("N") +
                            extension.ToLowerInvariant();

                        string filePath =
                            Path.Combine(folderPath, fileName);

                        uploadImage.SaveAs(filePath);

                        product.ProductImage = fileName;

                        db.Products.Add(product);
                        db.SaveChanges();

                        return RedirectToAction("Index");
                    }
                }
            }

            ViewBag.CategoryID = new SelectList(
                db.Categories,
                "CategoryID",
                "CategoryName",
                product.CategoryID
            );

            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.CategoryID = new SelectList(
                db.Categories,
                "CategoryID",
                "CategoryName",
                product.CategoryID
            );

            return View(product);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include =
                "ProductID,CategoryID,ProductName,ProductPrice,ProductDescription")]
            Product product,
            HttpPostedFileBase uploadImage)
        {
            if (ModelState.IsValid)
            {
                Product existingProduct =
                    db.Products.Find(product.ProductID);

                if (existingProduct == null)
                {
                    return HttpNotFound();
                }

                existingProduct.CategoryID = product.CategoryID;
                existingProduct.ProductName = product.ProductName;
                existingProduct.ProductPrice = product.ProductPrice;
                existingProduct.ProductDescription =
                    product.ProductDescription;

                if (uploadImage != null &&
                    uploadImage.ContentLength > 0)
                {
                    string extension =
                        Path.GetExtension(uploadImage.FileName);

                    string[] allowedExtensions =
                    {
                        ".jpg", ".jpeg", ".png", ".gif", ".webp"
                    };

                    if (String.IsNullOrEmpty(extension) ||
                        !allowedExtensions.Contains(
                            extension.ToLowerInvariant()))
                    {
                        ModelState.AddModelError(
                            "",
                            "Chỉ chấp nhận ảnh JPG, JPEG, PNG, GIF hoặc WEBP."
                        );
                    }
                    else
                    {
                        string folderPath =
                            Server.MapPath("~/Content/images");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        string fileName =
                            Guid.NewGuid().ToString("N") +
                            extension.ToLowerInvariant();

                        string filePath =
                            Path.Combine(folderPath, fileName);

                        uploadImage.SaveAs(filePath);

                        existingProduct.ProductImage = fileName;
                    }
                }

                if (ModelState.IsValid)
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.CategoryID = new SelectList(
                db.Categories,
                "CategoryID",
                "CategoryName",
                product.CategoryID
            );

            return View(product);
        }

        // GET: Admin/Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            db.Products.Remove(product);
            db.SaveChanges();

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
