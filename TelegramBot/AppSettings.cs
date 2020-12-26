using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class AppSettings
    {

        public const string AppInfo = 
            "================================" +
            "\n\r" +
            "\n\r" +
            "https://t.me/YouTubeSubscriptionsBot" +
            "\n\r" +
            "Telegram Bot @YouTubeSubscriptionsBot" +
            "\n\r" +
            "\n\r" +
            "================================" +
            "\n\r" +
            "\n\r" +
            "Powered by Mike Belkin, 2019" +
            "\n\r" +
            "HSE CS SE Student" +
            "\n\r" +
            "\n\r" +
            "================================";

        public static string YouTubeAuthUrl         = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={client_id}&prompt={prompt}&response_type={response_type}&scope={scope}&redirect_uri={redirect_uri}&access_type={access_type}";
        public const string client_id               = "851581466263-nrgosup4ggsm49qp56ks4j6k3l2bgboc.apps.googleusercontent.com";
        public const string client_secret           = "Xqbk8qdgePOLP6GhL7Vz_cMX"; 
        public const string response_type           = "code";
        public const string scope                   = "https://www.googleapis.com/auth/youtube";
        public const string redirect_uri            = "https://youtubesubscriptionsbot.azurewebsites.net/auth/google";
        public const string access_type             = "offline";
        public const string prompt                  = "consent"; //none|consent|select_account
        public const string AzureUrl                = "https://youtubesubscriptionsbot.azurewebsites.net";
        public const string apikey                  = "AIzaSyBeEpYhC3KC3Y8pbn-I6B8ESSRWlYq-ntQ";
        public static string Url { get; set; }      = "https://youtubesubscriptionsbot.azurewebsites.net:443/{0}";
        public static string Name { get; set; }     = "YouTubeBot";
        public static string Key { get; set; }      = "776686035:AAHNDrYuevuC6e8hoczjuol1yd0GXgM710M";
        public static string BotUrl { get; set; }   = "https://t.me/YouTubeSubscriptionsBot";
    }
}
