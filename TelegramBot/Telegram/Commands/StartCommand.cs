using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Models.Commands
{
    public class StartCommand : Command
    {
        public override string Name => @"/start";

        public override bool Contains(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return message.Text.Contains(this.Name);
        }

        public override async Task Execute(Message message, Update update)
        {

            try
            {
                var chatId = message.Chat.Id;
                var userId = message.From.Id.ToString();
                string msg = "";
                ExtendedUser user = new ExtendedUser(message.From);

                if (!Bot.users.Contains(user))
                {
                    Bot.users.Add(user);
                }
                user = Bot.GetUserById(userId);

                if (!user.ChatsAndLastNotifications.ContainsKey(chatId.ToString()))
                {
                    user.ChatsAndLastNotifications.TryAdd(chatId.ToString(), new List<YouTubeNotification>());
                }
                if (!user.ChatsAndSubscribedChannels.ContainsKey(chatId.ToString()))
                {
                    user.ChatsAndSubscribedChannels.TryAdd(chatId.ToString(), new List<string>());
                }

                if (user.Credential == null) //offer sign in
                {
                    msg = $"Добро пожаловать, @{user.Username}" + $"\n\r" +
                          $"Войдите в ваш аккаунт YouTube, чтобы получать уведомления о новых видео" + $"\n\r" +
                          $"Для получения справки отправьте команду /info";
                    InlineKeyboardButton login = new InlineKeyboardButton();
                    login.Text = "Войти в аккаунт YouTube";
                    login.Url = $"{AppSettings.YouTubeAuthUrl}&state={userId}_{chatId}";
                    InlineKeyboardMarkup markup = new InlineKeyboardMarkup(login);

                    await Bot.SendTextMessageAsync(chatId, msg, Telegram.Bot.Types.Enums.ParseMode.Default, markup);
                }
                else //offer sign out
                {
                    msg = $"Добро пожаловать, @{user.Username}\n\r" +
                          $"Вы уже вошли в аккаут YouTube: [{user.YoutubeUsername}](https://www.youtube.com/channel/{user.YoutubeChannelId})\n\r" +
                          $"Вы можете выйти из аккаунта YouTube и перестать получать уведомления нажав на кнопку";
                    InlineKeyboardButton logout = new InlineKeyboardButton();
                    logout.Text = "Выйти из аккаунта YouTube";
                    logout.CallbackData = $"l-o~_~{chatId}";
                    InlineKeyboardMarkup markup = new InlineKeyboardMarkup(logout);
                    await Bot.SendTextMessageAsync(chatId, msg, Telegram.Bot.Types.Enums.ParseMode.Markdown, markup);
                }
            } catch (Exception e)
            {
                await Bot.SendDebugMessageAsync(e.Message);
                await Bot.SendDebugMessageAsync(e.StackTrace);
            }

        }
    }
}
