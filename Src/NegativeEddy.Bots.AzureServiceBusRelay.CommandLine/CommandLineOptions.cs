// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace NegativeEddy.Bots.AzureServiceBusRelay.CommandLine
{
    public class CommandLineOptions
    {
        [Option(
            'n',
            "namespace",
            Required = true,
            HelpText = "The name of the relay's namespace, e.g. '[Your Namespace].servicebus.windows.net'")]
        public string RelayNamespace { get; set; }

        [Option(
            'r',
            "relay",
            Required = true,
            HelpText = "The name of the relay")]
        public string RelayName { get; set; }

        [Option(
            'p',
            "policy",
            Required = true,
            HelpText = "The name of the relay's Shared Access Policy")]
        public string PolicyName { get; set; }

        [Option(
            'k',
            "key",
            Required = true,
            HelpText = "The Shared Access Policy's key")]
        public string PolicyKey { get; set; }

        [Option(
            'b',
            "botUri",
            Required = true,
            HelpText = "The url to your local bot e.g. 'http://localhost:[PORT]'")]
        public string TargetServiceAddress { get; set; }
    }
}