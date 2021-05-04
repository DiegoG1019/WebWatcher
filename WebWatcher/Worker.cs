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
            => Service.Active(stoppingToken);
    }
}
