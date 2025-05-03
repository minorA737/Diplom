using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManufactPlanner.Converters
{
    public class EventsCountToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count switch
                {
                    0 => string.Empty,
                    1 => "1 событие",
                    2 => "2 события",
                    3 => "3 события",
                    4 => "4 события",
                    _ => $"{count} событий"
                };
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
