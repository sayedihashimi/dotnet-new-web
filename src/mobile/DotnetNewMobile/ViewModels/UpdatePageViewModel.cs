using System;
using System.Windows.Input;
using Xamarin.Forms;

namespace DotnetNewMobile.ViewModels
{
    public class UpdatePageViewModel : BaseViewModel
    {
        public UpdatePageViewModel()
        {
            UpdateTemplates = new Command(()=>ExecuteUpdateTemplates());
            _resultMessage = string.Empty;
            Title = "Update template list";
        }

        public ICommand UpdateTemplates{
            get;private set;
        }
        private string _resultMessage;
        public string ResultMessage{
            get{
                return _resultMessage;
            }
            set{
                if (_resultMessage != value)
                {
                    SetProperty(ref _resultMessage, value, nameof(ResultMessage));
                }
            }
        }

        private void ExecuteUpdateTemplates(){
            ResultMessage = string.Empty;
            try{
                string url = "http://dotnetnew-api.azurewebsites.net/template-report.json";
                string filename = "dotnetnew-template-report.json";
                var saveAndLoad = new SaveAndLoad();
                saveAndLoad.DownloadAndSave(url, filename);
                string res = saveAndLoad.LoadText(filename);
                ResultMessage = "Templates successfully updated";
            }
            catch(Exception ex){
                ResultMessage = ex.ToString();
            }
        }
    }
}
