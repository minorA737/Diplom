using Avalonia.Data.Converters;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManufactPlanner.Converters
{
    public class AuthMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAuthenticated)
            {
                if (isAuthenticated)
                {
                    // Используем переданный параметр как отступы для авторизованного состояния
                    return parameter;
                }
                else
                {
                    // В неавторизованном состоянии отступы отсутствуют
                    return new Thickness(0);
                }
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
