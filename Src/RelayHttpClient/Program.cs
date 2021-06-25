using Microsoft.Azure.Relay;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RelayHttpClient
{
    public static class Program
    {
        public static IConfiguration Configuration { get; set; }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();


            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            var relayNamespace = $"{Configuration["Relay:Namespace"]}.servicebus.windows.net";
            var connectionName = Configuration["Relay:ConnectionName"];
            var keyName = Configuration["Relay:PolicyName"];
            var key = Configuration["Relay:PolicyKey"];

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(
             keyName, key);
            var uri = new Uri(string.Format("https://{0}/{1}", relayNamespace, connectionName));
            var token = (await tokenProvider.GetTokenAsync(uri.AbsoluteUri, TimeSpan.FromHours(1))).TokenString;
            var client = new HttpClient();
            var request = new HttpRequestMessage()
            {
                RequestUri = uri,
                Method = HttpMethod.Get,
            };
            request.Headers.Add("ServiceBusAuthorization", token);
            var response = await client.SendAsync(request);
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}
