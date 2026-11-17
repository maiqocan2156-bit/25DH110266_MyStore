
using System.ComponentModel.DataAnnotations;

namespace _25DH110266_MyStore.Models
{
    public class UserMetadata
    {
        [StringLength(20)]
        public string UserRole { get; set; }
    }

    [MetadataType(typeof(UserMetadata))]
    public partial class User
    {
    }
    public class CategoryMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
        public string CategoryName { get; set; }
    }

    [MetadataType(typeof(CategoryMetadata))]
    public partial class Category
    {
    }
    public class ProductMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        public string ProductName { get; set; }

        [Range(typeof(decimal), "0.01", "999999999999",
            ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
        public decimal ProductPrice { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn danh mục.")]
        public int CategoryID { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Số lượng tồn không được nhỏ hơn 0.")]
        public int Quantity { get; set; }
    }

    [MetadataType(typeof(ProductMetadata))]
    public partial class Product
    {
    }
}
