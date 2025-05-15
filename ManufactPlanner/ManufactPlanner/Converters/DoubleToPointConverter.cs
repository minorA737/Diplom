
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
            if (value is int position && parameter is string paramStr)
            {
                string[] parts = paramStr.Split(',');
                if (parts.Length == 2)
                {
                    string xStr = parts[0];
                    string yStr = parts[1];

                    if (double.TryParse(xStr, out double x))
                    {
                        if (yStr == "Y")
                        {
                            // X фиксированный, Y берем из значения
                            return new Point(x, position);
                        }
                        else if (double.TryParse(yStr, out double y))
                        {
                            // Обе координаты заданы
                            return new Point(x, y);
                        }
                    }
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