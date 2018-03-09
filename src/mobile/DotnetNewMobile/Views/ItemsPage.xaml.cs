using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using Xamarin.Forms;
using TemplatesShared;

namespace DotnetNewMobile
{
    public partial class ItemsPage : ContentPage
    {
        ItemsViewModel viewModel;

        public ItemsPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new ItemsViewModel();

            var text = GetJsonFileContents();
            var templatePacks = TemplatePack.CreateFromText(text);
        }

        async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var item = args.SelectedItem as Item;
            if (item == null)
                return;

            await Navigation.PushAsync(new ItemDetailPage(new ItemDetailViewModel(item)));

            // Manually deselect item
            ItemsListView.SelectedItem = null;
        }

        async void AddItem_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new NewItemPage());
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (viewModel.Items.Count == 0)
                viewModel.LoadItemsCommand.Execute(null);
        }

        protected string GetJsonFileContents(){
            string resxname = "DotnetNewMobile.iOS.Assets.template-report.json";
            var assembly = typeof(ItemsPage).GetTypeInfo().Assembly;
            string text = null;
            using(var stream = assembly.GetManifestResourceStream(resxname))
            using(var reader = new System.IO.StreamReader(stream)){
                text = reader.ReadToEnd();
            }
            return text;
        }
    }
}
