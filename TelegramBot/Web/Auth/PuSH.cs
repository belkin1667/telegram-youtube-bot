using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TelegramBot.Models;

namespace TelegramBot.PubSubHubbub
{
    public static class PuSH
    {
        public static async Task AddChannelAsync(long chatId, ExtendedUser user, string channelId) //ID!!! NOT URL
        {
            var requestUrl = "https://pubsubhubbub.appspot.com/subscribe";

            Dictionary<string, string> requestParams = new Dictionary<string, string>();
            requestParams.Add("hub.callback", $"https://youtubesubscriptionsbot.azurewebsites.net:443/webhook/youtube/updates/{user.Id}_{chatId}");
            requestParams.Add("hub.mode", "subscribe"); // subscribe | unsubscribe
            requestParams.Add("hub.topic", "https://www.youtube.com/xml/feeds/videos.xml?channel_id=" + channelId);
            var response = await Http.PostAsync<HttpStatusCode>(requestUrl, requestParams);

            if (response == HttpStatusCode.Accepted)
                user.ChatsAndSubscribedChannels.GetValueOrDefault(chatId.ToString()).Add(channelId);
        }

        public static async Task RemoveChannelAsync(long chatId, ExtendedUser user, string channelId)
        {
            var requestUrl = "https://pubsubhubbub.appspot.com/subscribe";

            Dictionary<string, string> requestParams = new Dictionary<string, string>();
            requestParams.Add("hub.callback", $"https://youtubesubscriptionsbot.azurewebsites.net:443/webhook/youtube/updates/{user.Id}_{chatId}");
            requestParams.Add("hub.mode", "unsubscribe"); // subscribe | unsubscribe
            requestParams.Add("hub.topic", "https://www.youtube.com/xml/feeds/videos.xml?channel_id=" + channelId);
            var response = await Http.PostAsync<HttpStatusCode>(requestUrl, requestParams);

            if (response == HttpStatusCode.Accepted)
                user.ChatsAndSubscribedChannels.GetValueOrDefault(chatId.ToString()).Remove(channelId);

        }

        public static async Task AddAllChannelsAsync(long chatId, ExtendedUser user)
        {
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = user.Credential, //проверка на не null
                   //ApiKey = AppSettings.apikey,
                    ApplicationName = "TelegramBot",
                });

                var subscribtions = youtubeService.Subscriptions;

                // Request settings
                var Request = subscribtions.List("snippet");
                Request.Mine = true;
                Request.MaxResults = 50; //max is 50, default is 5

                // Response handling
                var Response = await Request.ExecuteAsync();

                //Counting the amount of pages
                int amountOfPages = Response.PageInfo.TotalResults.Value / Response.PageInfo.ResultsPerPage.Value;
                if (Response.PageInfo.TotalResults.Value % Response.PageInfo.ResultsPerPage.Value != 0)
                {
                    amountOfPages++;
                }
                for (int i = 0; i < amountOfPages; i++)
                {
                    foreach (var item in Response.Items)
                    {
                        var channelId = item.Snippet.ResourceId.ChannelId;
                        await AddChannelAsync(chatId, user, channelId);
                    }
                    if (Response.NextPageToken != null && Response.NextPageToken != String.Empty)
                    {
                        Request.PageToken = Response.NextPageToken;
                        Response = Request.Execute();
                    }
                }
            }
            catch (Exception ex)
            {
                await Bot.SendTextMessageAsync(chatId, ex.Message);
                await Bot.SendTextMessageAsync(chatId, ex.StackTrace);
            }
        }

        public static async Task RemoveAllChannelsAsync(long chatId, ExtendedUser user)
        {
            foreach (var channelId in user.ChatsAndSubscribedChannels.GetValueOrDefault(chatId.ToString()).ToList())
            {
                await RemoveChannelAsync(chatId, user, channelId);
            }
        }
    }
}
