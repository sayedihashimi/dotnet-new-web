using System;
using Xamarin.Forms;
using Xamarin.Essentials;
namespace DotnetNewMobile.Controls
{
    // https://forums.xamarin.com/discussion/27323/how-can-i-recognize-long-press-gesture-in-xamarin-forms
    public class LabelWithCopyOnLongPress : Label
    {
        public void HandleLongPress(){
            Clipboard.SetText(this.Text);
            Application.Current.MainPage.DisplayAlert(
                "Copied", "Install command copied to the clipboard", "Close");
        }
    }
}
