using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Models.Commands
{
    public class LoginCommand : Command
    {
        public override String Name => @"/login";

        public override Boolean Contains(Message message)
        {
            //Сделать нормально, без повторов кода
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return message.Text.Contains(this.Name);
        }

        public override async Task Execute(Message message, Update update)
        {
            var userId = message.From.Id.ToString();
            var user = Bot.GetUserById(userId);
            var msg = $"Войдите в ваш аккаунт YouTube, чтобы получать уведомления о новых видео" + $"\n\r" +
                      $"Для получения справки отправьте команду /info";
            var chatId = message.Chat.Id;
            if (user?.Credential == null)
            {
                InlineKeyboardButton login = new InlineKeyboardButton();
                login.Text = "Войти в аккаунт YouTube";
                login.Url = $"{AppSettings.YouTubeAuthUrl}&state={userId}_{chatId}";
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(login);
                await Bot.SendTextMessageAsync(chatId, msg, Telegram.Bot.Types.Enums.ParseMode.Default, markup);
            } else
            {
                await Bot.SendTextMessageAsync(chatId, "Вы можете войти в аккаунт. Сначала выйдите из текущего аккаунта");
                await new LogoutCommand().Execute(message, update);
            }
        }
    }
}
