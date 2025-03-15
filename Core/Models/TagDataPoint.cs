using System;

namespace SiemensTrend.Core.Models
{
    /// <summary>
    /// Точка данных для графика
    /// </summary>
    public class TagDataPoint
    {
        /// <summary>
        /// Метка времени
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Ссылка на тег
        /// </summary>
        public TagDefinition Tag { get; set; }

        /// <summary>
        /// Значение тега
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Числовое значение для графика
        /// </summary>
        public double? NumericValue
        {
            get
            {
                if (Value == null) return null;

                // Преобразуем разные типы в double
                if (Value is bool boolValue)
                    return boolValue ? 1.0 : 0.0;
                if (Value is int intValue)
                    return intValue;
                if (Value is double doubleValue)
                    return doubleValue;
                if (Value is float floatValue)
                    return floatValue;

                // Пытаемся преобразовать строку
                if (double.TryParse(Value.ToString(), out double result))
                    return result;

                return null;
            }
        }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public TagDataPoint()
        {
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Конструктор с параметрами
        /// </summary>
        public TagDataPoint(TagDefinition tag, object value)
        {
            Tag = tag;
            Value = value;
            Timestamp = DateTime.Now;
        }
    }
}