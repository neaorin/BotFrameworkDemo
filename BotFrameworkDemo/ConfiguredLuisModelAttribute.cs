using Microsoft.Bot.Builder.Luis;
using System;
using System.Configuration;

namespace BotFrameworkDemo
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true)]
    public class ConfiguredLuisModelAttribute : LuisModelAttribute, ILuisModel
    {
        public ConfiguredLuisModelAttribute() : base(
            GetModelId(),
            GetSubscriptionKey(),
            LuisApiVersion.V2)
        { }

        private static string GetModelId()
        {
            return ConfigurationManager.AppSettings.Get("LuisModelId");
        }

        private static string GetSubscriptionKey()
        {
            return ConfigurationManager.AppSettings.Get("LuisSubscriptionKey");
        }
    }
}