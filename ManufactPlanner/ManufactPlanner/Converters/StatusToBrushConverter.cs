using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Выполнено" => new SolidColorBrush(Color.Parse("#4CAF9D")),
                    "В процессе" => new SolidColorBrush(Color.Parse("#00ACC1")),
                    "В очереди" => new SolidColorBrush(Color.Parse("#9575CD")),
                    "Ждем производство" => new SolidColorBrush(Color.Parse("#FFB74D")),
                    "Просрочено" => new SolidColorBrush(Color.Parse("#FF7043")),
                    _ => new SolidColorBrush(Color.Parse("#9E9E9E")),
                };
            }
            return new SolidColorBrush(Color.Parse("#9E9E9E"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}