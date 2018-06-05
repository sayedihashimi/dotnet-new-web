using System;
using System.Collections.Generic;
using DotnetNewMobile;
using Xamarin.Forms;

namespace DotnetNewMobile.Views
{
    public partial class SearchPage : BaseContentPage
    {
        public SearchPage() : base()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.entrySearch.Focus();
        }
    }
}
