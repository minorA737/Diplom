using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class TimePositionToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Поддерживаем как int, так и double
            double timePosition = 0;

            if (value is int intPos)
                timePosition = intPos;
            else if (value is double doublePos)
                timePosition = doublePos;

            // Корректируем отступ для точного позиционирования эллипса
            return new Thickness(46, timePosition - 4, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}