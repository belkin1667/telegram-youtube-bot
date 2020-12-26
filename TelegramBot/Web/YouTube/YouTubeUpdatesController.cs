using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Models;
using TelegramBot.Models.Commands;

namespace TelegramBot.Controllers
{


    [Route("webhook/youtube/updates/{state?}")]
    public class YouTubeUpdatesController : Controller
    {

        //GET
        [HttpGet]
        public async Task<ObjectResult> GetAsync()
        {
            if (Request.Query.ContainsKey("hub.challenge"))
            {
                string challenge = Request.Query["hub.challenge"];

                return Ok(challenge); //verify the subscribtion request
            }
            else
            {
                await Bot.SendDebugMessageAsync("BadRequest at YouTubeUpdatesController.Get()");

                return BadRequest("");
            }

        }


        //POST
        [HttpPost]
        public async Task<StatusCodeResult> PostAsync()
        {
            RouteData route = ControllerContext.RouteData;

            if (route.Values.ContainsKey("state"))
            {
                var state = route.Values.GetValueOrDefault("state") as String;
                var userId = state.Split('_')[0];
                var chatId = state.Split('_')[1];
                var user = Bot.GetUserById(userId);

                string xml = "";
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    xml = await reader.ReadToEndAsync();
                }

                YouTubeNotification notification;
                DateTime published = new DateTime(DateTime.Now.Year - 1, DateTime.Now.Month, DateTime.Now.Day);
                try
                {
                    if (xml == "")
                    {
                        throw new Exception("Xml is empty");
                    }

                    notification = parseXml(xml);

                    // если уже отправляли - не отправляем еще раз
                    // если вышло время - удаляем
                    foreach (YouTubeNotification lastNotification in user.ChatsAndLastNotifications.GetValueOrDefault(chatId).ToList())
                    {
                        if (lastNotification.VideoId == notification.VideoId)
                        {
                            notification.AlreadySent = true;
                        }
                        if (lastNotification.OutdatedAt < DateTime.Now)
                        {
                            user.ChatsAndLastNotifications.GetValueOrDefault(chatId).Remove(lastNotification);
                        }
                    }

                    if (notification.IsNewVideo)
                    {
                        var text = notification.ToMarkdownString();
                        long id;
                        long.TryParse(chatId, out id);
                        var markup = await ButtonController.GetMarkupAsync(id, notification.VideoId, true, "n");
                        await Bot.SendTextMessageAsync(id, text, Telegram.Bot.Types.Enums.ParseMode.Markdown, markup);

                        notification.OutdatedAt = DateTime.Now + TimeSpan.FromMinutes(10);
                        user.ChatsAndLastNotifications.GetValueOrDefault(chatId).Add(notification);
                    }

                }
                catch (Exception ex)
                {
                    //await Bot.SendDebugMessageAsync(ex.Message);
                    //await Bot.SendDebugMessageAsync(ex.StackTrace);
                    //await Bot.SendDebugMessageAsync(xml);
                }

                return Ok();
            }

            await Bot.SendDebugMessageAsync("BadRequest at YouTubeUpdatesController.Post()");

            return BadRequest();
        }

        private YouTubeNotification parseXml(String xml)
        {
            YouTubeNotification notification = new YouTubeNotification();

            try
            {

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                XmlElement root = doc.DocumentElement;

                XmlNode entry = null;

                foreach (XmlNode node in root)
                {
                    if (node.Name == "entry")
                    {
                        entry = node;
                        break;
                    }
                }
                if (entry != null)
                {
                    foreach (XmlNode node in entry)
                    {
                        switch (node.Name)
                        {
                            case "id":
                                notification.Id = node.InnerText;
                                break;
                            case "yt:videoId":
                                notification.VideoId = node.InnerText;
                                break;
                            case "yt:channelId":
                                notification.ChannelId = node.InnerText;
                                break;
                            case "title":
                                notification.Title = node.InnerText;
                                break;
                            case "link":
                                notification.Link = node.Attributes.Item(1).InnerText;
                                break;
                            case "published":
                                notification.Published = node.InnerText;
                                break;
                            case "updated":
                                notification.Updated = node.InnerText;
                                break;
                            case "author":
                                foreach (XmlNode innerNode in node.ChildNodes)
                                {
                                    switch (innerNode.Name)
                                    {
                                        case "name":
                                            notification.AuthorName = innerNode.InnerText;
                                            break;
                                        case "uri":
                                            notification.AuthorUri = innerNode.InnerText;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //Bot.SendDebugMessage(e.Message);
                //Bot.SendDebugMessage(e.StackTrace);
            }
            return notification;

        }
    }
}
