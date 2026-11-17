
using System.Collections.Generic;
using _25DH110266_MyStore.Models;

namespace _25DH110266_MyStore.Models.ViewModel
{
    public class MyOrderVM
    {
        public string SearchTerm { get; set; }

        public List<Order> TatCaDon { get; set; }

        public List<Order> ChoThanhToan { get; set; }

        public List<Order> DangXuLy { get; set; }

        public List<Order> DangVanChuyen { get; set; }

        public List<Order> DaGiao { get; set; }

        public List<Order> DaHuy { get; set; }
    }
}
