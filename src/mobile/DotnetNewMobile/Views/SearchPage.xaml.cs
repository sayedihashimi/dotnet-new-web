using System;
using System.Collections.Generic;
using DotnetNewMobile;
using Xamarin.Forms;

namespace DotnetNewMobile.Views
{
    public partial class SearchPage : BaseContentPage
    {
        private SearchPageViewModel viewModel;
        public SearchPage() : base()
        {
            InitializeComponent();
            BindingContext = viewModel = new SearchPageViewModel();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.entrySearch.Focus();
        }
    }
}
