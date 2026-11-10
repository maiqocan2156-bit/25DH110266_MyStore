
using System.Collections.Generic;

namespace _25DH110266_MyStore.Models.ViewModel
{
    public class ProductDetailVM
    {
        public Product Product { get; set; }

        public List<Product> RelatedProducts { get; set; }

        public List<Product> TopProducts { get; set; }

        public int Page { get; set; }

        public int TotalPages { get; set; }
    }
}
