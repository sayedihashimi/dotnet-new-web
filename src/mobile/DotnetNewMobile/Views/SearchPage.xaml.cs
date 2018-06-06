using System;
using System.Collections.Generic;
using DotnetNewMobile;
using System.Linq;
using Xamarin.Forms;

namespace DotnetNewMobile.Views
{
    public partial class SearchPage : BaseContentPage
    {
        private SearchPageViewModel viewModel;
        private bool FirstRun = true;
        public SearchPage() : base()
        {
            InitializeComponent();
            BindingContext = viewModel = new SearchPageViewModel(Navigation);
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (FirstRun)
            {
                this.entrySearch.Focus();
                FirstRun = false;
            }
            // disable the selected gesture - from https://forums.xamarin.com/discussion/comment/261433/#Comment_261433
            if (!searchResultList.GestureRecognizers.Any())
            {
                searchResultList.GestureRecognizers.Add(new TapGestureRecognizer());
            }
        }
    }
}
