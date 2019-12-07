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
        protected bool EnabledMetaFiles => SettingsKeyInfoProvider.GetBoolValue("TinyPngOptimizeMetaFiles", _siteName);
        protected IEnumerable<string> FileExtensions => SettingsKeyInfoProvider.GetValue("TinyPngFileExtensions", _siteName)
            .Split(new[] {",", ";", "|", " "}, StringSplitOptions.RemoveEmptyEntries);

        private string Digest => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"api:{ApiKey}"));
        private readonly string _siteName;

        public TinyPngImageOptimizer(string siteName)
        {
            _siteName = siteName;
        }

        public void Optimize(BaseInfo image)
        {
            if (!Enabled) return;

            string fileExtension;
            switch (image)
            {
                case MediaFileInfo mediaFile:
                    if (!EnabledMediaFiles) return;
                    fileExtension = mediaFile.FileExtension;
                    break;

                case MetaFileInfo metaFile:
                    if (!EnabledMetaFiles) return;
                    fileExtension = metaFile.MetaFileExtension;
                    break;

                case AttachmentInfo attachment:
                    if (!EnabledPageAttachments) return;
                    fileExtension = attachment.AttachmentExtension;
                    break;

                case AttachmentHistoryInfo attachmentHistory:
                    if (!EnabledPageAttachments) return;
                    fileExtension = attachmentHistory.AttachmentExtension;
                    break;

                default:
                    return;
            }
            if (!FileExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase)) return;

            var eventArgs = new TinyPngImageOptimizerEventArgs();

            try
            {
                eventArgs.Image = image;
                Events.Before?.Invoke(null, eventArgs);

                // If cancelled in Before event
                if (eventArgs.CancelImageOptimization) return;

                var fileBinary = GetFileBinary(image);

                var shrinkResponse = OptimizeFileBinary(fileBinary);
                eventArgs.ShrinkResponse = shrinkResponse;

                var ms = DownloadOptimizedImage(shrinkResponse.Location);
                if (ms.Length > 0)
                {
                    SaveOptimized(image, ms);

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

        private void SaveOptimized(BaseInfo image, MemoryStream ms)
        {
            switch (image)
            {
                case MediaFileInfo mediaFile:
                    mediaFile.FileSize = ms.Length;
                    mediaFile.FileBinary = ms.ToArray();
                    break;

                case MetaFileInfo metaFile:
                    metaFile.MetaFileSize = ValidationHelper.GetInteger(ms.Length, 0);
                    metaFile.MetaFileBinary = ms.ToArray();
                    break;

                case AttachmentInfo attachment:
                    attachment.AttachmentSize = Convert.ToInt32(ms.Length);
                    attachment.AttachmentBinary = ms.ToArray();
                    break;

                case AttachmentHistoryInfo attachmentHistory:
                    attachmentHistory.AttachmentSize = Convert.ToInt32(ms.Length);
                    attachmentHistory.AttachmentBinary = ms.ToArray();
                    break;
            }
        }

        private byte[] GetFileBinary(BaseInfo image)
        {
            switch (image)
            {
                case MediaFileInfo mediaFile:
                    // For files with uploaded binary (new file or update)
                    if (mediaFile.FileBinary != null) return mediaFile.FileBinary;
                    // For existing files
                    var mediaLibrary = MediaLibraryInfoProvider.GetMediaLibraryInfo(mediaFile.FileLibraryID);
                    return MediaFileInfoProvider.GetFile(mediaFile, mediaLibrary.LibraryFolder, SiteContext.CurrentSiteName);

                case MetaFileInfo metaFile:
                    // For files with uploaded binary (new file or update)
                    if (metaFile.MetaFileBinary != null) return metaFile.MetaFileBinary;
                    // For existing files
                    return MetaFileInfoProvider.GetFile(metaFile, SiteContext.CurrentSiteName);

                case AttachmentInfo attachment:
                    return attachment.AttachmentBinary;

                case AttachmentHistoryInfo attachmentHistory:
                    return attachmentHistory.AttachmentBinary;

                default:
                    return null;
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
    }
}