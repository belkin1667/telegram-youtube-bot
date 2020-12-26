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
    public static class InlineModeController
    {
        public static async Task Search(Update update)
        {
            try
            {
                var userId = update.InlineQuery.From.Id.ToString();
                var user = Bot.GetUserById(userId);
                var query = update.InlineQuery.Query;
                var queryId = update.InlineQuery.Id;

                IReadOnlyList<YoutubeExplode.Models.Video> videos = new List<YoutubeExplode.Models.Video>();
                if (query.Split(' ')[0] == "videoId:")
                {
                    try
                    {
                        videos = new List<YoutubeExplode.Models.Video>()
                        {
                            await new YoutubeClient().GetVideoAsync(query.Split(' ')[1])
                        };
                    }
                    catch
                    {
                        //Do nothing as it's normal to have an exception here, just wait for another request
                    }

                }
                else
                {
                    videos = await new YoutubeClient().SearchVideosAsync(query, 1);
                }
                try
                {
                    if (videos.Count > 0)
                        await Bot.AnswerInlineQueryAsync(user, queryId, videos);
                } catch
                {
                    //Do nothing as it's normal to have an exception here, just wait for another request
                }
            }
            catch (Exception e)
            {
                await Bot.SendDebugMessageAsync(e.Message);
                await Bot.SendDebugMessageAsync(e.StackTrace);
            }

        }

    }
}
