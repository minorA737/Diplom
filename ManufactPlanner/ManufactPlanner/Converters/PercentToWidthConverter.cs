using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class PercentToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            // Получаем максимальную ширину (по умолчанию 100)
            double maxWidth = 100;
            if (parameter != null && double.TryParse(parameter.ToString(), out double parsedMaxWidth))
            {
                maxWidth = parsedMaxWidth;
            }

            // Преобразуем значение в процент
            if (value is int intValue)
            {
                return (intValue / 100.0) * maxWidth;
            }
            else if (value is double doubleValue)
            {
                return (doubleValue / 100.0) * maxWidth;
            }
            else if (value is string stringValue && int.TryParse(stringValue.Replace("%", ""), out int percentValue))
            {
                return (percentValue / 100.0) * maxWidth;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}