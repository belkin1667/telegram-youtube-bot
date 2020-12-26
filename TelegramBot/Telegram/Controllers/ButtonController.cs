using FFMpegCore;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Downloading;
using YoutubeExplode;

namespace TelegramBot.Models.Commands
{
    public static class ButtonController
    {
        public static async Task Execute(Update update)
        {
            try
            {
                var userId = update.CallbackQuery.From.Id.ToString();
                ExtendedUser user = null;
                user = Bot.GetUserById(userId);
                YoutubeExplode.Models.Channel author = null;
                string channelId = "";
                if (user != null)
                {
                    var CallbackQuery = update.CallbackQuery;
                    var CallbackData = CallbackQuery.Data.Split('~');
                    //0 - btnName; 1 - videoId; 2 - chatId; 3 - rate; 4 - subscribed; 
                    var btnName = CallbackData[0];
                    string videoId = "";
                    long chatId = -1;
                    string rate = null;
                    bool subscribed = false;
                    if (CallbackData.Length > 1)
                    {
                        videoId = CallbackData[1];
                        if (CallbackData.Length > 2)
                        {
                            chatId = long.Parse(CallbackData[2]);
                            if (CallbackData.Length > 3)
                            {
                                rate = CallbackData[3];
                                subscribed = CallbackData[4] == "t";
                            }
                        }
                        try
                        {
                            author = await new YoutubeClient().GetVideoAuthorChannelAsync(videoId);
                            channelId = author.Id;
                        } catch
                        {
                            //videoid can be "-" if user wants to logout, so just ignore exception
                        }
                    }

                    switch (btnName)
                    {
                        case "l": //like
                            if (user?.Credential != null)
                            {
                                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                                {
                                    HttpClientInitializer = user.Credential, 
                                    ApplicationName = "TelegramBot",
                                });
                                await youtubeService.Videos.Rate(videoId, VideosResource.RateRequest.RatingEnum.Like).ExecuteAsync();
                                if (chatId != -1)
                                    await Bot.EditMessageReplyMarkupAsync(chatId, CallbackQuery.Message.MessageId, await GetMarkupAsync(chatId, videoId, subscribed, "l"));
                            }
                            else
                            {
                                if (chatId != -1)
                                    await Bot.SendTextMessageAsync(chatId, "Невозможно поставить оценку видео, вы должны войти в ваш аккаунт YouTube. Выполните команду /login");
                            }
                            break;
                        case "d": //dislike
                            if (user?.Credential != null)
                            {
                                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                                {
                                    HttpClientInitializer = user.Credential, 
                                    ApplicationName = "TelegramBot",
                                });
                                await youtubeService.Videos.Rate(videoId, VideosResource.RateRequest.RatingEnum.Dislike).ExecuteAsync();
                                if (chatId != -1)
                                    await Bot.EditMessageReplyMarkupAsync(chatId, CallbackQuery.Message.MessageId, await GetMarkupAsync(chatId, videoId, subscribed, "d"));
                            }
                            else
                            {
                                if (chatId != -1)
                                    await Bot.SendTextMessageAsync(chatId, "Невозможно поставить оценку видео, вы должны войти в ваш аккаунт YouTube. Выполните команду /login");
                            }
                            break;
                        case "d-A": //download audio
                            if (chatId == -1)
                            {
                                var chats = user.ChatsAndLastNotifications.Keys;
                                if (chats.Count > 0)
                                {
                                    chatId = long.Parse(chats.ElementAt(0));
                                    if (chats.Contains(user.Id))
                                        chatId = long.Parse(user.Id);
                                }
                            }
                            if (chatId != -1)
                            {
                                await Bot.SendTextMessageAsync(chatId, "Началась загрузка аудиофайла. Пожалуйста, подождите - это может занять некоторое время");
                                Thread thread = new Thread(new ParameterizedThreadStart(Audio));
                                thread.Start(new Data(chatId, user, videoId));
                            }
                            break;
                        case "s-N": //switch Notifications
                            if (user?.Credential != null)
                            {
                                if (user.ChatsAndSubscribedChannels.GetValueOrDefault(chatId.ToString()).Contains(channelId))
                                {
                                    await PubSubHubbub.PuSH.RemoveChannelAsync(chatId, user, channelId);
                                    await Bot.SendTextMessageAsync(chatId,
                                                                   $"Теперь с канала [{author.Title}](https://www.youtube.com/channel/{channelId}) не будут приходить уведомления о новых видео",
                                                                   ParseMode.Markdown,
                                                                   null);
                                    await Bot.EditMessageReplyMarkupAsync(chatId, CallbackQuery.Message.MessageId, await GetMarkupAsync(chatId, videoId, false, rate));
                                }
                                else
                                {
                                    await PubSubHubbub.PuSH.AddChannelAsync(chatId, user, channelId);
                                    await Bot.SendTextMessageAsync(chatId,
                                                                   $"Теперь с канала [{author.Title}](https://www.youtube.com/channel/{channelId}) будут приходить уведомления о новых видео",
                                                                   ParseMode.Markdown,
                                                                   null);
                                    await Bot.EditMessageReplyMarkupAsync(chatId, CallbackQuery.Message.MessageId, await GetMarkupAsync(chatId, videoId, true, rate));
                                }
                            }
                            break;
                        case "l-o": //logout
                            if (user?.Credential != null)
                            {
                                user.Credential = null;
                                user.YoutubeChannelId = null;
                                user.YoutubeUsername = null;

                                await Bot.SendTextMessageAsync(chatId, $"*Вы вышли из аккаунта YouTube*" + "\n\r" +
                                                                            $"Больше вы не будете получать уведомления о новых видео. " + "\n\r" +
                                                                            $"Чтобы включить уведомления отправьте команду /start и следуйте инструкциям", ParseMode.Markdown, null);

                                await PubSubHubbub.PuSH.RemoveAllChannelsAsync(chatId, user);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                await Bot.SendDebugMessageAsync(e.Message);
                await Bot.SendDebugMessageAsync(e.StackTrace);
            }

        }

        public static async Task<InlineKeyboardMarkup> GetMarkupAsync(long chatId, string videoId, bool subscribed, string rate) //rate: l = like; d = dislike; n = neutral
        {
            var video = await new YoutubeClient().GetVideoAsync(videoId);
            var author = video.Author;
            //'~' is used to split CallbackQuery params, so it's cannot be used in videoId, channelId or title
            //more info at: https://webapps.stackexchange.com/questions/54443/format-for-id-of-youtube-video

            var s = "f";
            if (subscribed)
                s = "t";

            videoId = videoId.Replace('~', '-');

            InlineKeyboardButton like = new InlineKeyboardButton();

            if (rate == "l")
                like.Text += "✅ 👍";
            else
                like.Text = "👍";
            like.CallbackData = $"l~{videoId}~{chatId}~{rate}~{s}";

            InlineKeyboardButton dislike = new InlineKeyboardButton();

            if (rate == "d")
                dislike.Text = "✅ 👎";
            else
                dislike.Text = "👎";

            dislike.CallbackData = $"d~{videoId}~{chatId}~{rate}~{s}";

            InlineKeyboardButton share = new InlineKeyboardButton();
            share.Text = "📤 Поделиться"; //
            share.SwitchInlineQuery = $"videoId: {video.Id}";

            InlineKeyboardButton search = new InlineKeyboardButton();
            search.Text = "🔎 Поиск"; //
            search.SwitchInlineQueryCurrentChat = $"{author}: ";


            InlineKeyboardButton switchNotifications = new InlineKeyboardButton();
            if (subscribed)
            {
                switchNotifications.Text = "Выключить уведомления";
            }
            else
            {
                switchNotifications.Text = "Включить уведомления";
            }
            switchNotifications.CallbackData = $"s-N~{videoId}~{chatId}~{rate}~{s}";

            InlineKeyboardButton downloadAudio = new InlineKeyboardButton();
            downloadAudio.Text = "📥 Загрузить аудио";
            downloadAudio.CallbackData = $"d-A~{videoId}~{chatId}~{rate}~{s}"; //0 - btnName; 1 - videoId; 2 - chatId; 3 - rate; 4 - subscribed; 

            InlineKeyboardButton[] row1 = new InlineKeyboardButton[] { like, dislike };
            InlineKeyboardButton[] row2 = new InlineKeyboardButton[] { switchNotifications };
            InlineKeyboardButton[] row3 = new InlineKeyboardButton[] { downloadAudio };
            InlineKeyboardButton[] row4 = new InlineKeyboardButton[] { share, search };
            

            InlineKeyboardButton[][] keyboard = new InlineKeyboardButton[][] { row1, row2, row3, row4 };
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);
            return markup;
        }

        private static async void Audio(object o)
        {
            Data data = (Data)o;
            FileInfo info = await YouTubeDownload.DownloadAudioAsync(data.chatId, data.user, data.videoId, data.title);
        }

        private class Data
        {
            public ExtendedUser user;
            public String videoId;
            public String title;
            public long chatId;

            public Data(long chatId, ExtendedUser user, String videoId)
            {
                this.user = user;
                this.videoId = videoId;
                this.chatId = chatId;

                title = getVideoTitle(videoId).Result;
            }

            private async Task<string> getVideoTitle(String videoId)
            {
                YoutubeClient client = new YoutubeClient();
                var video = await client.GetVideoAsync(videoId);
                return video.Title;
            }

        }

    }
}
