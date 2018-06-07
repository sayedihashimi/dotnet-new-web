using System;
using System.Collections;
using System.Windows.Input;
using System.Linq;
using Plugin.Share;
using Plugin.Share.Abstractions;
using TemplatesShared;
using Xamarin.Forms;
using System.Collections.Generic;

namespace DotnetNewMobile.ViewModels
{
    public class TemplatePackViewModel
    {
        public TemplatePackViewModel(TemplatePack pack){
            Pack = pack;
            BrowseToNuGet = new Command(ExecuteBrowseToNuget);
            BrowseProjectSite = new Command(ExecuteBrowseProjectSite);
            BrowseLicense = new Command(ExecuteBrowseToLicense);
            ShareCommand = new Command(ExecuteShare);
            ShowActions = new Command(() => ExecuteShowActions());
        }

        public TemplatePack Pack
        {
            get; private set;
        }

        public ICommand BrowseToNuGet
        {
            get; private set;
        }

        public ICommand BrowseProjectSite{
            get; private set;
        }

        public ICommand BrowseLicense{
            get; private set;
        }

        public ICommand ShareCommand{
            get;private set;
        }
        public ICommand ShowActions { get; private set; }

        public string DownloadCount
        {
            get{
                return Pack != null ? Pack.DownloadCount.ToString() : string.Empty;
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

		public string Description
		{
			get
			{
				return Pack != null ? Pack.Description : string.Empty;
			}
		}
		public bool HasLicense{
			get{
				return !string.IsNullOrWhiteSpace(LicenseUrl);
			}
		}
        public string LicenseUrl
        {
            get
            {
                return Pack != null ? Pack.LicenseUrl : string.Empty;
            }
        }
		public bool HasProjectUrl{
			get{
				return !string.IsNullOrWhiteSpace(ProjectUrl);
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
                return Pack != null ? $"https://www.nuget.org/packages/{Pack.Package}" : string.Empty;
            }
        }

        public void ExecuteBrowseToNuget(object s){
            Device.OpenUri(new System.Uri(NuGetUrl));
        }

        public void ExecuteBrowseProjectSite(){
            Device.OpenUri(new System.Uri(ProjectUrl));
        }

        public void ExecuteBrowseToLicense(object s)
        {
            Device.OpenUri(new System.Uri(LicenseUrl));
        }

        public async void ExecuteShare(){
            ShareMessage msg = new ShareMessage();
            msg.Title = "Share";
            msg.Text = "Check out this dotnet template";
            msg.Url = $"http://dotnetnew.azurewebsites.net/pack/{PackageString}";
            await CrossShare.Current.Share(msg);
        }

        async void ExecuteShowActions()
        {
            var actions = GetTemplatePackActions();
            var actionSelection = await Application.Current.MainPage.DisplayActionSheet(
                "Actions", "Cancel", null, TemplateDisplayAction.GetActionStrings(actions));

            if(string.Compare("Cancel",actionSelection,StringComparison.OrdinalIgnoreCase) != 0){
                TemplateDisplayAction.ExecuteAction(actionSelection, actions);
            }
        }

        private List<TemplateDisplayAction>GetTemplatePackActions(){
            List<TemplateDisplayAction> actions =  new List<TemplateDisplayAction>
            {
                new TemplateDisplayAction("View on nuget.org",()=>ExecuteBrowseToNuget(null), true),
                new TemplateDisplayAction("Go to project site",()=>ExecuteBrowseProjectSite(), !string.IsNullOrWhiteSpace(ProjectUrl)),
                new TemplateDisplayAction("View license",()=>ExecuteBrowseToLicense(null), !string.IsNullOrWhiteSpace(LicenseUrl) ),
                new TemplateDisplayAction("Share",()=>ExecuteShare(), true)
            };

            return actions;
        }
        private Page GetMainPage(){
            return Application.Current.MainPage;
        }
    }
}

