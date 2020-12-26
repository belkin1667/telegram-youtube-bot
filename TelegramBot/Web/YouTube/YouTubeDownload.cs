using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using TelegramBot.Models;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using YoutubeExplode.Converter;
using System.Text.RegularExpressions;
using Tyrrrz.Extensions;
using Microsoft.AspNetCore.Http;
using FFMpegCore;
using FFMpegCore.FFMPEG;
using FFMpegCore.Enums;
using FFMpegCore.FFMPEG.Enums;
using System.Threading;
using NAudio.Wave;

namespace TelegramBot.Downloading
{
    public static class YouTubeDownload
    {
        public static async Task<FileInfo> DownloadAudioAsync(long chatId, ExtendedUser user, String id, String videoTitle)
        {
            //Setting up youtube downloader
            await Bot.SendChatActionAsync(chatId, Telegram.Bot.Types.Enums.ChatAction.UploadDocument);

            YoutubeClient client = new YoutubeClient();


            var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(id);
            var streamInfo = streamInfoSet.Muxed.WithHighestVideoQuality();

            var filesToDelete = new List<FileInfo>();

            var rawFileExtension = streamInfo.Container.GetFileExtension();
            var rawFileName = $"{id}_{user.Id}{chatId}";
            var rawFilePath = Path.Combine(@"D:\home\site\wwwroot\Data\Raw", rawFileName);

            //Downloading file from youtube
            if (!System.IO.File.Exists(rawFilePath))
            {
                using (FileStream fs = new FileStream(rawFilePath, FileMode.Create))
                {
                    await client.DownloadMediaStreamAsync(streamInfo, fs);
                }
            }

            //Convert file to suitable file format
            var convertedfileExtension = "mp3";
            var convertedFileName = $"{rawFileName}.{convertedfileExtension}";
            var convertedFilePath = Path.Combine(@"D:\home\site\wwwroot\Data\Converted", convertedFileName);

            await Bot.SendChatActionAsync(chatId, Telegram.Bot.Types.Enums.ChatAction.UploadDocument);

            FileInfo info = null;
            try
            {

                if (System.IO.File.Exists(rawFilePath) && !System.IO.File.Exists(convertedFilePath))
                {
                    info = await ConvertAudio(rawFilePath, convertedFilePath);

                    long fileSize = info.Length;
                    const long maxFileSize = 50 * 1048576; //Max file size telegram bot can send in one message is 50 MegaByte = 50 * 1048576 Byte

                    //Целочисленное деление с округлением вверх
                    long amountOfFiles = fileSize / maxFileSize;
                    if (fileSize % maxFileSize != 0)
                        amountOfFiles++;

                    double duration = GetMediaDuration(convertedFilePath);
                    double splittedFileDuration = duration / amountOfFiles;

                    await Bot.SendChatActionAsync(chatId, Telegram.Bot.Types.Enums.ChatAction.UploadDocument);

                    var filesToPublish = new List<String>();
                    if (amountOfFiles > 1)
                    {
                        var mp3Dir = Path.GetDirectoryName(convertedFilePath);
                        for (int i = 0; i < amountOfFiles; i++)
                        {
                            var splittedFilePath = Path.Combine(mp3Dir, Path.GetFileNameWithoutExtension(convertedFilePath) + $"_split{i + 1}.mp3");
                            filesToPublish.Add(splittedFilePath);
                            TimeSpan start = TimeSpan.FromSeconds(splittedFileDuration * i);
                            TimeSpan end = TimeSpan.FromSeconds(splittedFileDuration * (i + 1));
                            if (i + 1 == amountOfFiles)
                                end = TimeSpan.FromSeconds(duration);

                            TrimMp3(convertedFilePath, splittedFilePath, start, end);
                        }
                    }
                    else
                    {
                        filesToPublish.Add(convertedFilePath);
                    }

                    var title = videoTitle;
                    int counter = 1;

                    await Bot.SendChatActionAsync(chatId, Telegram.Bot.Types.Enums.ChatAction.UploadDocument);
                    foreach (var path in filesToPublish)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            using (FileStream fs = new FileStream(path, FileMode.Open))
                            {
                                InputOnlineFile file = new InputOnlineFile(fs);
                                var fileNumber = "";
                                if (counter > 1 || filesToPublish.Count() > 1)
                                    fileNumber = "Часть " + counter.ToString();
                                else
                                    fileNumber = null;

                                await Bot.SendAudioAsync(chatId, file, fileNumber, title);
                            }
                            counter++;
                        }
                    }

                    //TODO: Add Key-value pair of youtube video id and telegram files ids to DATABASE

                    var convertedDir = @"D:\home\site\wwwroot\Data\Converted";
                    var rawDir = @"D:\home\site\wwwroot\Data\Raw";
                    filesToDelete.AddRange(new System.IO.DirectoryInfo(convertedDir).GetFiles());
                    filesToDelete.AddRange(new System.IO.DirectoryInfo(rawDir).GetFiles());

                    foreach (var file in filesToDelete)
                    {
                        var path = file.FullName;
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                await Bot.SendDebugMessageAsync(e.Message);
                await Bot.SendDebugMessageAsync(e.StackTrace);
            }
            return info;
        }

