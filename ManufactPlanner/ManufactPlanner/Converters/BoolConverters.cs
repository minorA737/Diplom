using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class BoolToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string parameterString)
            {
                var values = parameterString.Split(',');
                return boolValue ? values[0] : values.Length > 1 ? values[1] : values[0];
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToValueConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

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
                return null;
            }

            string paramStr = parameter.ToString();
            string[] parts = paramStr.Split(',');

            if (parts.Length != 2)
                return null;

            // Получаем значения для true и false
            string trueValue = parts[0].Trim();
            string falseValue = parts[1].Trim();

            // Возвращаем соответствующее значение
            return boolValue ? trueValue : falseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            string valueStr = value.ToString();
            string paramStr = parameter.ToString();
            string[] parts = paramStr.Split(',');

            if (parts.Length != 2)
                return null;

            string trueValue = parts[0].Trim();

            return valueStr.Equals(trueValue);
        }
    }
}
