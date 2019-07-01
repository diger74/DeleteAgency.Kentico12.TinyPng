[![Nuget](https://img.shields.io/badge/nuget-v1.1.0-blue.svg)](https://www.nuget.org/packages/Delete.Kentico12.TinyPng/)

# Delete.Kentico12.TinyPng
Automatically shrink images using TinyPNG API during uploading to Media Library or as a Page Attachment. Applicable for Kentico 12 CMS (including Service Pack).

# Usage
## Installation
1. Install Nuget package https://www.nuget.org/packages/Delete.Kentico12.TinyPng/ into Kentico 12 CMS solution. If your project is MVC, you should install this package **for both CMS and MVC solutions**!
2. Build and run projects for the first time, it will trigger installation of **TinyPNG Image Optimization** module.

## Configuration
1. Go to **Settings application -> Integration -> TinyPNG** (section should appear after successful module installation)
2. Tick **Enabled** and fill your **Api Key**. That's it! From now on all your images uploaded to Media Libraries or Attachments will be optimized!

![TinyPNG package settings](https://i.imgur.com/iDcaCiD.png)

You can refer to detailed blog HowTo article: https://diger74.net/image-optimization-using-tinypng-api
