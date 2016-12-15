using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace LunchScheduler.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool IsInverted { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var shouldBeVisibile = (bool) value;

            if (IsInverted)
            {
                return shouldBeVisibile ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return shouldBeVisibile ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
