using System;
using System.Collections.ObjectModel;
using TemplatesShared;
using System.Reflection;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Input;
using DotnetNewMobile.Views;

namespace DotnetNewMobile.ViewModels
{
    public class TemplatePacksViewModel : BaseViewModel
    {
        public ObservableCollection<TemplatePackViewModel> Items { get; set; }
        public Command LoadItemsCommand { get; private set; }
        public ICommand TappedCommand { get; private set; }
        private INavigation Navigation { get; set; }

        public TemplatePacksViewModel(INavigation navigation)
        {
            Items = new ObservableCollection<TemplatePackViewModel>();
            Navigation = navigation;
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            TappedCommand = new Command<TemplatePackViewModel>(ExecuteTapped);
        }

        async void ExecuteTapped(TemplatePackViewModel pack){
            System.Console.WriteLine("inside ExecuteTapped");

            await Navigation.PushAsync(new TemplatePackPage(pack));
        }

        async Task ExecuteLoadItemsCommand(){
            if(IsBusy){
                return;
            }

            IsBusy = true;

            try
            {
                Items.Clear();
                var items = await TemplatePack.CreateFromTextAsync(GetJsonFileContents());
                foreach (var item in items)
                {
                    Items.Add(new TemplatePackViewModel(item));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        List<TemplatePack> GetTemplatePacks()
        {
            var text = GetJsonFileContents();
            return TemplatePack.CreateFromText(text);
        }
        protected string GetJsonFileContents()
        {
            string resxname = "DotnetNewMobile.iOS.Assets.template-report.json";
            var assembly = typeof(ItemsPage).GetTypeInfo().Assembly;
            string text = null;
            using (var stream = assembly.GetManifestResourceStream(resxname))
            using (var reader = new System.IO.StreamReader(stream))
            {
                text = reader.ReadToEnd();
            }
            return text;
        }
    }
}
