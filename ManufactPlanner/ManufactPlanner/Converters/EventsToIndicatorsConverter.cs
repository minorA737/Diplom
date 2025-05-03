using Avalonia.Data.Converters;
using ManufactPlanner.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ManufactPlanner.ViewModels.CalendarViewModel;

namespace ManufactPlanner.Converters
{
    public class EventsToIndicatorsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<CalendarEventViewModel> events && events.Count > 0)
            {
                var indicators = new ObservableCollection<EventIndicator>();

                // Группируем события по цвету и добавляем индикаторы
                var uniqueColors = events.Select(e => e.Color).Distinct().Take(4);

                foreach (var color in uniqueColors)
                {
                    indicators.Add(new EventIndicator { Color = color });
                }

                return indicators;
            }

            return new ObservableCollection<EventIndicator>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
