using CMS;
using CMS.DataEngine;
using CMS.EventLog;
using CMS.MediaLibrary;
using Delete.Kentico12.TinyPng;

[assembly: RegisterModule(typeof(TinyPngModule))]
namespace Delete.Kentico12.TinyPng
{
    public class TinyPngModule : Module
    {
        public TinyPngModule() : base("Delete.Kentico12.TinyPng", true)
        {
        }

        protected override void OnInit()
        {
            base.OnInit();

            MediaFileInfo.TYPEINFO.Events.Insert.Before += UpdateOnBefore;
            MediaFileInfo.TYPEINFO.Events.Update.Before += UpdateOnBefore;

            EventLogProvider.LogInformation("TinyPNG", "MODULESTART");
        }

        private void UpdateOnBefore(object sender, ObjectEventArgs e)
        {
            if (e.Object == null) return;

            if (e.Object is MediaFileInfo image)
            {
                TinyPngImageOptimizer.Optimize(image);
            }
        }
    }
}