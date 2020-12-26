using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Models;
using TelegramBot.Models.Commands;

namespace TelegramBot.Controllers
{
    [Route("api/message/update")]
    public class MessageController : Controller
    {

        //GET
        [HttpGet]
        public string Get()
        {
            return AppSettings.AppInfo;
        }


        //POST
        [HttpPost]
        public async Task<OkResult> Post([FromBody]Update update)
        {
            if (update == null) return Ok();
            var commands = Bot.Commands;

            switch (update.Type)
            {
                case UpdateType.CallbackQuery:
                    await ButtonController.Execute(update);
                    break;
                case UpdateType.Message:
                    var message = update.Message;
                    foreach (var command in commands)
                    {
                        if (command.Contains(message))
                        {
                            await command.Execute(message, update);
                            break;
                        }
                    }
                    break;
                case UpdateType.InlineQuery:
                    await InlineModeController.Search(update);
                    break;

            }

            return Ok();
        }

    }
}
