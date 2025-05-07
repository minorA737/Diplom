// Converters/ThemeAwareColorConverter.cs
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class ThemeAwareColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLightTheme && parameter is string colors)
            {
                // Формат параметра: "ЦветДляСветлойТемы,ЦветДляТемнойТемы"
                var colorParts = colors.Split(',');
                if (colorParts.Length == 2)
                {
                    return isLightTheme ? colorParts[0] : colorParts[1];
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}