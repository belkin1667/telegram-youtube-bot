using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Models.Commands;
using YoutubeExplode;
using YoutubeExplode.Models;

namespace TelegramBot.Models
{
    /*
    TODO:
    Аутентификация

    1. Решение со StackOverflow (https://stackoverflow.com/questions/37264827/telegram-bot-oauth-authorization)
           Созать страницу callback.html
           Редирект с гугл-регистрации отправлять на callback.html
           В параметрах URL помимо code=<Token> получаем уникальный заранее сгенерированный для каждого пользователя state=<State>
           По State получаем экземпляр пользователя, добавляем ему <Token>, который далее используем для доступа к YouTube Api

       Либо через амазон, но почему то не работает редирект (открывается амазоновский Url, но страница телеграма)


       Может быть полезно:
                           OpenId Connect: https://developers.google.com/identity/protocols/OpenIDConnect
                         OAuth2.0 Example: https://support.dracoon.com/hc/en-us/articles/360001329825-OAuth-2-0-Example
                             OAuth2.0 RFC: https://tools.ietf.org/html/rfc6749#page-9
                                  Postman: https://www.getpostman.com/downloads/
           OpenID Connect in ASP.NET Core: https://andrewlock.net/an-introduction-to-openid-connect-in-asp-net-core/

       */
    public class Bot
    {
        private static TelegramBotClient botClient;
        private static List<Command> commandsList;

        public static List<ExtendedUser> users = new List<ExtendedUser>();

        public static IReadOnlyList<Command> Commands => commandsList.AsReadOnly();

        public static async Task<TelegramBotClient> GetBotClientAsync()
        {
            if (botClient != null)
            {
                return botClient;
            }

            commandsList = new List<Command>();
            //Adding commands:
            commandsList.Add(new StartCommand());
            commandsList.Add(new LoginCommand());
            commandsList.Add(new LogoutCommand());
            commandsList.Add(new InfoCommand());

            botClient = new TelegramBotClient(AppSettings.Key);
            string messageHook = string.Format(AppSettings.Url, "api/message/update");
            await botClient.SetWebhookAsync(messageHook);

            return botClient;
        }

