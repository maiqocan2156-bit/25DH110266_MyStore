
using System.Collections.Generic;

namespace _25DH110266_MyStore.Models.ViewModel
{
    public class ProductInventoryViewModel
    {
        public List<Product> StockProducts { get; set; }

        public List<Product> NeedRestockProducts { get; set; }
    }
}
