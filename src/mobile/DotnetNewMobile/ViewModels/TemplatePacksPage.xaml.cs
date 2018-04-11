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
        private bool UpdateTemplateList = true;
        public TemplatePacksPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new TemplatePacksViewModel(Navigation);
            MessagingCenter.Subscribe<UpdatePageViewModel>(this, "TemplateListUpdated", (sender) =>
            {
                UpdateTemplateList = true;
            });
        }

        // public ICommand TapCommand { get; set; }
		protected override void OnAppearing()
		{
			base.OnAppearing();
            if (UpdateTemplateList)
            {
                viewModel.LoadItemsCommand.Execute(null);
                UpdateTemplateList = false;
            }
		}
	}
}
