using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

public class ThongBaoEmailService : BackgroundService
{
    private readonly IServiceProvider _provider;

    public ThongBaoEmailService(IServiceProvider provider)
    {
        _provider = provider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISliderThongBaoService>();
            service.GuiEmailThongBao();

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Gửi mỗi ngày
        }
    }
}
