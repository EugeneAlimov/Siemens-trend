using System;

namespace SiemensTrend.Core.Models
{
    /// <summary>
    /// Тип данных тега
    /// </summary>
    public enum TagDataType
    {
        Bool,   // Логический тип (BOOL)
        Int,    // Целое число (INT)
        DInt,   // Двойное целое (DINT)
        Real,   // Число с плавающей точкой (REAL)
        Other   // Другой тип данных
    }

    /// <summary>
    /// Определение тега для мониторинга
    /// </summary>
    public class TagDefinition
    {
        /// <summary>
        /// Уникальный идентификатор тега
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Имя тега
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Адрес тега
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Тип данных
        /// </summary>
        public TagDataType DataType { get; set; }

        /// <summary>
        /// Комментарий
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Группа тега (таблица, блок данных)
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// Полное имя тега, включая группу
        /// </summary>
        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(GroupName))
                    return Name;
                return $"{GroupName}.{Name}";
            }
        }

        /// <summary>
        /// Строковое представление
        /// </summary>
        public override string ToString()
        {
            return $"{Name} : {DataType} @ {Address}";
        }
    }
}