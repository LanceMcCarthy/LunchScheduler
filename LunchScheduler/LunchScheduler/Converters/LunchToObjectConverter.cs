using System;
using Windows.UI.Xaml.Data;
using LunchScheduler.Data.Models;

namespace LunchScheduler.Converters
{
    class LunchToObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value as object;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value as LunchAppointment;
        }
    }
}
