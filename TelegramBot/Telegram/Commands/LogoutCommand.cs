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
    public class LogoutCommand : Command
    {
        public override String Name => @"/logout";

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
            var msg = $"Добро пожаловать, @{user.Username}\n\r" +
                      $"Нажмите кнопку \"Выйти\" для выхода из аккаутна YouTube: [{user.YoutubeUsername}](https://www.youtube.com/channel/{user.YoutubeChannelId})\n\r" +
                      $"Выйдя из аккаута вы перестанете получать уведомления о новых видео";
            var chatId = message.Chat.Id;
            if (user?.Credential != null)
            {
                
                InlineKeyboardButton logout = new InlineKeyboardButton();
                logout.Text = "Выйти";
                logout.CallbackData = $"l-o~_~{chatId}";
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(logout);
                await Bot.SendTextMessageAsync(chatId, msg, Telegram.Bot.Types.Enums.ParseMode.Markdown, markup);
            } else
            {
                await Bot.SendTextMessageAsync(chatId, "Вы не можете выйти из аккаунта т.к. вы еще не вошли в аккаунт");
                await new LoginCommand().Execute(message, update);
            }
        }
    }
}
