using System;
using System.Collections.Generic;
using System.Windows.Input;
using DotnetNewMobile.ViewModels;
using DotnetNewMobile.Views;
using Xamarin.Forms;

namespace DotnetNewMobile
{
    public partial class TemplatePacksPage : ContentPage
    {
        TemplatePacksViewModel viewModel;
        public TemplatePacksPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new TemplatePacksViewModel(Navigation);
            //TapCommand = new Command(OnTapped);
        }

        // public ICommand TapCommand { get; set; }
		protected override void OnAppearing()
		{
			base.OnAppearing();
            if(viewModel.Items.Count <= 0){
                viewModel.LoadItemsCommand.Execute(null);
            }
		}

        async void Handle_ItemTapped(object sender, Xamarin.Forms.ItemTappedEventArgs e)
        {
            await Navigation.PushAsync(new TemplatePage());
        }

        async void ViewTemplateTapped(object sender, EventArgs e)
        {
            //await Navigation.PushAsync(new NewItemPage());
            await Navigation.PushAsync(new TemplatePage());
        }

        //async void OnTapped(object sender){
        //    await Navigation.PushAsync(new TemplatePage());
        //}
	}
}
