using System;
using Windows.UI.Xaml.Data;

namespace LunchScheduler.Converters
{
    class StringToUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new Uri(value.ToString(), UriKind.Absolute);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var uri = (Uri) value;

            return uri?.AbsolutePath;
        }
    }
}
