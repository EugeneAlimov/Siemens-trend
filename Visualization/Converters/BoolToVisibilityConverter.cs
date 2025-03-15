using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SiemensTrend.Visualization.Converters
{
    /// <summary>
    /// Конвертер логического значения в видимость
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Преобразование из bool в Visibility
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Проверяем параметр для инвертирования (если задан)
                bool invert = false;
                if (parameter is string paramStr && paramStr.ToLower() == "invert")
                {
                    invert = true;
                }

                // Если параметр "invert", то инвертируем значение
                boolValue = invert ? !boolValue : boolValue;

                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        /// <summary>
        /// Преобразование из Visibility в bool
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = (visibility == Visibility.Visible);

                // Проверяем параметр для инвертирования (если задан)
                bool invert = false;
                if (parameter is string paramStr && paramStr.ToLower() == "invert")
                {
                    invert = true;
                }

                // Если параметр "invert", то инвертируем результат
                return invert ? !result : result;
            }

            return true;
        }
    }
}