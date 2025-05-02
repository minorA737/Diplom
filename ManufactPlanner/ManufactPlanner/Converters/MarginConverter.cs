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
    public class MarginConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return new Thickness(0);

            bool boolValue;
            if (value is bool b)
            {
                boolValue = b;
            }
            else if (bool.TryParse(value.ToString(), out bool parsedBool))
            {
                boolValue = parsedBool;
            }
            else
            {
                return new Thickness(0);
            }

            string paramStr = parameter.ToString();
            string[] marginValues = paramStr.Split(',');

            if (marginValues.Length == 2)
            {
                // Формат "left,right" для левой и правой границы
                if (double.TryParse(marginValues[0], out double leftRight1) &&
                    double.TryParse(marginValues[1], out double leftRight2))
                {
                    return boolValue
                        ? new Thickness(leftRight1, 0, leftRight1, 0)
                        : new Thickness(leftRight2, 0, leftRight2, 0);
                }
            }
            else if (marginValues.Length == 4)
            {
                // Формат "left,top,right,bottom" для всех сторон
                if (double.TryParse(marginValues[0], out double left) &&
                    double.TryParse(marginValues[1], out double top) &&
                    double.TryParse(marginValues[2], out double right) &&
                    double.TryParse(marginValues[3], out double bottom))
                {
                    return boolValue
                        ? new Thickness(left, top, right, bottom)
                        : new Thickness(0);
                }
            }
            else if (marginValues.Length == 8)
            {
                // Формат "left1,top1,right1,bottom1,left2,top2,right2,bottom2" для двух разных значений
                if (double.TryParse(marginValues[0], out double left1) &&
                    double.TryParse(marginValues[1], out double top1) &&
                    double.TryParse(marginValues[2], out double right1) &&
                    double.TryParse(marginValues[3], out double bottom1) &&
                    double.TryParse(marginValues[4], out double left2) &&
                    double.TryParse(marginValues[5], out double top2) &&
                    double.TryParse(marginValues[6], out double right2) &&
                    double.TryParse(marginValues[7], out double bottom2))
                {
                    return boolValue
                        ? new Thickness(left1, top1, right1, bottom1)
                        : new Thickness(left2, top2, right2, bottom2);
                }
            }

            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}