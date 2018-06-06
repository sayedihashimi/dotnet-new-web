using System;
using System.Globalization;
using Xamarin.Forms;

namespace DotnetNewMobile
{
    public class EntryCompletedEventArgsToEventArgsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var eventArgs = value as EventArgs;
            return eventArgs;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
