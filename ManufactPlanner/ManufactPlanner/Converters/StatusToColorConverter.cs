using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "#CCCCCC"; // Серый по умолчанию

            string status = value.ToString();
            bool isBackground = parameter != null && parameter.ToString() == "Background";

            string color;
            switch (status.ToLower())
            {
                case "активен":
                    color = "#4CAF9D"; // Зеленый
                    break;
                case "завершен":
                    color = "#9575CD"; // Фиолетовый
                    break;
                case "отменен":
                    color = "#FF7043"; // Красный
                    break;
                default:
                    color = "#666666"; // Серый
                    break;
            }

            // Для фона делаем полупрозрачный
            if (isBackground)
                return color; // Opacity применяется в XAML
            else
                return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}