using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.MediaLibrary;
using CMS.SiteProvider;
using DeleteAgency.Kentico12.TinyPng.Events;
using DeleteAgency.Kentico12.TinyPng.Models;
using Newtonsoft.Json;

namespace DeleteAgency.Kentico12.TinyPng
{
    public class TinyPngImageOptimizer
    {
        public static TinyPngImageOptimizerEvents Events = new TinyPngImageOptimizerEvents();
        
        protected string ApiKey => SettingsKeyInfoProvider.GetValue("TinyPngApiKey", _siteName);
        protected string ShrinkUrl => SettingsKeyInfoProvider.GetValue("TinyPngShrinkUrl", _siteName);
        protected bool Enabled => SettingsKeyInfoProvider.GetBoolValue("TinyPngOptimizationEnabled", _siteName);
        protected bool EnabledMediaFiles => SettingsKeyInfoProvider.GetBoolValue("TinyPngOptimizeMediaFiles", _siteName);
        protected bool EnabledPageAttachments => SettingsKeyInfoProvider.GetBoolValue("TinyPngOptimizePageAttachments", _siteName);
        protected IEnumerable<string> FileExtensions => SettingsKeyInfoProvider.GetValue("TinyPngFileExtensions", _siteName)
            .Split(new[] {",", ";", "|", " "}, StringSplitOptions.RemoveEmptyEntries);

        private string Digest => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"api:{ApiKey}"));
        private readonly string _siteName;

        public TinyPngImageOptimizer(string siteName)
        {
            _siteName = siteName;
        }

        public void Optimize(MediaFileInfo mediaFile)
        {
            if (!Enabled || !EnabledMediaFiles ||
                !FileExtensions.Contains(mediaFile.FileExtension, StringComparer.OrdinalIgnoreCase)) return;
            var eventArgs = new TinyPngImageOptimizerEventArgs();

            try
            {
                eventArgs.MediaFile = mediaFile;
                Events.Before?.Invoke(null, eventArgs);

                // If cancelled in Before event
                if (eventArgs.CancelImageOptimization) return;

                var fileBinary = GetMediaFileBinary(mediaFile);

                var shrinkResponse = OptimizeFileBinary(fileBinary);
                eventArgs.ShrinkResponse = shrinkResponse;

                var ms = DownloadOptimizedImage(shrinkResponse.Location);
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

        public void Optimize(AttachmentHistoryInfo pageAttachment)
        {
            if (!Enabled || !EnabledPageAttachments ||
                !FileExtensions.Contains(pageAttachment.AttachmentExtension, StringComparer.OrdinalIgnoreCase)) return;
            var eventArgs = new TinyPngImageOptimizerEventArgs();

            try
            {
                eventArgs.PageAttachmentVersion = pageAttachment;
                Events.Before?.Invoke(null, eventArgs);

                // If cancelled in Before event
                if (eventArgs.CancelImageOptimization) return;

                var fileBinary = pageAttachment.AttachmentBinary;

                var shrinkResponse = OptimizeFileBinary(fileBinary);
                eventArgs.ShrinkResponse = shrinkResponse;

                var ms = DownloadOptimizedImage(shrinkResponse.Location);
                if (ms.Length > 0)
                {
                    pageAttachment.AttachmentSize = Convert.ToInt32(ms.Length);
                    pageAttachment.AttachmentBinary = ms.ToArray();

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

        public void Optimize(AttachmentInfo pageAttachment)
        {
            if (!Enabled || !EnabledPageAttachments ||
                !FileExtensions.Contains(pageAttachment.AttachmentExtension, StringComparer.OrdinalIgnoreCase)) return;
            var eventArgs = new TinyPngImageOptimizerEventArgs();

            try
            {
                eventArgs.PageAttachment = pageAttachment;
                Events.Before?.Invoke(null, eventArgs);

                // If cancelled in Before event
                if (eventArgs.CancelImageOptimization) return;

                var fileBinary = pageAttachment.AttachmentBinary;

                var shrinkResponse = OptimizeFileBinary(fileBinary);
                eventArgs.ShrinkResponse = shrinkResponse;

                var ms = DownloadOptimizedImage(shrinkResponse.Location);
                if (ms.Length > 0)
                {
                    pageAttachment.AttachmentSize = Convert.ToInt32(ms.Length);
                    pageAttachment.AttachmentBinary = ms.ToArray();

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

        private TinyPngShrinkResponse OptimizeFileBinary(byte[] fileBinary)
        {
            var shrinkRequest = WebRequest.Create(ShrinkUrl);
            shrinkRequest.Headers.Add("Authorization", $"Basic {Digest}");
            shrinkRequest.Method = "POST";

            var shrinkRequestStream = shrinkRequest.GetRequestStream();
            shrinkRequestStream.Write(fileBinary, 0, fileBinary.Length);
            shrinkRequestStream.Close();

            var shrinkResponse = shrinkRequest.GetResponse();
            var streamReader = new StreamReader(shrinkResponse.GetResponseStream() ?? Stream.Null);
            var tinyPngShrinkResponse = JsonConvert.DeserializeObject<TinyPngShrinkResponse>(streamReader.ReadToEnd());
            tinyPngShrinkResponse.CompressionCount =
                ValidationHelper.GetInteger(shrinkResponse.Headers["Compression-Count"], 0);

            tinyPngShrinkResponse.Location = shrinkResponse.Headers["Location"];

            return tinyPngShrinkResponse;
        }

        private MemoryStream DownloadOptimizedImage(string location)
        {
            var downloadRequest = WebRequest.Create(location);
            downloadRequest.Headers.Add("Authorization", $"Basic {Digest}");

            var ms = new MemoryStream();
            var downloadResponse = downloadRequest.GetResponse();
            downloadResponse.GetResponseStream()?.CopyTo(ms);

            return ms;
        }

        private byte[] GetMediaFileBinary(MediaFileInfo mediaFile)
        {
            // For files with uploaded binary (new file or update)
            if (mediaFile.FileBinary != null) return mediaFile.FileBinary;

            // For existing files
            var mediaLibrary = MediaLibraryInfoProvider.GetMediaLibraryInfo(mediaFile.FileLibraryID);
            return MediaFileInfoProvider.GetFile(mediaFile, mediaLibrary.LibraryFolder, SiteContext.CurrentSiteName);
        }
    }
}