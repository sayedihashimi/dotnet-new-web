using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DotnetNewMobile.ViewModels;
using TemplatesShared;
using Xamarin.Forms;

namespace DotnetNewMobile
{
    public class SearchPageViewModel : BaseViewModel
    {
        public SearchPageViewModel(INavigation navigation) : this(navigation, null, null)
        {

        }

        public SearchPageViewModel(INavigation navigation,string searchTerm, IList<Template>foundTemplates){
            IsBusy = false;
            SearchCommand = new Command(() => ExecuteSearchCommand());
            Navigation = navigation;
            SearchTerm = searchTerm;

            FoundItems = new ObservableCollection<SearchTemplateViewModel>();
            if(foundTemplates != null && foundTemplates.Count > 0){
                foreach(var template in foundTemplates){
                    FoundItems.Add(new SearchTemplateViewModel(template, navigation,this));
                }
            }
            NumSearchResults = FoundItems.Count;
        }

        public INavigation Navigation { get; set; }

        private TemplateSearcher _searcher = new TemplateSearcher();

        private string _searchTerm;
        public string SearchTerm
        {
            get
            {
                return _searchTerm;
            }
            set
            {
                if (_searchTerm != value)
                {
                    SetProperty(ref _searchTerm, value, nameof(SearchTerm));
                    UpdateIsSearchEnabled();
                }
            }
        }

        public ICommand SearchCommand
        {
            get; private set;
        }

        private bool _isSearchEnabled;
        public bool IsSearchEnabled{
            get{
                return _isSearchEnabled;
            }
            set{
                if(_isSearchEnabled != value){
                    SetProperty(ref _isSearchEnabled, value, nameof(IsSearchEnabled));
                }
            }
        }
        private void UpdateIsSearchEnabled(){
            IsSearchEnabled = !string.IsNullOrWhiteSpace(SearchTerm);
        }

        public ObservableCollection<SearchTemplateViewModel> FoundItems { get; set; }
        internal void SetFoundItems(IList<Template> templates){
            if(templates != null){
                FoundItems.Clear();
                foreach(var item in templates){
                    FoundItems.Add(new SearchTemplateViewModel(item, Navigation, this));
                }
                NumSearchResults = FoundItems.Count;
                NumSearchResultLabelVisible = true;
            }
            else{
                FoundItems.Clear();
                NumSearchResults = 0;
            }
        }

        private int _numSearchResults;
        public int NumSearchResults{
            get{
                return _numSearchResults;
            }
            set{
                if(_numSearchResults != value){
                    SetProperty(ref _numSearchResults, value, nameof(NumSearchResults));
                    NumSearchResultLabelVisible = value > 0;
                }
            }
        }

        private bool _numSearchResultLabelVisible;
        public bool NumSearchResultLabelVisible{
            get{
                return _numSearchResultLabelVisible;
            }
            set{
                if(_numSearchResultLabelVisible != value){
                    SetProperty(ref _numSearchResultLabelVisible, value, nameof(NumSearchResultLabelVisible));
                }
            }
        }

        void ExecuteSearchCommand()
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;
                var helper = new TemplateHelper();
                var allTemplates = helper.GetTemplatePacks();
                var foundTemplates = _searcher.Search(SearchTerm, allTemplates);
                SetFoundItems(foundTemplates);
                
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
