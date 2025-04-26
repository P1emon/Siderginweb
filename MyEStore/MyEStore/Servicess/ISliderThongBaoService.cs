using System.Collections.Generic;
using MyEStore.Models;

public interface ISliderThongBaoService
{
    List<Slider> GetSlidersSapDienRa();
    void GuiEmailThongBao();
}
