using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    /// <summary>
    /// Конвертер bool в заданное значение
    /// </summary>
    public class BoolToValueConverterDoc : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string parameterString)
            {
                string[] values = parameterString.Split(',');
                if (values.Length == 2)
                {
                    return boolValue ? values[0] : values[1];
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string parameterString)
            {
                string[] values = parameterString.Split(',');
                if (values.Length == 2 && value != null)
                {
                    if (value.ToString() == values[0])
                        return true;
                    else if (value.ToString() == values[1])
                        return false;
                }
            }

            return value;
        }
    }

    /// <summary>
    /// Конвертер проверяет, больше ли значение, чем параметр
    /// </summary>
    public class IsGreaterThanConverterDoc : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // Пытаемся преобразовать оба значения в double для сравнения
            if (double.TryParse(value.ToString(), out double doubleValue) &&
                double.TryParse(parameter.ToString(), out double doubleParameter))
            {
                return doubleValue > doubleParameter;
            }

            // Если это коллекция, проверяем количество элементов
            if (value is ICollection collection && int.TryParse(parameter.ToString(), out int intParameter))
            {
                return collection.Count > intParameter;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер проверяет, равна ли строка заданному значению
    /// </summary>
    public class StringEqualsValueConverterDoc : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString().Equals(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер для генерации Margin в зависимости от значения
    /// </summary>
    public class MarginConverterDoc : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue
                    ? new Thickness(60, 60, 0, 0)
                    : new Thickness(200, 60, 0, 0);
            }

            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}