using System;
using System.Globalization;
using System.Windows.Data;

namespace SiemensTagExporter
{
    /// <summary>
    /// Конвертер для определения, должен ли прогресс-бар быть в режиме неопределенного времени
    /// </summary>
    public class ProgressValueConverter : IValueConverter
    {
        /// <summary>
        /// Конвертирует значение прогресса в булево значение для свойства IsIndeterminate
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int progressValue)
            {
                // Если прогресс равен 0, используем неопределенный режим
                return progressValue == 0;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}