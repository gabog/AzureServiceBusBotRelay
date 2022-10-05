// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;

namespace NegativeEddy.Bots.AzureServiceBusRelay.CommandLine
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(opt => CreateHostBuilder(opt).Build().Run());
        }

        public static IHostBuilder CreateHostBuilder(CommandLineOptions options) =>
            Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(cb => cb.AddUserSecrets(typeof(Program).Assembly))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<CommandLineOptions>(options);
                    services.AddHostedService<DispatcherService>();
                });
    }
}