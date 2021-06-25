using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;

namespace GaboG.ServiceBusRelayUtil
{
    internal class Program
    {
        // https://github.com/pmhsfelix/WebApi.Explorations.ServiceBusRelayHost
        // https://docs.microsoft.com/en-us/azure/service-bus-relay/service-bus-relay-rest-tutorial
        private static void Main()
        {
            var relayNamespace = ConfigurationManager.AppSettings["RelayNamespace"];
            var relayAddress = ServiceBusEnvironment.CreateServiceUri("https", relayNamespace, ConfigurationManager.AppSettings["RelayName"]);

            var config = new ServiceBusRelayUtilConfig
            {
                RelayAddress = relayAddress,
                RelayPolicyName = ConfigurationManager.AppSettings["PolicyName"],
                RelayPolicyKey = ConfigurationManager.AppSettings["PolicyKey"],
                MaxReceivedMessageSize = long.Parse(ConfigurationManager.AppSettings["MaxReceivedMessageSize"]),
                TargetAddress = new Uri(ConfigurationManager.AppSettings["TargetServiceAddress"])
            };

            var host = CreateWebServiceHost(config, relayAddress);
            host.Open();

            Console.WriteLine("Azure Service Bus is listening at \n\r\t{0}\n\rrouting requests to \n\r\t{1}\n\r\n\r", relayAddress, config.TargetAddress);
            Console.WriteLine();
            Console.WriteLine("Press [Enter] to exit");
            Console.ReadLine();

            host.Close();
        }

        private static WebServiceHost CreateWebServiceHost(ServiceBusRelayUtilConfig config, Uri address)
        {
            var host = new WebServiceHost(new DispatcherService(config));
            var binding = GetBinding(config.MaxReceivedMessageSize);
            var endpoint = host.AddServiceEndpoint(typeof(DispatcherService), binding, address);
            var behavior = GetTransportBehavior(config.RelayPolicyName, config.RelayPolicyKey);
            endpoint.Behaviors.Add(behavior);
            return host;
        }

        private static Binding GetBinding(long maxReceivedMessageSize)
        {
            var webHttpRelayBinding = new WebHttpRelayBinding(EndToEndWebHttpSecurityMode.None, RelayClientAuthenticationType.None)
            {
                MaxReceivedMessageSize = maxReceivedMessageSize
            };
            var bindingElements = webHttpRelayBinding.CreateBindingElements();
            var webMessageEncodingBindingElement = bindingElements.Find<WebMessageEncodingBindingElement>();
            webMessageEncodingBindingElement.ContentTypeMapper = new RawContentTypeMapper();
            return new CustomBinding(bindingElements);
        }

        private static TransportClientEndpointBehavior GetTransportBehavior(string keyName, string sharedAccessKey)
        {
            return new TransportClientEndpointBehavior
            {
                TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, sharedAccessKey)
            };
        }
    }
}