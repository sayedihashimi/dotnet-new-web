using System;
using System.Collections.Generic;
using DotnetNewMobile.ViewModels;
using Xamarin.Forms;

namespace DotnetNewMobile.Views
{
    public partial class UpdatePage : ContentPage
    {
        public UpdatePage()
        {
            InitializeComponent();
            BindingContext = new UpdatePageViewModel();
        }
    }
}
