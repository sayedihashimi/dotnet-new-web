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
        public IList<TemplatePack> TemplateList { get; set; }
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

        private int _overallDownloads;
        public int OverallDownloads 
        { 
            get{
                return _overallDownloads;
            }
            set{
                if(_overallDownloads != value){
                    SetProperty(ref _overallDownloads, value, nameof(OverallDownloads));
                }
            }
        }

        private int _numTemplates;
        public int NumTemplates
        {
            get
            {
                return _numTemplates;
            }
            set
            {
                if (_numTemplates != value)
                {
                    SetProperty(ref _numTemplates, value, nameof(NumTemplates));
                }
            }
        }

        private int _numTemplatePacks;
        public int NumTemplatePacks
        {
            get
            {
                return _numTemplatePacks;
            }
            set
            {
                if (_numTemplatePacks != value)
                {
                    SetProperty(ref _numTemplatePacks, value, nameof(NumTemplatePacks));
                }
            }
        }

        private int _numAuthors;
        public int NumAuthors
        {
            get
            {
                return _numAuthors;
            }
            set
            {
                if (_numAuthors != value)
                {
                    SetProperty(ref _numAuthors, value, nameof(NumAuthors));
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
                TemplateList = await TemplatePack.CreateFromTextAsync(GetJsonFileContents());
                foreach (var item in TemplateList)
                {
                    Items.Add(new TemplatePackViewModel(item, Navigation));
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
            OverallDownloads = 0;
            NumTemplates = 0;
            NumTemplatePacks = 0;
            NumAuthors = 0;

            if(Items != null){
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
