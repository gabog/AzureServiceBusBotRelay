// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NegativeEddy.Bots.AzureServiceBusRelay.Service
{
    public class RelayOptions
    {
        public string RelayNamespace { get; set; }
        public string RelayName { get; set; }
        public string PolicyName { get; set; }
        public string PolicyKey { get; set; }
        public string TargetServiceAddress { get; set; }
    }
}