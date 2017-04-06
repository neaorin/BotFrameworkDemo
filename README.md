# Codecamp Romania Bot (beta)

A demo of using [Microsoft Bot Framework](https://dev.botframework.com/) and [Language Understanding Intelligent Service](http://luis.ai) (LUIS) to produce an intelligent bot.

This bot allows users to find sessions that are of interest to them at [Codecamp Romania](http://www.codecamp.ro) events.

![](images/skype.png)

## Setup

Follow these steps to set up your own copy of this bot:

1. Clone this repository.

2. Log onto [LUIS.ai](http://luis.ai) and [import an application](https://www.luis.ai/Help/Index#ImportingApps) by using the `BotFrameworkDemo/CodeCampDemoLuisApp.json` file. 

3. [Train](https://www.luis.ai/Help/Index#Training) and [Publish](https://www.luis.ai/Help/Index#PublishingModel) your model.

4. Open the `BotFrameworkDemo.sln` solution file in Visual Studio 2015 or later.

5. Open the `BotFrameworkDemo/CodeCampDialog.cs` file and point the bot dialog at your imported LUIS model, by editing the parameters for the `LuisModel` attribute as shown in the picture below. 
   * You will find the **Application ID** as the GUID in the URL of the application, when editing it inside the LUIS.ai portal. (the URL will be `https://www.luis.ai/application/xxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`)
   * For the **key** to use, see the [Manage your keys](https://www.microsoft.com/cognitive-services/en-us/LUIS-api/documentation/Manage-Keys) section.

    ![](images/luisModelConfig.png)

6. Build and run the `BotFrameworkDemo` project in Debug mode. You should have a web application running on `http://localhost:3979/`.

    ![](images/webapp.png)


7. Use the [Bot Framework Channel Emulator](https://download.botframework.com/bf-v3/tools/emulator/publish.htm) to test the bot locally. The only configuration required is to set the Bot URL to `http://localhost:3979/api/messages`.

    ![](images/emulator.png)

8. Go to [Microsoft Bot Framework](https://dev.botframework.com/) portal and [register a new bot](https://dev.botframework.com/bots/new), then you can enable it for Skype and any other channels you like.

9. To make your bot accessible by other users, you need to:
    * Update the `Web.config` file of the project to reflect the correct `BotId`, `MicrosoftAppId` and `MicrosoftAppPassword` for the bot you have registered. You would have created all three values during the previous step, in the Microsoft Bot Framework portal, in the configuration section for your bot.

        ![](images/botregister.png)
        ![](images/webconfig.png)

    * Deploy the `BotFrameworkDemo` web application to a public website host - for example [Azure App Service](https://azure.microsoft.com/en-us/services/app-service/). Here is [a tutorial](https://azure.microsoft.com/en-us/documentation/articles/web-sites-dotnet-get-started/). You can get $25 of free Azure per month by signing up for [Visual Studio Dev Essentials](https://www.visualstudio.com/dev-essentials/).
    * Publish the bot from the [Microsoft Bot Framework](https://dev.botframework.com/) portal.



You can post any issues you find with this bot right inside the Issues section of this GitHub repo.

## Have fun!

*Sorin Peste (sorinpe at microsoft dot com)*