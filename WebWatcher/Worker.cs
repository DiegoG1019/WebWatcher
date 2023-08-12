using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DiegoG.WebWatcher;

public class Worker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.WhenAny(
            Task.Run(async () => await WatcherService.Active(stoppingToken), stoppingToken),
            Task.Run(async () => await SubscriptionService.Active(stoppingToken), stoppingToken)
        );
}
