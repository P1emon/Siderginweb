namespace MyEStore.Models
{
	public class HangHoaVM
	{
		public int MaHh { get; set; }
		public string TenHh { get; set; }
		public string Hinh { get; set; }
		public double DonGia { get; set; }
        public string TenAlias { get; set; }
        public double GiamGia { get; internal set; }
        public bool IsInStock { get; set; }
    }
}
