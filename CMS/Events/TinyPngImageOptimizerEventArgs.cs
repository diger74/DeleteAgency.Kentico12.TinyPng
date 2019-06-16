using System;
using CMS.MediaLibrary;
using Delete.Kentico12.TinyPng.Models;

namespace Delete.Kentico12.TinyPng.Events
{
    public class TinyPngImageOptimizerEventArgs : EventArgs
    {
        public MediaFileInfo MediaFile { get; set; }

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