using System;
using System.Text;
using System.Windows.Input;
using DotnetNewMobile.Views;
using TemplatesShared;
using Xamarin.Forms;

namespace DotnetNewMobile.ViewModels
{
    public class SearchTemplateViewModel : BaseViewModel
    {
        public SearchTemplateViewModel(Template template, INavigation navigation, SearchPageViewModel searchPage){
            Template = template;
            Navigation = navigation;
            NavigateToTemplatePackCommand = new Command(() => ExecuteNavigateToTemplatePack());
            SearchAuthor = new Command(ExecuteSearchAuthor);
            SearchPage = searchPage;
        }

        private SearchPageViewModel SearchPage { get; set; }
        public INavigation Navigation { get; set; }
        private Template _template;
        public Template Template
        {
            get{
                return _template;
            }
            set{
                SetTemplateObj(value);
            }
        }

        private string _tagsString;
        public string TagsString{
            get{
                return _tagsString;
            }
            set{
                if(_tagsString != value){
                    SetProperty(ref _tagsString, value, nameof(TagsString));
                }
            }
        }

        private string _classificationsString;
        public string ClassificationsString{
            get{
                return _classificationsString;
            }
            set{
                if(_classificationsString != value){
                    SetProperty(ref _classificationsString, value, nameof(ClassificationsString));
                }
            }
        }

        protected void SetTemplateObj(Template template){
            if(_template != template){
                SetProperty(ref _template, template, nameof(Template));

                StringBuilder tagBuilder = new StringBuilder();
                foreach(var tag in template.Tags){
                    tagBuilder.Append($"{tag.Key}={tag.Value}; ");
                }

                TagsString = tagBuilder.ToString();

                StringBuilder classificationsBuilder = new StringBuilder();
                foreach(var citem in template.Classifications){
                    classificationsBuilder.Append($"{citem}; ");
                }
                ClassificationsString = classificationsBuilder.ToString();
            }
        }

        public ICommand NavigateToTemplatePackCommand { get; set; }
        public async void ExecuteNavigateToTemplatePack(){
            var helper = new TemplateHelper();
            TemplatePack pack = helper.GetTemplatePackById(Template.TemplatePackId, helper.GetTemplatePacks());

            await Navigation.PushAsync(new TemplatePackPage( new TemplatePackViewModel(pack,Navigation)));
        }

        private TemplateSearcher _searcher = new TemplateSearcher();

        public ICommand SearchAuthor
        {
            get; private set;
        }
        void ExecuteSearchAuthor(object param)
        {
            string author = (string)param;
            var helper = new TemplateHelper();
            var allTemplates = helper.GetTemplatePacks();
            var foundTemplates = _searcher.SearchByAuthor(author, allTemplates);
            SearchPage.SetFoundItems(foundTemplates);
            SearchPage.SearchTerm = $"author=\"{author}\"";
        }
    }
}
