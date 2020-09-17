﻿using ClassifiedAds.Application.EmailMessages.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClassifiedAds.BackgroundServer.HostedServices
{
    public class ResendEmailHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ResendEmailHostedService> _logger;

        public ResendEmailHostedService(IServiceProvider services,
            ILogger<ResendEmailHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() => DoWork(stoppingToken));
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                int rs = 0;

                using (var scope = _services.CreateScope())
                {
                    var resendEmailTask = scope.ServiceProvider.GetRequiredService<ResendEmailTask>();

                    rs = resendEmailTask.Run();
                }

                if (rs == 0)
                {
                    await Task.Delay(10000, stoppingToken);
                }
            }
        }
    }
}