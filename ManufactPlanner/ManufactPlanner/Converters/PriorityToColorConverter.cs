using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class PriorityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int priority)
            {
                return priority switch
                {
                    1 => "#FF7043", // Высокий
                    2 => "#FFB74D", // Средний
                    3 => "#9575CD", // Низкий
                    _ => "#666666"  // По умолчанию
                };
            }
            return "#666666";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
