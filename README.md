# Overview
An relay utility for bots based on Azure Service Bus.  

This utility allows you forward a message sent to a bot hosted on any channel to your local machine.

It is useful for debug scenarios or for more complex situations where the BotEmulator is not enough (i.e.: you use the WebChat control hosted on a site and you need to receive ChannelData in your requests).

## Acknowledgments 
Part of this code is based on the work that [Pedro Felix](https://github.com/pmhsfelix) did in his project [here](https://github.com/pmhsfelix/WebApi.Explorations.ServiceBusRelayHost).

# How to configure and run the utility
1.	Create an [Azure Relay Service](https://docs.microsoft.com/en-us/azure/service-bus-relay/) on your Azure Subscription.
2.	Write down the key value for the RootManageSharedAccessKey role.
3.	Pull this utility from github.
4.	Open the solution in Visual Studio 2017 and update the following parameters in app.config:  
    * **SBNamespace:** the namespace you used to create the service bus relay, don’t include .servicebus.windows.net, this will be appended in code.  
    Example: *contosorelay*.    
    * **SBPolicyKey:** the key for RootManageSharedAccessKey role in service bus.  
    * **SBRelayName:** the name of the relay to be created. The utility will create a dynamic relay for you, just put a name here that will be used in the service URL for the bot.  
    Example DebugBot  
    * **TargetServiceAddress:** update this if you are using anything different than http://localhost:3979/  
5.	Update the message endpoint for your bot to point to the SB namespace and relay (replace **[SBNamespace]** and **[SBRelayName]** by the values you included in app.config):   
    https://*[SBNamespace]*.servicebus.windows.net/*[SBRelayName]*/api/messages  
    For example:  
    https://*contosorelay*.servicebus.windows.net/*DebugBot*/api/messages  
6.	Compile an run 	the utility, your bot should now send messages to Azure Service Bus and the utility will forward them to your local machine on TargetServiceAddress so you can debug. 


