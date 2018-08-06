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
