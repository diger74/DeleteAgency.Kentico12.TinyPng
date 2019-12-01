using System.Linq;
using CMS;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.EventLog;
using CMS.MediaLibrary;
using CMS.SiteProvider;
using DeleteAgency.Kentico12.TinyPng;

[assembly: RegisterModule(typeof(TinyPngModule))]
namespace DeleteAgency.Kentico12.TinyPng
{
    public class TinyPngModule : Module
    {
        public TinyPngModule() : base("DeleteAgency.Kentico12.TinyPng", true)
        {
        }

        protected override void OnInit()
        {
            base.OnInit();

            MediaFileInfo.TYPEINFO.Events.Insert.Before += MediaFileOnBeforeSave;
            MediaFileInfo.TYPEINFO.Events.Update.Before += MediaFileOnBeforeSave;

            AttachmentInfo.TYPEINFO.Events.Insert.Before += AttachmentOnBeforeSave;
            AttachmentInfo.TYPEINFO.Events.Update.Before += AttachmentOnBeforeSave;

            AttachmentHistoryInfo.TYPEINFO.Events.Insert.Before += AttachmentOnBeforeSave;

            EventLogProvider.LogInformation("TinyPNG", "MODULESTART");
        }

        private void AttachmentOnBeforeSave(object sender, ObjectEventArgs e)
        {
            if (e.Object == null) return;

            // If workflow enabled
            if (e.Object is AttachmentHistoryInfo attachmentVersion)
            {
                var latestAttachmentVersion = AttachmentHistoryInfoProvider.GetAttachmentHistories()
                    .WhereEquals("AttachmentGUID", attachmentVersion.AttachmentGUID)
                    .OrderByDescending("AttachmentLastModified")
                    .TopN(1)
                    .FirstOrDefault();

                if (latestAttachmentVersion == null ||
                    latestAttachmentVersion.AttachmentSize != attachmentVersion.AttachmentSize)
                {
                    var optimizer = new TinyPngImageOptimizer(SiteContext.CurrentSiteName);
                    optimizer.Optimize(attachmentVersion);
                }
            }

            // If workflow disabled
            if (e.Object is AttachmentInfo attachment)
            {
                var document = DocumentHelper.GetDocument(attachment.AttachmentDocumentID, new TreeProvider());
                
                if (document.WorkflowStep == null)
                {
                    var currentAttachment = AttachmentInfoProvider.GetAttachmentInfo(attachment.AttachmentID, true);

                    if (currentAttachment == null || currentAttachment.AttachmentSize != attachment.AttachmentSize)
                    {
                        var optimizer = new TinyPngImageOptimizer(SiteContext.CurrentSiteName);
                        optimizer.Optimize(attachment);
                    }
                }
            }
        }

        private void MediaFileOnBeforeSave(object sender, ObjectEventArgs e)
        {
            if (e.Object == null) return;

            if (e.Object is MediaFileInfo image)
            {
                var optimizer = new TinyPngImageOptimizer(SiteContext.CurrentSiteName);
                optimizer.Optimize(image);
            }
        }
    }
}