using PagedList;
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
    public class OrdersController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Orders
        public ActionResult Index(
            string customerName,
            string paymentStatus,
            DateTime? orderDate,
            int? page)
        {
            var orders = db.Orders
                .Include(o => o.Customer)
                .AsQueryable();

            // Tìm theo tên khách hàng
            if (!string.IsNullOrEmpty(customerName))
            {
                orders = orders.Where(o =>
                    o.Customer.CustomerName.Contains(customerName));
            }

            // Tìm theo trạng thái thanh toán
            if (!string.IsNullOrEmpty(paymentStatus))
            {
                orders = orders.Where(o =>
                    o.PaymentStatus == paymentStatus);
            }

            // Tìm theo ngày đặt hàng
            if (orderDate.HasValue)
            {
                DateTime startDate = orderDate.Value;
                DateTime endDate = startDate.AddDays(1);

                orders = orders.Where(o =>
                    o.OrderDate >= startDate &&
                    o.OrderDate < endDate);
            }

            // Sắp xếp đơn hàng mới nhất trước
            orders = orders.OrderByDescending(o => o.OrderDate);

            // Phân trang 15 đơn hàng
            return View(orders.ToPagedList(page ?? 1, 15));
        }

        // GET: Admin/Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // GET: Admin/Orders/Create
        public ActionResult Create()
        {
            ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "CustomerName");
            return View();
        }

        // POST: Admin/Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OrderID,CustomerID,OrderDate,TotalAmount,PaymentStatus,AddressDelivery")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "CustomerName", order.CustomerID);
            return View(order);
        }

        // GET: Admin/Orders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "CustomerName", order.CustomerID);
            return View(order);
        }

        // POST: Admin/Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderID,CustomerID,OrderDate,TotalAmount,PaymentStatus,AddressDelivery")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "CustomerName", order.CustomerID);
            return View(order);
        }

        // GET: Admin/Orders/UpdateStatus/5
        public ActionResult UpdateStatus(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Order order = db.Orders.Find(id);

            if (order == null)
            {
                return HttpNotFound();
            }

            ViewBag.OrderStatuses = new SelectList(new[]
            {
                "Chờ thanh toán",
                "Đang xử lý",
                "Đang vận chuyển",
                "Đã giao",
                "Đã hủy"
            }, order.OrderStatus);

            return View(order);
        }

        // POST: Admin/Orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int OrderID, string OrderStatus)
        {
            if (OrderID <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var validStatuses = new[]
            {
                "Chờ thanh toán",
                "Đang xử lý",
                "Đang vận chuyển",
                "Đã giao",
                "Đã hủy"
            };

            if (!validStatuses.Contains(OrderStatus))
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest,
                    "Trạng thái đơn hàng không hợp lệ."
                );
            }

            using (var transaction = db.Database.BeginTransaction(
                System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    var order = db.Orders
                        .Include(o => o.OrderDetails.Select(d => d.Product))
                        .FirstOrDefault(o => o.OrderID == OrderID);

                    if (order == null)
                    {
                        return HttpNotFound();
                    }

                    // Không cho thay đổi trạng thái đơn hàng đã hủy
                    if (order.OrderStatus == "Đã hủy")
                    {
                        return new HttpStatusCodeResult(
                            HttpStatusCode.BadRequest,
                            "Đơn hàng đã hủy, không thể thay đổi trạng thái."
                        );
                    }

                    // Chỉ hoàn kho khi chuyển từ trạng thái khác sang Đã hủy
                    if (OrderStatus == "Đã hủy"
                        && order.OrderStatus != "Đã hủy")
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.Product == null)
                            {
                                throw new Exception(
                                    "Không tìm thấy sản phẩm trong đơn hàng."
                                );
                            }

                            detail.Product.Quantity += detail.Quantity;
                        }
                    }

                    order.OrderStatus = OrderStatus;

                    db.SaveChanges();
                    transaction.Commit();

                    return RedirectToAction("Details", new { id = OrderID });
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    TempData["Error"] =
                        "Không thể cập nhật trạng thái đơn hàng. Vui lòng thử lại.";

                    return RedirectToAction("Details", new { id = OrderID });
                }
            }
        }

        // GET: Admin/Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.Orders.Find(id);

            if (order == null)
            {
                return HttpNotFound();
            }

            db.Orders.Remove(order);
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
