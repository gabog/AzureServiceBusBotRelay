using System;

namespace GaboG.ServiceBusRelayUtil
{
    public class ServiceBusRelayUtilConfig
    {
        public string SBPolicyName { get; set; }
        public string SBPolicyKey { get; set; }
        public Uri SBAddress { get; set; }

        public bool BufferRequestContent { get; set; }
        public long MaxReceivedMessageSize { get; set; }
        public Uri TargetAddress { get; set; }
    }
}