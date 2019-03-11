using System;

namespace GaboG.ServiceBusRelayUtil
{
    public class ServiceBusRelayUtilConfig
    {
        public string RelayPolicyName { get; set; }
        public string RelayPolicyKey { get; set; }
        public Uri RelayAddress { get; set; }

        public bool BufferRequestContent { get; set; }
        public long MaxReceivedMessageSize { get; set; }
        public Uri TargetAddress { get; set; }
    }
}