        internal static ExtendedUser GetUserById(String userId)
        {
            try
            {
                return users.Find(x => x.Id.Equals(userId));
            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal static async Task SendTextMessageAsync(Int64 chatId, String msg, ParseMode parseMode = ParseMode.Default, bool disableWebPagePreview = false, bool disableNotification = false, int replyToMessageId = 0, IReplyMarkup replyMarkup = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            await botClient.SendTextMessageAsync(chatId, msg, parseMode, disableWebPagePreview, disableNotification, replyToMessageId, replyMarkup, cancellationToken);
        }

        internal static async Task SendTextMessageAsync(Int64 chatId, String msg, ParseMode parseMode = ParseMode.Default, IReplyMarkup replyMarkup = null)
        {
            await botClient.SendTextMessageAsync(chatId, msg, parseMode, false, false, 0, replyMarkup);
        }

        internal static async Task SendTextMessageAsync(Int64 chatId, String msg)
        {
            await botClient.SendTextMessageAsync(chatId, msg);
        }


        /*internal static void SendTextMessage(Int64 chatId, String msg)
        {
            botClient.SendTextMessageAsync(chatId, msg);
        }*/

        internal static async Task SendDebugMessageAsync(string message)
        {
            ChatId adminChatId = "269824373";
            //await botClient.SendTextMessageAsync(adminChatId, message);
        }

        /*internal static void SendVideo(Int64 chatId, InputOnlineFile video)
        {
            botClient.SendVideoAsync(chatId, video);
        }*/

        internal static async Task SendVideoAsync(Int64 chatId, InputOnlineFile video)
        {
            await botClient.SendVideoAsync(chatId, video);

        }

        internal static async Task SendChatActionAsync(Int64 chatId, ChatAction action)
        {
            await botClient.SendChatActionAsync(chatId, action);
        }

        internal static async Task EditMessageReplyMarkupAsync(String inlineMessageId, InlineKeyboardMarkup markup)
        {
            try
            {
                await botClient.EditMessageReplyMarkupAsync(inlineMessageId, markup);
            }
            catch
            {
                //means that markup was not modified for example liked video was liked again
                //it's ok, so do nothing
            }
        }

        internal static async Task EditMessageReplyMarkupAsync(ChatId chatId, int messageId, InlineKeyboardMarkup markup)
        {
            try
            {
                await botClient.EditMessageReplyMarkupAsync(chatId, messageId, markup);
            }
            catch
            {
                //means that markup was not modified for example liked video was liked again
                //it's ok, so do nothing
            }
        }


        internal static async Task SendAudioAsync(Int64 chatId, InputOnlineFile audio, string performer, string title)
        {
            await botClient.SendAudioAsync(chatId, audio, performer: $"{performer}", title: $"{title}");
        }

        internal static async Task AnswerInlineQueryAsync(ExtendedUser user, string inlineQueryId, IReadOnlyList<YoutubeExplode.Models.Video> videos)
        {
            var results = new List<InlineQueryResultArticle>();
            for (int i = 0; i < Math.Min(50, videos.Count); i++)
            {
                var video = videos.ElementAt(i);

                var message = $"*{video.Author}*" + "\n\r" +
                   $"[{video.Title}](https://www.youtube.com/watch?v={video.Id})";

                var thumbnail = video.Thumbnails.LowResUrl;
                var url = "https://www.youtube.com/watch?v=" + video.Id;
                InputTextMessageContent content = new InputTextMessageContent(message);
                content.ParseMode = ParseMode.Markdown;
                InlineQueryResultArticle result = new InlineQueryResultArticle(inlineQueryId + i * 123, video.Title, content);
                result.ThumbUrl = thumbnail;
                result.Url = url;
                result.HideUrl = true;

                result.ReplyMarkup = GetMarkup(user, video);
                result.Description = $@"👁️ {ShortenNumber(video.Statistics.ViewCount)} | {ShortenNumber(video.Statistics.LikeCount)} 👍 \ 👎 {ShortenNumber(video.Statistics.DislikeCount)}";

                results.Add(result);
            }

            await botClient.AnswerInlineQueryAsync(inlineQueryId, results, 100, true);
            //cacheTime  - The maximum amount of time the result of the inline query may be cached on the server
            //isPersonal - Pass True, if results may be cached on the server side only for the user that sent the query. By default, results may be returned to any user who sends the same query
            //offset     - Pass the offset that a client should send in the next query with the same text to receive more results. Pass an empty string if there are no more results or if you don‘t support pagination. Offset length can’t exceed 64 bytes.
            //pmText     - clients will display a button with specified text that switches the user to a private chat with the bot and sends the bot a start message with the parameter switchPmParameter
        }

        private static string ShortenNumber(Int64 number)
        {
            long n = Math.Abs(number);
            long t = 1000;
            long m = t * t;
            long b = m * t;
            if (n >= b) //billions
            {
                return number / b + " млрд.";
            }
            if (n >= m) //millions
            {
                return number / m + " млн.";
            }
            if (n >= t) //billions
            {
                return number / t + " тыс.";
            }
            return number.ToString();
        }

        private static InlineKeyboardMarkup GetMarkup(ExtendedUser user, YoutubeExplode.Models.Video video)
        {
            var videoId = video.Id.Replace('~', '-');
            var title = video.Title.Replace('~', '-');


            /*if (user.Credential != null)
            {*/
            InlineKeyboardButton like = new InlineKeyboardButton();
            like.Text = "👍"; //
            like.CallbackData = $"l~{videoId}~-1";

            InlineKeyboardButton dislike = new InlineKeyboardButton();
            dislike.Text = "👎"; //
            dislike.CallbackData = $"d~{videoId}~-1";


            InlineKeyboardButton share = new InlineKeyboardButton();
            share.Text = "📤 Поделиться"; //
            share.SwitchInlineQuery = $"videoId: {video.Id}";


            InlineKeyboardButton search = new InlineKeyboardButton();
            search.Text = "🔎 Поиск"; //
            search.SwitchInlineQueryCurrentChat = $"{video.Author}: ";

            //
            InlineKeyboardButton downloadAudio = new InlineKeyboardButton();
            downloadAudio.Text = "📥 Загрузить аудио";
            downloadAudio.CallbackData = $"d-A~{videoId}";

            InlineKeyboardButton[] row1 = new InlineKeyboardButton[] { like, dislike };
            InlineKeyboardButton[] row2 = new InlineKeyboardButton[] { downloadAudio };
            InlineKeyboardButton[] row3 = new InlineKeyboardButton[] { share, search };
            InlineKeyboardButton[][] keyboard = new InlineKeyboardButton[][] { row1, row2, row3 };
            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

            return markup;
            /*}
            else
            {
                InlineKeyboardButton downloadAudio = new InlineKeyboardButton();
                downloadAudio.Text = "Загрузить аудио";
                downloadAudio.CallbackData = $"d-A~{videoId}";

                InlineKeyboardButton[] row2 = new InlineKeyboardButton[] { downloadAudio };
                InlineKeyboardButton[][] keyboard = new InlineKeyboardButton[][] { row2 };
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(keyboard);

                return markup;
            }*/


        }
    }
}
