# AzureServiceBusBotRelay

## Overview

A relay utility for bots based on [Azure Relay](https://docs.microsoft.com/en-us/azure/azure-relay/relay-what-is-it).  

This utility allows you to forward a message sent to a bot hosted on any channel to your local machine.

It is useful for debug scenarios or for more complex situations where the BotEmulator is not enough (i.e.: you use the WebChat control hosted on a site and you need to receive ChannelData in your requests).

It uses the Azure Relay service along with a small local application to recieve messages from the bot service and forward them to your locally hosted bot.

![architecture diagram](Docs/architecture.png)

### Acknowledgments

Part of this code is based on the work that [Pedro Felix](https://github.com/pmhsfelix) did in his project [here](https://github.com/pmhsfelix/WebApi.Explorations.ServiceBusRelayHost).

## Setup

To setup the Azure Bot Service to connect to your local bot you need to

1. Deploy an Azure Relay service
2. Configure your Azure Bot Service to send messages to the Azure Relay
3. Launch your bot locally
4. Run the AzureServiceBusBotRelay tool configured to listen to your relay and push messages to your bot

### Deploy an Azure Relay service

You can use this button to deploy an Azure Relay service with the correct configuration. You will just need to supply it with a unique name for the Azure Service Bus namespace.

After it completes, select the Outputs tab and copy the 5 values. You will need those to configure the bot relay tool and your bot service.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fnegativeeddy%2FAzureServiceBusBotRelay%2Fcommandline%2FDeployment%2Fdeploy.json)

If you want to deploy the relay service manually you will need to

1. ensure the relay does not require authentication
2. add a shared access policy to the hybrid relay that has permission to send & listen

### Configure your Azure Web App Bot or Azure Bot Registration

Before testing the relay, your Azure Web App Bot's messaging endpoint must be updated to match the relay.

1. Login to the Azure portal and open your Web App Bot or Bot Registration.

2. Select **Settings** under Bot management to open the settings blade.

3. In the **Messaging endpoint** field, enter the service bus namespace and relay. This is the "messagingEndpoint" value from the output of the deployment step above.

    Ensure that the URI ends with "/api/messages"

    For example, “https://example-service-bus.servicebus.windows.net/hc1/api/messages".

4. Click **Save** when completed. (You might have to click save twice)

### Launch your bot locally

1. Start your bot as usual on your local machine

2. Once your bot is running, note the localhost endpoint it is attached to

### Building and run the relay tool

1. Once the solution has been cloned to your machine, open and build the solution in Visual Studio.

2. Run the tool with the settings you have captured up until now. The tool has five required options. Four of these are from the deployment outputs copied above. The fifth is the URI to your bot (do not include the "/api/messages" portion). 

````text
  -n, --namespace    The name of the relay's namespace, e.g. '[Your Namespace].servicebus.windows.net'

  -r, --relay        The name of the relay

  -p, --policy       The name of the relay's Shared Access Policy

  -k, --key          The Shared Access Policy's key

  -b, --botUri       The url to your local bot e.g. 'http://localhost:[PORT]'
````

You can specify the parameters either through the Visual Studio project's properties page, or you can run the tool from a command prompt.

Here is an example command line 

````text

ServiceBusRelayUtilNetCore.exe -n benwillidemorelay.servicebus.windows.net -r botrelay 
-p SendAndListenPolicy -k XOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOX -b http://localhost:3980
````

### Test your bot

1. Test your bot on a channel (Test in Web Chat, Skype, Teams, etc.). User data is captured and logged as activity occurs.
