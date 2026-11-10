
using System.Collections.Generic;
using _25DH110266_MyStore.Models;
using PagedList;

namespace _25DH110266_MyStore.Models.ViewModel
{
    public class HomeProductVM
    {
        public List<Product> FeaturedProducts { get; set; }

        public IPagedList<Product> NewProducts { get; set; }
    }
}
