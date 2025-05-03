using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManufactPlanner.Converters
{
    public class TimeToCanvasPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TimeSpan time)
            {
                // Начинаем с 8:00 и считаем количество минут от этого времени
                double minutesSince8AM = (time.Hours - 8) * 60 + time.Minutes;
                // Преобразуем минуты в пиксели (1 час = примерно 60 пикселей)
                return minutesSince8AM * (60.0 / 60.0);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
