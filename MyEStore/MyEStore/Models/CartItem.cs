namespace MyEStore.Models
{
    public class CartItem
    {
        public int MaHh { get; set; }
        public string TenHh { get; set; }
        public double DonGia { get; set; }
        public double GiamGia { get; set; } // Added to store discount
        public string? Hinh { get; set; }
        public int SoLuong { get; set; }
        public double ThanhTien => SoLuong * (DonGia * (1 - GiamGia)); // Use discounted price
    }
}