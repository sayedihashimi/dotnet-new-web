using System;
using System.Collections.Generic;
using DotnetNewMobile.ViewModels;
using Xamarin.Forms;

namespace DotnetNewMobile
{
    public partial class TemplatePacksPage : ContentPage
    {
        TemplatePacksViewModel viewModel;
        public TemplatePacksPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new TemplatePacksViewModel();
        }

		protected override void OnAppearing()
		{
			base.OnAppearing();
            if(viewModel.Items.Count <= 0){
                viewModel.LoadItemsCommand.Execute(null);
            }
		}
        async void ViewTemplateClicked(object sender, EventArgs e)
        {
            //await Navigation.PushAsync(new NewItemPage());
            await Navigation.PushAsync(new TemplatedPage());
        }
	}
}
