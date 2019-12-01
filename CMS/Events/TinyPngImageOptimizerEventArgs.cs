using System;
using CMS.DocumentEngine;
using CMS.MediaLibrary;
using DeleteAgency.Kentico12.TinyPng.Models;

namespace DeleteAgency.Kentico12.TinyPng.Events
{
    public class TinyPngImageOptimizerEventArgs : EventArgs
    {
        public MediaFileInfo MediaFile { get; set; }

        public AttachmentHistoryInfo PageAttachmentVersion { get; set; }

        public AttachmentInfo PageAttachment { get; set; }

        public TinyPngShrinkResponse ShrinkResponse { get; set; }

        public bool CancelImageOptimization { get; set; }

        public bool ImageOptimizationSuccessful { get; set; }

        public Exception Error { get; set; }

        public TinyPngImageOptimizerEventArgs()
        {
            ImageOptimizationSuccessful = false;
            CancelImageOptimization = false;
        }
    }
}