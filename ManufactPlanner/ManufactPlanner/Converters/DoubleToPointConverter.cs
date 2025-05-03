// Converters/DoubleToPointConverter.cs
using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class DoubleToPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int timePosition && parameter is string coords)
            {
                string[] parts = coords.Split(',');
                if (parts.Length == 2)
                {
                    double x = parts[0] == "X" ? timePosition : double.Parse(parts[0]);
                    double y = parts[1] == "Y" ? timePosition : double.Parse(parts[1]);
                    return new Point(x, y);
                }
            }

            return new Point(0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}