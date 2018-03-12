using System;
using TemplatesShared;
using Xamarin.Forms;

namespace DotnetNewMobile.ViewModels
{
    public class TemplatePackViewModel
    {
        private TemplatePack Pack{
            get;set;
        }

        public TemplatePackViewModel(TemplatePack pack){
            Pack = pack;
        }

        public string DownloadString
        {
            get{
                return Pack != null ? $"Downloads: {Pack.DownloadCount}" : string.Empty;
            }
        }
   
        public string PackageString{
            get{
                return Pack != null? Pack.Package: "";
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
    }
}

