
namespace _25DH110266_MyStore.Models
{
    public class CategoryProductStatisticViewModel
    {
        public string CategoryName { get; set; }

        public int ProductCount { get; set; }

        public decimal MaxPrice { get; set; }

        public decimal MinPrice { get; set; }

        public decimal AveragePrice { get; set; }
    }
}
