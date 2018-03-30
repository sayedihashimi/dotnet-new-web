using System;
using DotnetNewMobile.Views;
using Xamarin.Forms;

namespace DotnetNewMobile
{
    public class MainPage : TabbedPage
    {
        public MainPage()
        {
            Page templatePacksPage = null;
            Page updateTemplatesPage = null;
            //try{
            //    string url = "http://dotnetnew-api.azurewebsites.net/template-report.json";
            //    string filename = "template-report.json";
            //    new SaveAndLoad().DownloadAndSave(url, filename);
            //    string res = new SaveAndLoad().LoadText(filename);
            //}
            //catch(Exception ex){
            //    System.Console.WriteLine(ex.ToString());
            //}

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                   
                    templatePacksPage = new NavigationPage(new TemplatePacksPage())
                    {
                        Title = "Templates"
                    };
                    updateTemplatesPage = new NavigationPage(new UpdatePage())
                    {
                        Title = "Update templates"
                    };
                    //itemsPage.Icon = "tab_feed.png";
                    //aboutPage.Icon = "tab_about.png";

                    updateTemplatesPage.Icon = "tab_feed.png";
                    templatePacksPage.Icon = "tab_home.png";

                    break;
                default:

                    break;
            }



            Children.Add(templatePacksPage);
            Children.Add(updateTemplatesPage);
            Title = Children[0].Title;
        }

        protected override void OnCurrentPageChanged()
        {
            base.OnCurrentPageChanged();
            Title = CurrentPage?.Title ?? string.Empty;
        }
    }
}
