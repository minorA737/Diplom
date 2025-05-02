using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class IsGreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Проверка на null
            if (value == null || parameter == null)
                return false;

            bool invert = false;
            string paramStr = parameter.ToString();

            // Проверка наличия параметра инверсии
            if (paramStr.Contains("Invert=true"))
            {
                invert = true;
                paramStr = paramStr.Replace("Invert=true", "").Trim();
            }

            // Преобразование в числовые значения
            if (int.TryParse(paramStr, out int compareValue))
            {
                bool result;

                // Обработка коллекций
                if (value is System.Collections.ICollection collection)
                {
                    result = collection.Count > compareValue;
                }
                // Обработка числовых значений
                else if (value is int intValue)
                {
                    result = intValue > compareValue;
                }
                else if (value is double doubleValue && double.TryParse(paramStr, out double doubleCompare))
                {
                    result = doubleValue > doubleCompare;
                }
                else if (value is decimal decimalValue && decimal.TryParse(paramStr, out decimal decimalCompare))
                {
                    result = decimalValue > decimalCompare;
                }
                else
                {
                    // Если не удалось преобразовать значение
                    return false;
                }

                // Инвертирование результата при необходимости
                return invert ? !result : result;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}