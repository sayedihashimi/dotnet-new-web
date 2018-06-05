using System;
using System.Text;
using TemplatesShared;

namespace DotnetNewMobile.ViewModels
{
    public class SearchTemplateViewModel : BaseViewModel
    {
        public SearchTemplateViewModel(Template template){
            Template = template;
        }

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
    }
}
