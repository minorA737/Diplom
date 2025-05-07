// DateOnlyToDateTimeConverter.cs
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ManufactPlanner.Converters
{
    public class DateOnlyToDateTimeConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateOnly dateOnly)
            {
                return new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return DateOnly.FromDateTime(dateTime);
            }
            return null;
        }
    }
}