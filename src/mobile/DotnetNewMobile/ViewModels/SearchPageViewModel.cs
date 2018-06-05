using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DotnetNewMobile
{
    public class SearchPageViewModel : BaseViewModel
    {
        private string _searchTerm;
        public string SearchTerm
        { 
            get{
                return _searchTerm;
            } 
            set{
                if(_searchTerm != value){
                    SetProperty(ref _searchTerm, value, nameof(SearchTerm));
                }
            }
        }

        public ICommand SearchCommand{
            get;private set;
        }

        public SearchPageViewModel() {
            IsBusy = false;
            // SearchCommand = new Command(async () =>  ExecuteSearchCommand());
            SearchCommand = new Command(()=>ExecuteSearchCommand());
            SearchTerm = "foo";
        }

         void ExecuteSearchCommand(){
            if(IsBusy){
                return;
            }

            try{

            }
            finally{
                IsBusy = false;
            }
        }
    }
}
