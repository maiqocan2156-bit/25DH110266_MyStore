using System;
using _25DH110266_MyStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using _25DH110266_MyStore.Models.ViewModel;
using System.Data.Entity.Validation;
using System.Data.Entity;

namespace _25DH110266_MyStore.Controllers
{
    public class CartController : Controller
    {
        // Hiển thị giỏ hàng
        public ActionResult Index()
        {
            // Kiểm tra đăng nhập khách hàng
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "CustomerAccount");
            }

            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null)
            {
                cart = new List<CartItem>();
            }

            return View(cart);
        }

        // Xóa một sản phẩm khỏi giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(int id)
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "CustomerAccount");
            }

            var cart = Session["Cart"] as List<CartItem>;

            if (cart != null)
            {
                cart.RemoveAll(x => x.ProductID == id);
                Session["Cart"] = cart;
            }

            return RedirectToAction("Index");
        }


        // Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int id, int quantity)
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "CustomerAccount");
            }

            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null)
            {
                return RedirectToAction("Index");
            }

            var item = cart.FirstOrDefault(x => x.ProductID == id);

            if (item != null)
            {
                if (quantity < 1)
                {
                    TempData["Error"] = "Số lượng phải lớn hơn 0.";
                }
                else
                {
                    using (var db = new MyStoreEntities())
                    {
                        var product = db.Products.Find(id);

                        if (product == null)
                        {
                            TempData["Error"] = "Không tìm thấy sản phẩm.";
                        }
                        else if (quantity > product.Quantity)
                        {
                            TempData["Error"] =
                                "Số lượng vượt quá tồn kho! Hiện chỉ còn "
                                + product.Quantity + " sản phẩm.";
                        }
                        else
                        {
                            item.Quantity = quantity;
                            Session["Cart"] = cart;
                            TempData["Success"] = "Cập nhật số lượng thành công.";
                        }
                    }
                }
            }

            return RedirectToAction("Index");
        }

        // Xóa toàn bộ giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Clear()
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "CustomerAccount");
            }

            Session["Cart"] = new List<CartItem>();

            return RedirectToAction("Index");
        }

        // Thanh toán các sản phẩm được chọn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(
            int[] selectedProductIds,
            string deliveryMethod,
            string paymentMethod,
            string addressDelivery,
            string bankName = null,
            string onlineMethod = null)
        {
            // Kiểm tra đăng nhập
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "CustomerAccount");
            }

            // Kiểm tra sản phẩm được chọn
            if (selectedProductIds == null ||
                selectedProductIds.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn sản phẩm cần thanh toán.";
                return RedirectToAction("Index");
            }

            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null || cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }

            // Loại bỏ ID trùng lặp
            var selectedIds = selectedProductIds.Distinct().ToList();

            // Chỉ cho phép thanh toán sản phẩm thực sự có trong giỏ
            var selectedItems = cart
                .Where(x => selectedIds.Contains(x.ProductID))
                .ToList();

            if (selectedItems.Count != selectedIds.Count)
            {
                TempData["Error"] = "Sản phẩm được chọn không hợp lệ.";
                return RedirectToAction("Index");
            }

            // Nếu chọn chuyển khoản ngân hàng
            // thì chuyển sang trang chọn ngân hàng
            if (paymentMethod == "Chuyển khoản ngân hàng"
                && string.IsNullOrWhiteSpace(bankName))
            {
                ViewBag.SelectedProductIds = selectedIds;
                ViewBag.DeliveryMethod = deliveryMethod;
                ViewBag.AddressDelivery = addressDelivery;

                return View("SelectBank", selectedItems);
            }

            // Nếu chọn thanh toán trực tuyến
            if (paymentMethod == "Thanh toán trực tuyến"
                && string.IsNullOrWhiteSpace(onlineMethod))
            {
                ViewBag.SelectedProductIds = selectedIds;
                ViewBag.DeliveryMethod = deliveryMethod;
                ViewBag.AddressDelivery = addressDelivery;

                return View("OnlinePayment");
            }

            int customerId = (int)Session["CustomerID"];

            using (var db = new MyStoreEntities())
            using (var transaction = db.Database.BeginTransaction(
                System.Data.IsolationLevel.Serializable))
            {
                // Lấy thông tin khách hàng
                var customer = db.Customers.Find(customerId);

                if (customer == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin khách hàng.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra số lượng tồn kho trước khi đặt hàng
                foreach (var item in selectedItems)
                {
                    var product = db.Products.Find(item.ProductID);

                    if (product == null)
                    {
                        TempData["Error"] = "Không tìm thấy sản phẩm.";
                        return RedirectToAction("Index");
                    }

                    if (item.Quantity < 1 || product.Quantity < item.Quantity)
                    {
                        TempData["Error"] =
                            "Sản phẩm " + product.ProductName +
                            " không đủ số lượng tồn kho.";

                        return RedirectToAction("Index");
                    }
                }

                // Tạo đơn hàng
                var order = new Order
                {
                    CustomerID = customerId,
                    OrderDate = DateTime.Now,
                    TotalAmount = selectedItems.Sum(x => x.Total),

                    PaymentStatus = "Chưa thanh toán",
                    OrderStatus = "Chờ thanh toán",

                    AddressDelivery = string.IsNullOrEmpty(addressDelivery)
                        ? (string.IsNullOrEmpty(customer.CustomerAddress)
                            ? "Chưa cập nhật địa chỉ"
                            : customer.CustomerAddress)
                        : addressDelivery,

                    DeliveryMethod = deliveryMethod,

                    PaymentMethod = !string.IsNullOrWhiteSpace(bankName)
                        ? paymentMethod + " - " + bankName
                        : !string.IsNullOrWhiteSpace(onlineMethod)
                            ? paymentMethod + " - " + onlineMethod
                            : paymentMethod
                };

                db.Orders.Add(order);

                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    var errors = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.PropertyName + ": " + x.ErrorMessage);

                    throw new Exception(
                        string.Join("\n", errors), ex);
                }

                // Lưu chi tiết đơn hàng và trừ số lượng tồn kho
                foreach (var item in selectedItems)
                {
                    var product = db.Products.Find(item.ProductID);

                    if (product == null || product.Quantity < item.Quantity)
                    {
                        throw new Exception(
                            "Số lượng tồn kho không đủ để hoàn tất đơn hàng.");
                    }

                    var detail = new OrderDetail
                    {
                        OrderID = order.OrderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.ProductPrice,
                        TotalPrice = item.Quantity * item.ProductPrice
                    };

                    db.OrderDetails.Add(detail);

                    // Trừ số lượng tồn kho
                    product.Quantity -= item.Quantity;
                }

                db.SaveChanges();

                // Xác nhận giao dịch khi lưu đơn hàng và trừ kho thành công
                transaction.Commit();

                // Chỉ xóa những sản phẩm đã thanh toán
                cart.RemoveAll(x => selectedIds.Contains(x.ProductID));
                Session["Cart"] = cart;

                // Lưu mã đơn hàng để hiển thị ở trang thành công
                TempData["OrderID"] = order.OrderID;
            }

            return RedirectToAction("OrderSuccess");
        }

        // Hiển thị trang đặt hàng thành công
        public ActionResult OrderSuccess()
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "CustomerAccount");
            }

            if (TempData["OrderID"] == null)
            {
                return RedirectToAction("Index");
            }

            int orderId = (int)TempData["OrderID"];
            int customerId = (int)Session["CustomerID"];

            using (var db = new MyStoreEntities())
            {
                var order = db.Orders.FirstOrDefault(x =>
                    x.OrderID == orderId &&
                    x.CustomerID == customerId);

                if (order == null)
                {
                    return RedirectToAction("Index");
                }

                return View(order);
            }
        }

        // Hiển thị lịch sử đơn hàng của khách hàng
        public ActionResult OrderHistory()
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "CustomerAccount");
            }

            int customerId = (int)Session["CustomerID"];

            using (var db = new MyStoreEntities())
            {
                var orders = db.Orders
                    .Where(x => x.CustomerID == customerId)
                    .OrderByDescending(x => x.OrderDate)
                    .ToList();

                return View(orders);
            }
        }

        // Xem chi tiết đơn hàng
        public ActionResult OrderDetail(int id)
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "CustomerAccount");
            }

            int customerId = (int)Session["CustomerID"];

            using (var db = new MyStoreEntities())
            {
                // Kiểm tra đơn hàng có thuộc khách hàng hiện tại không
                var order = db.Orders
                    .FirstOrDefault(x =>
                        x.OrderID == id &&
                        x.CustomerID == customerId);

                if (order == null)
                {
                    return RedirectToAction("OrderHistory");
                }

                // Lấy danh sách sản phẩm trong đơn hàng
                var details = db.OrderDetails
                    .Include(x => x.Product)
                    .Where(x => x.OrderID == id)
                    .ToList();

                ViewBag.Order = order;

                return View(details);
            }
        }
    }
}
