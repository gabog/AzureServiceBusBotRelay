using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using Microsoft.ServiceBus;

namespace GaboG.ServiceBusRelayUtil
{
    internal class Program
    {
        private static void Main()
        {
            var sbNamespace = ConfigurationManager.AppSettings["SBNamespace"];
            var sbAddress = ServiceBusEnvironment.CreateServiceUri("https", sbNamespace, ConfigurationManager.AppSettings["SBRelayName"]);

            var config = new ServiceBusRelayUtilConfig
            {
                SBAddress = sbAddress,
                SBPolicyName = ConfigurationManager.AppSettings["SBPolicyName"],
                SBPolicyKey = ConfigurationManager.AppSettings["SBPolicyKey"],
                MaxReceivedMessageSize = long.Parse(ConfigurationManager.AppSettings["MaxReceivedMessageSize"]),
                TargetAddress = new Uri(ConfigurationManager.AppSettings["TargetServiceAddress"])
            };

            var host = CreateWebServiceHost(config, sbAddress);
            host.Open();

            Console.WriteLine("Azure Service Bus is listening at \n\r\t{0}\n\rrouting requests to \n\r\t{1}\n\r\n\r", sbAddress, config.TargetAddress);
            Console.WriteLine();
            Console.WriteLine("Press [Enter] to exit");
            Console.ReadLine();

            host.Close();
        }

        private static WebServiceHost CreateWebServiceHost(ServiceBusRelayUtilConfig config, Uri address)
        {
            //WebServiceHost host = new WebServiceHost(typeof(DispatcherService), address);
            var host = new WebServiceHost(new DispatcherService(config));
            var binding = GetBinding(config.MaxReceivedMessageSize);
            var endpoint = host.AddServiceEndpoint(typeof(DispatcherService), binding, address);
            var behavior = GetTransportBehavior(config.SBPolicyName, config.SBPolicyKey);
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