using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManufactPlanner.Converters
{
    public class EventsCountToFillConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // Возвращаем строку цвета на основе количества событий
                return count switch
                {
                    0 => "#F8F9FA",
                    1 => "#E3F2FD", // Светло-голубой
                    2 => "#B3E5FC", // Голубой
                    3 => "#81D4FA", // Средне-голубой
                    4 => "#4FC3F7", // Насыщенный голубой
                    _ => "#29B6F6"  // Яркий голубой
                };
            }

            return "#F8F9FA"; // Фон по умолчанию
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
