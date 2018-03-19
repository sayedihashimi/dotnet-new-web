using System;
using System.Collections.Generic;
using DotnetNewMobile.ViewModels;
using Xamarin.Forms;

namespace DotnetNewMobile.Views
{
    public partial class TemplatePackPage : ContentPage
    {
        public TemplatePackPage(TemplatePackViewModel packViewModel)
        {
            InitializeComponent();
            PackViewModel = packViewModel;
        }

        public TemplatePackViewModel PackViewModel{
            get;private set;
        }



    }
}
