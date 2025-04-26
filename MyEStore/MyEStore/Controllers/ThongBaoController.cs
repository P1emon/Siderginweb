using Microsoft.AspNetCore.Mvc;


public class ThongBaoController : Controller
{
    private ISliderThongBaoService _service;
    public ThongBaoController(ISliderThongBaoService service)
    {
        _service = service;
    }

    // Giao diện hiển thị slider sắp diễn ra
    public IActionResult Index()
    {
        var sliders = _service.GetSlidersSapDienRa();
        return View(sliders);
    }

    // Route test gửi email ngay lập tức
    [HttpGet]
    public IActionResult TestGuiEmail()
    {
        _service.GuiEmailThongBao();
        return Content("✅ Đã test gửi email cho toàn bộ khách hàng thành công.");
    }


}
