using System;
using DotnetNewMobile.Views;
using Xamarin.Forms;

namespace DotnetNewMobile
{
	public class BaseContentPage : ContentPage
    {
        public BaseContentPage()
        {
            AddSharedToolbarItems();
        }

        protected void AddSharedToolbarItems(){
            this.ToolbarItems.Add(new ToolbarItem("Update", "update.png", () =>
            {
                // Task.WaitAll(Navigation.PushAsync(new NavigationPage(new UpdatePage())));
                Navigation.PushAsync(new UpdatePage());
            }));
        }
    }
}
