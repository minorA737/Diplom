using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    /// <summary>
    /// Конвертер для преобразования логического значения в одно из двух указанных значений.
    /// Использование: {Binding IsTrue, Converter={StaticResource BoolToValue}, ConverterParameter='TrueValue,FalseValue'}
    /// </summary>
    public class BoolToValueConverterTaskDetails : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return BindingOperations.DoNothing;

            if (parameter is not string paramString)
                return BindingOperations.DoNothing;

            var parts = paramString.Split(',');
            if (parts.Length != 2)
                return BindingOperations.DoNothing;

            return boolValue ? parts[0] : parts[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Конвертер для сравнения числового значения с параметром.
    /// Использование: {Binding Count, Converter={StaticResource IsGreaterThan}, ConverterParameter='5'}
    /// или {Binding Count, Converter={StaticResource IsGreaterThan}, ConverterParameter='5 Invert=true'} для инвертирования
    /// </summary>
    public class IsGreaterThanConverterTaskDetails : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string paramString = parameter.ToString();
            bool invert = paramString.Contains("Invert=true", StringComparison.OrdinalIgnoreCase);

            // Извлекаем числовую часть параметра
            string numberString = invert
                ? paramString.Split(new[] { " Invert" }, StringSplitOptions.RemoveEmptyEntries)[0]
                : paramString;

            if (!double.TryParse(numberString, out double paramValue))
                return false;

            bool result;
            if (value is int intValue)
                result = intValue > paramValue;
            else if (value is double doubleValue)
                result = doubleValue > paramValue;
            else if (value is float floatValue)
                result = floatValue > paramValue;
            else if (value is decimal decimalValue)
                result = (double)decimalValue > paramValue;
            else if (value is long longValue)
                result = longValue > paramValue;
            else if (double.TryParse(value.ToString(), out double parsedValue))
                result = parsedValue > paramValue;
            else
                return false;

            return invert ? !result : result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Конвертер для сравнения строкового значения с параметром.
    /// Использование: {Binding Text, Converter={StaticResource StringEqualsValue}, ConverterParameter='TargetValue'}
    /// </summary>
    public class StringEqualsValueConverterTaskDetails : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Конвертер для расчета отступов в зависимости от логического значения.
    /// Использование: {Binding IsSidebarCollapsed, Converter={StaticResource MarginConverter}}
    /// </summary>
    public class MarginConverterTaskDetails : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSidebarCollapsed)
            {
                // Если боковая панель свернута, устанавливаем левый отступ меньше
                // В противном случае - больше
                return isSidebarCollapsed
                    ? new Avalonia.Thickness(60, 0, 0, 0)
                    : new Avalonia.Thickness(200, 0, 0, 0);
            }

            return new Avalonia.Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Конвертер для преобразования процентного значения в ширину элемента.
    /// Использование: {Binding CompletionPercentage, Converter={StaticResource PercentToWidthConverter}}
    /// </summary>
    public class PercentToWidthConverterTaskDetails : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int percentValue)
            {
                // Предполагаем, что максимальная ширина 180 пикселей (это можно настроить через параметр)
                double maxWidth = 180;

                if (parameter != null && double.TryParse(parameter.ToString(), out double paramWidth))
                {
                    maxWidth = paramWidth;
                }

                // Рассчитываем ширину в зависимости от процента
                return Math.Max(0, Math.Min(maxWidth, maxWidth * percentValue / 100));
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }
}