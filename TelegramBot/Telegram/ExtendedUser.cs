using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.YouTube.v3;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;

namespace TelegramBot.Models
{
    public class ExtendedUser : User
    {
        public ExtendedUser(User telegramUser)
        {

            id = telegramUser.Id;

            IsBot = telegramUser.IsBot;


            FirstName = telegramUser.FirstName;

            LastName = telegramUser.LastName;


            Username = telegramUser.Username;

            LanguageCode = telegramUser.LanguageCode;
            //TelegramUser = telegramUser;
        }

        public Dictionary<string, List<string>> ChatsAndSubscribedChannels = new Dictionary<string, List<string>>();

        public Dictionary<string, List<YouTubeNotification>> ChatsAndLastNotifications = new Dictionary<string, List<YouTubeNotification>>();

        public UserCredential Credential { get; set; }

        public string YoutubeUsername { get; set; }

        //public User TelegramUser { get; set; }

        //public Boolean SilentMode { get; set; } = false;

        public string YoutubeChannelId { get; internal set; }

        private int id;
        public new string Id {
            get {
                if (id != default(int))
                {
                    return id.ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        public override Boolean Equals(Object obj)
        {
            if (obj == null && this != null)
                return false;
            else if (obj == null && this == null)
                return true;
            else
            {
                var user = obj as ExtendedUser;
                return Id.Equals(user.Id);
            }
        }

        public static Boolean operator ==(ExtendedUser user1, ExtendedUser user2)
        {
            return user1.Equals(user2);
        }

        public static Boolean operator !=(ExtendedUser user1, ExtendedUser user2)
        {
            return !(user1 == user2);
        }

        public override String ToString()
        {
            return $"Id: {Id}\tName: {FirstName} {LastName}\n\rAccess: {Credential.Token.AccessToken}\n\rRefresh: {Credential.Token.RefreshToken}\n\rUserId from Credential: {Credential.UserId}";
        }

        public async Task<bool> RevokeToken() => await Credential.RevokeTokenAsync(new System.Threading.CancellationToken());
        public async Task<bool> RefreshTokens() => await Credential.RefreshTokenAsync(new System.Threading.CancellationToken());

        public async Task SetCredentialAsync(String id, String code)
        {

            Int64 chatId;
            long.TryParse(id, out chatId);

            try
            {
                var values = new Dictionary<string, string>
                    {
                        { "code", $"{code}" },
                        { "client_id", $"{AppSettings.client_id}" },
                        { "client_secret", $"{AppSettings.client_secret}" },
                        { "redirect_uri", $"{AppSettings.redirect_uri}" },
                        { "grant_type", $"authorization_code" }
                    };
                var url = "https://www.googleapis.com/oauth2/v4/token";
                var responseString = await Http.PostAsync<String>(url, values);

                JsonRespose json = JsonConvert.DeserializeObject<JsonRespose>(responseString);
                string access = json.access_token;
                string refresh = json.refresh_token;
                if (!String.IsNullOrEmpty(access) && !String.IsNullOrEmpty(refresh))
                {
                    string[] scopes = new string[] {
                    YouTubeService.Scope.Youtube,
                    YouTubeService.Scope.YoutubeForceSsl,
                    YouTubeService.Scope.Youtubepartner,
                    YouTubeService.Scope.YoutubeUpload,
                    YouTubeService.Scope.YoutubeReadonly
                    };

                    var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = AppSettings.client_id,
                            ClientSecret = AppSettings.client_secret
                        },
                        Scopes = scopes,
                        DataStore = new FileDataStore("Store")
                    });

                    var token = new TokenResponse
                    {
                        AccessToken = access,
                        RefreshToken = refresh
                    };

                    Credential = new UserCredential(flow, Environment.UserName, token);
                    if (Credential != null)
                    {
                        await Bot.SendTextMessageAsync(chatId, "Вход в аккаунт Google произошел успешно");
                        var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                        {
                            HttpClientInitializer = Credential, //проверка на не null
                            ApplicationName = "TelegramBot",
                        });
                        var channels = youtubeService.Channels;
                        var Request = channels.List("snippet,id");
                        Request.Mine = true;
                        var Response = await Request.ExecuteAsync();
                        if (Response.Items.Count > 0)
                        {
                            YoutubeUsername = Response.Items.ElementAt(0).Snippet.Title;
                            YoutubeChannelId = Response.Items.ElementAt(0).Id;
                        }
                    }
                    else
                    {
                        throw new NullReferenceException("Credential is null");
                    }
                }
            }
            catch (Exception e)
            {
                await Bot.SendTextMessageAsync(chatId, "Не удалось произвести вход в аккаунт Google. Попробуйте снова");
                await Bot.SendDebugMessageAsync(e.Message);
                await Bot.SendDebugMessageAsync(e.StackTrace);
            }
        }

        public override Int32 GetHashCode()
        {
            var hashCode = 1550466422;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + id.GetHashCode();
            return hashCode;
        }

        class JsonRespose
        {
            public string access_token { get; set; }
            public string expires_in { get; set; }
            public string token_type { get; set; }
            public string refresh_token { get; set; }
        }

    }

}
