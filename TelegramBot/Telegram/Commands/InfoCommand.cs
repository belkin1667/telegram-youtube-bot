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
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Downloading;

namespace TelegramBot.Models.Commands
{
    public class InfoCommand : Command
    {
        public override string Name => @"/info";

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
                var text =  "С помощью этого бота вы можете\n\r" +
                            "1. Получать уведомления о новых видео от каналов на которые вы подписаны в YouTube\n\r" +
                            "2. Оценивать видео\n\r" +
                            "3. Скачивать аудио из YouTube\n\r" +
                            "4. Искать видео в YouTube и делиться ими в Telegram";
                var text2 = "Доступные команды:\n\r" +
                            "1. /start - включить бота\n\r" +
                            "2. /login и /logout - войти и выйти из аккаунта Google\n\r" +
                            "3. /info - информация о боте";



                await Bot.SendTextMessageAsync(chatId, text);
                await Bot.SendTextMessageAsync(chatId, text2);

            }
            catch (Exception e)
            {
                await Bot.SendDebugMessageAsync(e.Message);
                await Bot.SendDebugMessageAsync(e.StackTrace);
            }
        }
        

    }
}
