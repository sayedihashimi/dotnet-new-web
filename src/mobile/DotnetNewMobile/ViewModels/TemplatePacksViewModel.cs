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
using System.Linq;

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
            Title = "dotnet new templates";
        }

        public int OverallDownloads { get; set; }
        public int NumTemplates { get; set; }
        public int NumTemplatePacks { get; set; }
        public int NumAuthors { get; set; }

        private string _overallDownloadString;
        public string OverallDownloadsString { 
            get{
                return _overallDownloadString;
            }
            set{
                if(_overallDownloadString != value){
                    SetProperty(ref _overallDownloadString, value, nameof(OverallDownloadsString));
                }
            }
        }

        private string _numTemplateString;
        public string NumTemplatesString { 
            get{
                return _numTemplateString;
            }
            set{
                if(_numTemplateString != value){
                    SetProperty(ref _numTemplateString, value, nameof(NumTemplatesString));
                }
            }
        }

        private string _numTemplatePacksString;
        public string NumTemplatePacksString { 
            get{
                return _numTemplatePacksString;
            } 
            set{
                if(_numTemplatePacksString != value){
                    SetProperty(ref _numTemplatePacksString, value, nameof(NumTemplatePacksString));
                }
            }
        }

        private string _numAuthorsString;
        public string NumAuthorsString { 
            get{
                return _numAuthorsString;
            } 
            set{
                if(_numAuthorsString != value){
                    SetProperty(ref _numAuthorsString, value, nameof(NumAuthorsString));
                }
            }
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
                UpdateSummaryData();
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
        public void UpdateSummaryData(){
            if(Items != null){
                OverallDownloads = 0;
                NumTemplates = 0;
                NumTemplatePacks = 0;
                NumAuthors = 0;

                foreach(var i in Items){
                    if (i.Pack != null)
                    {
                        OverallDownloads += i.Pack.DownloadCount;
                        NumTemplates += i.Pack.Templates.Count();
                    }
                    NumTemplatePacks++;
                }

                NumAuthors = (from tp in Items
                              select tp.Pack.Authors).Distinct().ToList().Count;

            }
            else{
                OverallDownloads = 0;
                NumTemplates = 0;
                NumTemplatePacks = 0;
                NumAuthors = 0;
            }


            OverallDownloadsString = $"Overall downloads:       {OverallDownloads}";
            NumTemplatesString = $"Num of templates:        {NumTemplates}";
            NumTemplatePacksString = $"Num of template packs:   {NumTemplatePacks}";
            NumAuthorsString = $"Num of template authors: {NumAuthors}";
        }
        List<TemplatePack> GetTemplatePacks()
        {
            var text = GetJsonFileContents();
            return TemplatePack.CreateFromText(text);
        }
        protected string GetJsonFileContents()
        {
            // if the file exists locally use that file instead of the built in one
            var fileHelper = new SaveAndLoad();
            if(fileHelper.DoesFileExist("dotnetnew-template-report.json")){
                try{
                    return fileHelper.LoadText("dotnetnew-template-report.json");  
                }
                catch{
                }
            }

            // fallback to embedded file
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
