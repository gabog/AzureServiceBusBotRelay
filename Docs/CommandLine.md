# Command Line Instructions

## Overview

The Azure Bot Relay command line works similar to ngrok. It is a command line tool that will connect to your Azure Service Bus and forward all the incoming bot messages to your local bot. 

## Setup

### Deploy an Azure Relay service

Set up the bot relay in the same way indicated in the [README.md](../README.md)

#### Launch your bot locally

1. Start your bot as usual on your local machine

2. Once your bot is running, note the localhost endpoint it is attached to

#### Building and run the relay tool

1. Clone the solution to your machine, open and build the solution in Visual Studio.

2. Run the command line tool with the settings you have captured up until now. The tool has five required options. Four of these are from the deployment outputs of the relaye. The fifth is the URI to your bot (do not include the "/api/messages" portion). 

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

1. Test your bot on a channel (Test in Web Chat, Skype, Teams, etc.).