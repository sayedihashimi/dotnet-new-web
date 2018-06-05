using System;
using System.Threading.Tasks;
using DotnetNewMobile.Views;
using Xamarin.Forms;

namespace DotnetNewMobile
{
    public class MainPage : TabbedPage
    {

        public MainPage()
        {
            Page templatePacksPage = null;
            Page searchPage = null;
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
                    searchPage = new NavigationPage(new SearchPage())
                    {
                        Title = "Search"
                    };
                    //itemsPage.Icon = "tab_feed.png";
                    //aboutPage.Icon = "tab_about.png";

                    searchPage.Icon = "search.png";
                    templatePacksPage.Icon = "tab_home.png";

                    break;
                default:

                    break;
            }



            Children.Add(templatePacksPage);
            Children.Add(searchPage);

            Title = Children[0].Title;
        }

        protected override void OnCurrentPageChanged()
        {
            base.OnCurrentPageChanged();
            Title = CurrentPage?.Title ?? string.Empty;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        private void GoToPage(Page page){
            Task.WaitAll(Navigation.PushAsync(page));
        }
        private async void GoToPageAsync(Page page){
            await Navigation.PushAsync(page);
        }
    }
}
