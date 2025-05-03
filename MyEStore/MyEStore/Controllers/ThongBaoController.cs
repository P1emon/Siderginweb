using Microsoft.AspNetCore.Mvc;
using MyEStore.Entities;
using MyEStore.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class ThongBaoController : Controller
{
    private readonly MyeStoreContext _ctx;

    public ThongBaoController(MyeStoreContext ctx)
    {
        _ctx = ctx;
    }

    // Giao diện hiển thị slider sắp diễn ra
    public async Task<IActionResult> Index()
    {
        // Lấy danh sách slider đang hiệu lực, trong khoảng thời gian hợp lệ
        var sliders = await _ctx.Sliders
            .Where(s => s.HieuLuc && s.NgayBatDau <= DateTime.Now && s.NgayKetThuc >= DateTime.Now)
            .OrderBy(s => s.NgayTao)
            .Select(s => new ThongBaonewModel
            {
                MaSlider = s.MaSlider,
                TieuDe = s.TieuDe,
                MoTa = s.MoTa,
                HinhAnh = s.HinhAnh,
                LinkQuangCao = s.LinkQuangCao,
                NgayTao = s.NgayTao,
                NgayBatDau = s.NgayBatDau,
                NgayKetThuc = s.NgayKetThuc
            })
            .ToListAsync();

        return View(sliders);
    }

}

// ViewModel để hiển thị thông tin slider


// ViewModel để hiển thị thông tin thông báo