        //Cred: https://stackoverflow.com/questions/383164/how-to-retrieve-duration-of-mp3-in-net/13269914#13269914
        public static double GetMediaDuration(string path)
        {
            double duration = 0.0;
            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(fs);
                if (frame != null)
                {
                    var _sampleFrequency = (uint)frame.SampleRate;
                }
                while (frame != null)
                {
                    duration += (double)frame.SampleCount / (double)frame.SampleRate;
                    frame = Mp3Frame.LoadFromStream(fs);
                }
            }
            return duration;
        }

        internal static async Task<FileInfo> ConvertAudio(string rawFilePath, string convertedFilePath)
        {
            //await Bot.SendDebugMessageAsync("START OF ConvertAudioAsync");
            FFMpegOptions.Configure(new FFMpegOptions { RootDirectory = @"D:\home\site\wwwroot" });
            FFMpeg encoder = new FFMpeg();
            FileInfo info = encoder.ExtractAudio(
                VideoInfo.FromPath(rawFilePath),
                new FileInfo(convertedFilePath)
            );
            //await Bot.SendDebugMessageAsync("END OF ConvertAudioAsync");
            return info;
        }

        //Cred: http://stackoverflow.com/a/14169073/64334
        internal static void TrimMp3(string inputPath, string outputPath, TimeSpan? begin, TimeSpan? end)
        {
            if (begin.HasValue && end.HasValue && begin > end)
                throw new ArgumentOutOfRangeException("end", "end should be greater than begin");

            using (var reader = new Mp3FileReader(inputPath))
            using (var writer = System.IO.File.Create(outputPath))
            {
                Mp3Frame frame;
                while ((frame = reader.ReadNextFrame()) != null)
                    if (reader.CurrentTime >= begin || !begin.HasValue)
                    {
                        if (reader.CurrentTime <= end || !end.HasValue)
                            writer.Write(frame.RawData, 0, frame.RawData.Length);
                        else break;
                    }
            }
        }

        // Methods are unavaliable due to telegram policy of sending files with bots
        // Bots can send files less than 50MB
        // 20 seconds mp4 video is around 50MB 
        // As soon as telegram update their policy these methods can be used
        public static async Task<VideoInfo> DownloadVideoAsync(Int64 chatId, String id)
        {
            //Setting up youtube downloader
            await Bot.SendChatActionAsync(chatId, Telegram.Bot.Types.Enums.ChatAction.UploadVideo);
            YoutubeClient client = new YoutubeClient();
            var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(id);
            var streamInfo = streamInfoSet.Muxed.WithHighestVideoQuality();

            var pathsToDelete = new List<String>();

            var rawFileExtension = streamInfo.Container.GetFileExtension();
            var rawFileName = $"{id}";
            var rawFilePath = Path.Combine(@"D:\home\site\wwwroot\Data\Raw", rawFileName);
            pathsToDelete.Add(rawFilePath);

            //Downloading file from youtube
            using (FileStream fs = new FileStream(rawFilePath, FileMode.Create))
            {
                await client.DownloadMediaStreamAsync(streamInfo, fs);
            }

            //Convert file to suitable file format
            var convertedfileExtension = "mp4";
            var convertedFileName = $"{id}.{convertedfileExtension}";
            var convertedFilePath = Path.Combine(@"D:\home\site\wwwroot\Data\Converted", convertedFileName);
            pathsToDelete.Add(convertedFilePath);

            VideoInfo info = null;


            try
            {
                if (!System.IO.File.Exists(convertedFilePath))
                {
                    info = await ConvertVideo(rawFilePath, convertedFilePath);

                    using (FileStream fs = new FileStream(convertedFilePath, FileMode.Open))
                    {
                        InputOnlineFile file = new InputOnlineFile(fs);

                        await Bot.SendVideoAsync(chatId, file);
                    }

                }

                foreach (var path in pathsToDelete)
                {

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                }

            }
            catch (Exception e)
            {
                await Bot.SendDebugMessageAsync(e.Message);
                await Bot.SendDebugMessageAsync(e.StackTrace);
            }
            return info;
        }
        static async Task<VideoInfo> ConvertVideo(string rawFilePath, string convertedFilePath)
        {
            FFMpegOptions.Configure(new FFMpegOptions { RootDirectory = @"D:\home\site\wwwroot" });
            FFMpeg encoder = new FFMpeg();
            VideoInfo info = encoder.Convert(
                VideoInfo.FromPath(rawFilePath),
                new FileInfo(convertedFilePath),
                VideoType.Mp4,
                Speed.UltraFast,
                VideoSize.Original,
                AudioQuality.Hd
            );
            return info;
        }

    }
}
