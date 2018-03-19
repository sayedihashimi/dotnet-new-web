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
        }

        // public ICommand TapCommand { get; set; }
		protected override void OnAppearing()
		{
			base.OnAppearing();
            if(viewModel.Items.Count <= 0){
                viewModel.LoadItemsCommand.Execute(null);
            }
		}
	}
}
