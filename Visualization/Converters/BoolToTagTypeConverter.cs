using System;
using System.Globalization;
using System.Windows.Data;

namespace SiemensTrend.Visualization.Converters
{
    /// <summary>
    /// Конвертер логического значения IsDbTag в строковое представление типа тега
    /// </summary>
    public class BoolToTagTypeConverter : IValueConverter
    {
        /// <summary>
        /// Преобразование из bool в строку типа тега
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDbTag)
            {
                return isDbTag ? "DB" : "PLC";
            }

            return "Неизв.";
        }

        /// <summary>
        /// Преобразование из строки типа тега в bool
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tagType)
            {
                return tagType.Equals("DB", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}