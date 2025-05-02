using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class StringEqualsValueConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Проверка на null
            if (value == null || parameter == null)
                return false;

            string valueStr = value.ToString();
            string paramStr = parameter.ToString();

            // Возможность инвертировать результат
            bool invert = false;
            if (paramStr.Contains("Invert=true"))
            {
                invert = true;
                paramStr = paramStr.Replace("Invert=true", "").Trim();
            }

            // Сравнение строк без учета регистра
            bool result = string.Equals(valueStr, paramStr, StringComparison.OrdinalIgnoreCase);

            // Инвертирование результата при необходимости
            return invert ? !result : result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}