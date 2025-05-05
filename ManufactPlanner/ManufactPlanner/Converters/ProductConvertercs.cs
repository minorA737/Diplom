using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ManufactPlanner.Converters
{
    public class StatusToColorConverterProd : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string status)
                return "#666666"; // Серый цвет по умолчанию

            bool isBackground = parameter?.ToString() == "Background";

            return status switch
            {
                "Завершено" => "#00ACC1",  // Голубой
                "Отладка" => "#4CAF9D",    // Зеленый
                "Изготовление" => "#9575CD", // Фиолетовый
                "Подготовка" => "#FFB74D",  // Оранжевый
                "В очереди" => "#9575CD",   // Фиолетовый
                "В процессе" => "#00ACC1",  // Голубой
                "В работе" => "#00ACC1",    // Голубой
                "Ждем производство" => "#FFB74D", // Оранжевый
                "Просрочено" => "#FF7043",  // Красный
                "Ожидание" => "#FFB74D",    // Оранжевый
                "Готово" => "#4CAF9D",      // Зеленый
                _ => "#666666"              // Серый по умолчанию
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
        public class PriorityToColorConverterProd : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is not int priority)
                    return "#666666"; // Серый цвет по умолчанию

                return priority switch
                {
                    1 => "#FF7043", // Высокий - Красный
                    2 => "#FFB74D", // Средний - Оранжевый
                    3 => "#4CAF9D", // Низкий - Зеленый
                    _ => "#666666"  // По умолчанию - Серый
                };
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    
}