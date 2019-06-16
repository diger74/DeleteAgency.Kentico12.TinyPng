using System;
using System.Collections.Generic;
using CMS;
using CMS.DataEngine;
using CMS.Modules;
using CMSApp.CMSModules;
using NuGet;

[assembly: RegisterModule(typeof(TinyPngModuleGeneration))]
namespace CMSApp.CMSModules
{
    public class TinyPngModuleGeneration : Module
    {
        public TinyPngModuleGeneration() : base("TinyPngModuleGeneration", false)
        {
            ModulePackagingEvents.Instance.BuildNuSpecManifest.After += BuildNuSpecManifestOnAfter;
        }

        private void BuildNuSpecManifestOnAfter(object sender, BuildNuSpecManifestEventArgs e)
        {
            if (!e.ResourceName.Equals("Delete.Kentico12.TinyPng", StringComparison.OrdinalIgnoreCase)) return;

            e.Manifest.Metadata.DependencySets = new List<ManifestDependencySet>
            {
                new ManifestDependencySet
                {
                    Dependencies = new List<ManifestDependency>
                    {
                        new ManifestDependency
                        {
                            Id = "Newtonsoft.Json", Version = "11.0.2"
                        }
                    }
                }
            };
            e.Manifest.Metadata.Owners = "Delete Agency";
            e.Manifest.Metadata.Tags = "Kentico, TinyPNG, image optimization";
        }
    }
}