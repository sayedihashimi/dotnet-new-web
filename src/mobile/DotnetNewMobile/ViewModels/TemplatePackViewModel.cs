using System;
using TemplatesShared;
using Xamarin.Forms;

namespace DotnetNewMobile.ViewModels
{
    public class TemplatePackViewModel
    {
        public TemplatePack Pack{
            get; private set;
        }

        public TemplatePackViewModel(TemplatePack pack){
            Pack = pack;
        }

        public string DownloadCount
        {
            get{
                return Pack != null ? Pack.DownloadCount.ToString() : string.Empty;
            }
        }
   
        public string PackageString{
            get{
                return Pack != null? Pack.Owners: "";
            }
        }

        public string NumTemplatesString{
            get{
                return Pack != null ? $"{Pack.Templates.Length} templates" : string.Empty;
            }
        }

        public string OwnerString{
            get{
                return Pack != null ? $"By {Pack.Owners}": string.Empty;
            }
        }

        public string IconPngUrl{
            get{
                return Pack != null ? Pack.IconPngUrl : string.Empty;
            }
        }

        public string Description
        {
            get
            {
                return Pack != null ? Pack.Description : string.Empty;
            }
        }
        public string LicenseUrl
        {
            get
            {
                return Pack != null ? Pack.LicenseUrl : string.Empty;
            }
        }
        public string ProjectUrl
        {
            get
            {
                return Pack != null ? Pack.ProjectUrl : string.Empty;
            }
        }
        public string NuGetUrl
        {
            get
            {
                return Pack != null ? $"http://dotnetnew.azurewebsites.net/pack/{Pack.Package}" : string.Empty;
            }
        }
    }
}

