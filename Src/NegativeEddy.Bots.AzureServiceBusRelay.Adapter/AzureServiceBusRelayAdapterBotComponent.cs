// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NegativeEddy.Bots.AzureServiceBusRelay.Service;

namespace NegativeEddy.Bots.AzureServiceBusRelay.Adapter
{
    public class AzureServiceBusRelayAdapterBotComponent : BotComponent
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            if (configuration != null)
            {
                services.AddSingleton(new RelayOptions
                {
                    PolicyKey = configuration["SASKey"],
                    PolicyName = configuration["SASPolicy"],
                    RelayNamespace = configuration["Namespace"],
                    RelayName = configuration["Relay"],
                });
                services.AddHostedService<DispatcherService>();
            }
        }
    }
}
