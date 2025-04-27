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
                var margin = isSidebarCollapsed
                    ? new Thickness(60, 60, 0, 0)
                    : new Thickness(200, 60, 0, 0);
                Console.WriteLine($"MarginConverter вернул: {margin}");
                return margin;
            }
            return new Thickness(200, 60, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}