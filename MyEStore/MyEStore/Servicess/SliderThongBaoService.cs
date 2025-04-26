using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using MyEStore.Models;

namespace MyEStore.Services
{
    public class SliderThongBaoService : ISliderThongBaoService
    {
        private readonly MyeStoreContext _context;
        private readonly IEmailSender _emailSender;

        public SliderThongBaoService(MyeStoreContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public List<Slider> GetSlidersSapDienRa()
        {
            var targetDate = DateTime.Now.Date.AddDays(7);

            return _context.Sliders
                .Where(s => s.NgayBatDau.Date == targetDate && s.HieuLuc)
                .ToList();
        }

        public void GuiEmailThongBao()
        {
            var sliders = GetSlidersSapDienRa();

            if (!sliders.Any())
                return;

            // Lấy tất cả email khách hàng còn hiệu lực
            var emails = _context.KhachHangs
                .Where(kh => !string.IsNullOrEmpty(kh.Email) && kh.HieuLuc == true)
                .Select(kh => kh.Email)
                .Distinct()
                .ToList();

            if (!emails.Any())
                return;

            foreach (var slider in sliders)
            {
                string subject = $"[Sự kiện sắp diễn ra] {slider.TieuDe}";
                string body = $@"
                    Xin chào,<br/>
                    Sự kiện <strong>{slider.TieuDe}</strong> sẽ bắt đầu vào ngày <strong>{slider.NgayBatDau:dd/MM/yyyy}</strong>.<br/>
                    <a href='{slider.LinkQuangCao}'>Xem chi tiết tại đây</a><br/><br/>
                    Trân trọng,<br/>
                    <strong>My E-Store</strong>";

                foreach (var email in emails)
                {
                    try
                    {
                        _emailSender.SendEmail(email, subject, body);
                        Console.WriteLine($"✅ Đã gửi email tới: {email}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Lỗi gửi email tới {email}: {ex.Message}");
                    }
                }
            }
        }
    }
}
