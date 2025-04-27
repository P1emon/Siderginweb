namespace MyEStore.Entities
{
    public class Rank
    {
        public int Id { get; set; } // tự động Primary Key
        public string TenRank { get; set; } = null!;
        public int MucDiemTu { get; set; }
        public int MucDiemDen { get; set; }
        public string? LoiIch { get; set; }
        
        public string? Icon { get; set; } // Đường dẫn đến icon
    }
}
