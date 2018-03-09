using System;
using System.Collections.ObjectModel;
using TemplatesShared;
using System.Reflection;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DotnetNewMobile.ViewModels
{
    public class TemplatePacksViewModel : BaseViewModel
    {
        public ObservableCollection<TemplatePack> Items { get; set; }
        public Command LoadItemsCommand { get; set; }

        public TemplatePacksViewModel()
        {
            Items = new ObservableCollection<TemplatePack>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
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
                    Items.Add(item);
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
