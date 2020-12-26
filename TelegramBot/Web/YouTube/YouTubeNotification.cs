using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TelegramBot.Models
{
    public class YouTubeNotification
    {
        public string Id { get; set; }
        public string VideoId { get; set; }
        public string ChannelId { get; set; }
        public string Title { get; set; }

        private string link;
        public string Link { get; set; }
        public string AuthorName { get; set; }
        public string AuthorUri { get; set; }

        private string published;
        public DateTime DTpublished { get; private set; }
        public string Published {
            get {
                return published;
            }
            set {
                published = value;
                var date = published.Split('T')[0];
                var time = published.Split('T')[1].Split('+')[0];
                date = date.Replace('-', '/');
                published = date + " " + time;
                DTpublished = DateTime.Parse(published);
            }
        }

        private string updated;
        public DateTime DTupdated { get; private set; }
        public string Updated {
            get {
                return updated;
            }
            set {
                updated = value;
                var date = updated.Split('T')[0];
                var time = updated.Split('T')[1].Split('.')[0];
                date = date.Replace('-', '/');
                updated = date + " " + time;
                DTupdated = DateTime.Parse(updated);
            }
        }

        public bool AlreadySent { get; set; } = false;

        public bool IsNewVideo {
            get {

                return DTupdated != null && DTpublished != null && DTupdated.Subtract(DTpublished) < TimeSpan.FromMinutes(10) && !AlreadySent;
            }
        }

        public DateTime OutdatedAt { get; set; }

        public override String ToString()
        {
            CheckLink();

            return $"Id: {Id}" + "\n\r" +
                    $"VideoId: {VideoId}" + "\n\r" +
                    $"ChannelId: {ChannelId}" + "\n\r" +
                    $"Title: {Title}" + "\n\r" +
                    $"Link: {Link}" + "\n\r" +
                    $"Publsihed: {Published}" + "\n\r" +
                    $"Updated:   {Updated}" + "\n\r" +
                    $"Author.Name: {AuthorName}" + "\n\r" +
                    $"Author.Uri: {AuthorUri}" + "\n\r" +
                    $"IsNewVideo: {IsNewVideo}" + "\n\r" +
                    $"DTUpdated: {DTupdated.ToString()}" + "\n\r" +
                    $"DTPublished: {DTpublished.ToString()}" + "\n\r" +
                    $"updated - published: {DTupdated.Subtract(DTpublished)}" + "\n\r" +
                    $"published - updated: {DTpublished.Subtract(DTupdated)}" + "\n\r" +
                    $"10 min: {TimeSpan.FromMinutes(10)}";
        }

        internal String ToMarkdownString()
        {
            CheckLink();

            /*
                *bold text*
                _italic text_
                [text](URL)
                `inline fixed-width code`
                ```pre-formatted fixed-width code block```
            */

            return $"*Новое видео на канале {AuthorName}*" + "\n\r" +
                   $"[{Title}](https://www.youtube.com/watch?v={VideoId})" + "\n\r" +
                   $"Опубликовано (UTC): {DTpublished.ToString()}";
        }

        internal string ToHtmlString()
        {
            CheckLink();

            /*
                <b>bold</b>, <strong>bold</strong>
                <i>italic</i>, <em>italic</em>
                <a href="URL">inline URL</a>
                <code>inline fixed-width code</code>
                <pre>pre-formatted fixed-width code block</pre>
            */

            return $"<b>Новое видео на канале {AuthorName}</b>" + "\n\r" +
                   $"<a href=\"https://www.youtube.com/watch?v={VideoId}\">{Title}</a>" + "\n\r" +
                   $"Опубликовано (UTC): {DTpublished.ToString()}";
        }

        private void CheckLink()
        {
            var yt = "https://www.youtube.com/";
            if (Link == null | Link.Length < yt.Length | Link.Substring(0, yt.Length) != yt)
            {
                Link = $"https://www.youtube.com/watch?v={VideoId}";
            }
        }
    }

}
