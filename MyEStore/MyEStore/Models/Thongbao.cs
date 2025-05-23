﻿using MyEStore.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyEStore.Models
{
    public class Thongbao
    {
        public int MaHd { get; set; }
        public DateTime NgayDat { get; set; }
        public DateTime? NgayGiao { get; set; }
        public int? DaysToDelivery { get; set; }
        public List<ThongBao> ThongBaos { get; set; }

    }
}
