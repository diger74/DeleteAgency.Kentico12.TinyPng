using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.MediaLibrary;
using CMS.SiteProvider;
using Delete.Kentico12.TinyPng.Events;
using Delete.Kentico12.TinyPng.Models;
using Newtonsoft.Json;

namespace Delete.Kentico12.TinyPng
{
    public class TinyPngImageOptimizer
    {
        public static TinyPngImageOptimizerEvents Events = new TinyPngImageOptimizerEvents();

        protected static string ApiKey => SettingsKeyInfoProvider.GetValue("TinyPngApiKey");

        protected static string ShrinkUrl => SettingsKeyInfoProvider.GetValue("TinyPngShrinkUrl");

        protected static bool Enabled => SettingsKeyInfoProvider.GetBoolValue("TinyPngOptimizationEnabled");

        protected static IEnumerable<string> FileExtensions => SettingsKeyInfoProvider.GetValue("TinyPngFileExtensions")
            .Split(new[] {",", ";", "|", " "}, StringSplitOptions.RemoveEmptyEntries);

        public static void Optimize(MediaFileInfo mediaFile)
        {
            if (!Enabled) return;
            var eventArgs = new TinyPngImageOptimizerEventArgs();

            try
            {
                if (!FileExtensions.Contains(mediaFile.FileExtension, StringComparer.OrdinalIgnoreCase)) return;

                eventArgs.MediaFile = mediaFile;
                Events.Before?.Invoke(null, eventArgs);

                // If cancelled in Before event
                if (eventArgs.CancelImageOptimization) return;

                var shrinkRequest = WebRequest.Create(ShrinkUrl);
                var digest = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"api:{ApiKey}"));
                shrinkRequest.Headers.Add("Authorization", $"Basic {digest}");
                shrinkRequest.Method = "POST";

                var fileBinary = GetMediaFileBinary(mediaFile);

                var shrinkRequestStream = shrinkRequest.GetRequestStream();
                shrinkRequestStream.Write(fileBinary, 0, fileBinary.Length);
                shrinkRequestStream.Close();

                var shrinkResponse = shrinkRequest.GetResponse();
                var streamReader = new StreamReader(shrinkResponse.GetResponseStream() ?? Stream.Null);
                var tinyPngShrinkResponse = JsonConvert.DeserializeObject<TinyPngShrinkResponse>(streamReader.ReadToEnd());
                tinyPngShrinkResponse.CompressionCount =
                    ValidationHelper.GetInteger(shrinkResponse.Headers["Compression-Count"], 0);
                eventArgs.ShrinkResponse = tinyPngShrinkResponse;

                var location = shrinkResponse.Headers["Location"];
                var downloadRequest = WebRequest.Create(location);
                downloadRequest.Headers.Add("Authorization", $"Basic {digest}");

                var ms = new MemoryStream();
                var downloadResponse = downloadRequest.GetResponse();
                downloadResponse.GetResponseStream()?.CopyTo(ms);

                if (ms.Length > 0)
                {
                    mediaFile.FileSize = ms.Length;
                    mediaFile.FileBinary = ms.ToArray();

                    eventArgs.ImageOptimizationSuccessful = true;
                    Events.After?.Invoke(null, eventArgs);
                }
                else
                {
                    throw new Exception("TinyPng download response is empty!");
                }
            }
            catch (Exception exception)
            {
                eventArgs.Error = exception;
                Events.Error?.Invoke(null, eventArgs);
            }
        }

        private static byte[] GetMediaFileBinary(MediaFileInfo mediaFile)
        {
            // For files with uploaded binary (new file or update)
            if (mediaFile.FileBinary != null) return mediaFile.FileBinary;

            // For existing files
            var mediaLibrary = MediaLibraryInfoProvider.GetMediaLibraryInfo(mediaFile.FileLibraryID);
            return MediaFileInfoProvider.GetFile(mediaFile, mediaLibrary.LibraryFolder, SiteContext.CurrentSiteName);
        }
    }
}