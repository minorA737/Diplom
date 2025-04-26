using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class MarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSidebarCollapsed)
            {
                if (isSidebarCollapsed)
                    return new Thickness(60, 60, 0, 0); // Когда боковая панель свернута
                else
                    return new Thickness(200, 60, 0, 0); // Когда боковая панель развернута
            }

            return new Thickness(200, 60, 0, 0); // По умолчанию
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}