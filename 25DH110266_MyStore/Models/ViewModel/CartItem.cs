
namespace _25DH110266_MyStore.Models.ViewModel
{
    public class CartItem
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; }

        public string ProductImage { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; }

        public decimal Total
        {
            get { return ProductPrice * Quantity; }
        }
    }
}
