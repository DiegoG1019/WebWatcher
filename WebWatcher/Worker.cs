using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiegoG.WebWatcher
{
    public class Worker : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => Task.WhenAll(
                Task.Run(async () => await WatcherService.Active(stoppingToken), stoppingToken), 
                Task.Run(async () => await SubscriptionService.Active(stoppingToken), stoppingToken)
            );
    }
}
