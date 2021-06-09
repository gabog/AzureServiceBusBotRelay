# Overview
A relay utility for bots based on Azure Service Bus.  

This utility allows you to forward a message sent to a bot hosted on any channel to your local machine.

It is useful for debug scenarios or for more complex situations where the BotEmulator is not enough (i.e.: you use the WebChat control hosted on a site and you need to receive ChannelData in your requests).

## Acknowledgments 
Part of this code is based on the work that [Pedro Felix](https://github.com/pmhsfelix) did in his project [here](https://github.com/pmhsfelix/WebApi.Explorations.ServiceBusRelayHost).

# How to configure and run the utility
### Building with .Net Framework

1. Once the solution has been cloned to your machine, open the solution in Visual Studio.

2. In Solution Explorer, expand the **ServiceBusRelayUtil** folder.

3. Open the **App.config** file and replace the following values with those from your service bus (not the hybrid connection).
   
    a. "RelayNamespace" is the name of your service bus created earlier. Enter the value in place of **[Your Namespace]**.
    
    b. "RelayName" is the name of the shared access policy created in steps 9 through 11 during the service bus set up process. Enter the value in place of **[Your Relay Name]**.
    
    c. "PolicyName" is the value to the shared access policy created in steps 9 through 11 during the service bus set up process. Enter the value in place of **[Your Shared Access Policy Name]**.
   
    d. "PolicyKey" is the WCF relay to be used. Remember, this relay is programmatically created and only exists on your machine. Create a new, unused name and enter the value in place of **[Your Policy's Key]**.
   
    e. "TargetServiceAddress" sets the port to be used for localhost. The address and port number should match the address and port used by your bot. Enter a value in place of the "TODO" string part. For example, "http://localhost:[PORT]".
   
4. Before testing the relay, your Azure Web Bot's messaging endpoint must be updated to match the relay.
   
    a. Login to the Azure portal and open your Web App Bot.
    
    b. Select **Settings** under Bot management to open the settings blade.
    
    c. In the **Messaging endpoint** field, enter the service bus namespace and relay. The relay should match the relay name entered in the **App.config** file and should not exist in Azure.
    
    d. Append **"/api/messages"** to the end to create the full endpoint to be used. For example, “https://example-service-bus.servicebus.windows.net/wcf-example-relay/api/messages".
    
    e. Click **Save** when completed.
   
5. In Visual Studio, press **F5** to run the project.
   
6. Open and run your locally hosted bot.
   
7. Test your bot on a channel (Test in Web Chat, Skype, Teams, etc.). User data is captured and logged as activity occurs.

    - When using the Bot Framework Emulator: The endpoint entered in Emulator must be the service bus endpoint saved in your Azure Web Bot **Settings** blade, under **Messaging Endpoint**.

8. Once testing is completed, you can compile the project into an executable.

    a. Right click the project folder in Visual Studio and select **Build**.

    b. The .exe will output to the **/bin/debug** folder, along with other necessary files, located in the project’s directory folder. All the files are necessary to run and should be included when moving the .exe to a new folder/location.
    - The **app.config** is in the same folder and can be edited as credentials change without needing to recompile the project.

### Depoloy an Azure Relay service
[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https://raw.githubusercontent.com/negativeeddy/AzureServiceBusBotRelay/commandline/Deployment/deploy.json)

### Building with .Net Core

1. Once the solution has been cloned to your machine, open the solution in Visual Studio.

2. In Solution Explorer, expand the **ServiceBusRelayUtilNetCore** folder.

3. Open the **appsettings.json** file and replace the following values with those from your service bus hybrid connection.
   
    a. "RelayNamespace" is the name of your service bus created earlier. Enter the value in place of **[Your Namespace]**.

    b. "RelayName" is the name of the hybrid connection created in step 12. Enter the value in place of **[Your Relay Name]**.

    c. "PolicyName" is the name of the shared access policy created in steps 9 through 11 during the service bus set up process. Enter the value in place of **[Your Shared Access Policy Name]**.

    d. "PolicyKey" is the value to the shared access policy created in steps 9 through 11 during the service bus set up process. Enter the value in place of **[Your Policy's Key]**.
      
    e. "TargetServiceAddress" sets the port to be used for localhost. The address and port number should match the address and port used by your bot. Enter a value in place of the **"http://localhost:[PORT]"**. For example, "http://localhost:3978".
   
4. Before testing the relay, your Azure Web App Bot's messaging endpoint must be updated to match the relay.
   
    a. Login to the Azure portal and open your Web App Bot.
    
    b. Select **Settings** under Bot management to open the settings blade.
    
    c. In the **Messaging endpoint** field, enter the service bus namespace and relay.
    
    d. Append **"/api/messages"** to the end to create the full endpoint to be used. For example, “https://example-service-bus.servicebus.windows.net/hc1/api/messages".
    
    e. Click **Save** when completed.
   
5. In Visual Studio, press **F5** to run the project.
   
6. Open and run your locally hosted bot.
   
7. Test your bot on a channel (Test in Web Chat, Skype, Teams, etc.). User data is captured and logged as activity occurs.

    - When using the Bot Framework Emulator: The endpoint entered in Emulator must be the service bus endpoint saved in your Azure Web Bot **Settings** blade, under **Messaging Endpoint**.

8. Once testing is completed, you can compile the project into an executable.

    a. Right click the project folder in Visual Studio and select **Publish**.

    b. For **Pick a publish Target**, select **Folder**.

    c. For **Folder or File Share**, choose an output location or keep the default.

    d. Click **Create Profile** to create a publish profile.

    e. Click **Configure...** to change the build configuration and change the following:

    - **Configuration** to "Debug | Any CPU"
    - **Deployment Mode** to "Self-contained"
    - **Target Runtime** to "win-x64"

    f. Click **Save** and then **Publish**

    g. The .exe will output to the **/bin/debug** folder, along with other necessary files, located in the project’s directory folder. All the files are necessary to run and should be included when moving the .exe to a new folder/location.
    - The **appsettings.json** is in the same folder and can be edited as credentials change without needing to recompile the project.
