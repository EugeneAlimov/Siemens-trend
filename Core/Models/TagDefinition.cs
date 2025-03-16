//using System;

//namespace SiemensTrend.Core.Models
//{
//    /// <summary>
//    /// Тип данных тега
//    /// </summary>
//    public enum TagDataType
//    {
//        Bool,   // Логический тип (BOOL)
//        Int,    // Целое число (INT)
//        DInt,   // Двойное целое (DINT)
//        Real,   // Число с плавающей точкой (REAL)
//        Other   // Другой тип данных
//    }

//    /// <summary>
//    /// Определение тега для мониторинга
//    /// </summary>
//    public class TagDefinition
//    {
//        /// <summary>
//        /// Уникальный идентификатор тега
//        /// </summary>
//        public Guid Id { get; set; } = Guid.NewGuid();

//        /// <summary>
//        /// Имя тега
//        /// </summary>
//        public string Name { get; set; }

//        /// <summary>
//        /// Адрес тега
//        /// </summary>
//        public string Address { get; set; }

//        /// <summary>
//        /// Тип данных
//        /// </summary>
//        public TagDataType DataType { get; set; }

//        /// <summary>
//        /// Комментарий
//        /// </summary>
//        public string Comment { get; set; }

//        /// <summary>
//        /// Группа тега (таблица, блок данных)
//        /// </summary>
//        public string GroupName { get; set; }

//        /// <summary>
//        /// Полное имя тега, включая группу
//        /// </summary>
//        public string FullName
//        {
//            get
//            {
//                if (string.IsNullOrEmpty(GroupName))
//                    return Name;
//                return $"{GroupName}.{Name}";
//            }
//        }

//        /// <summary>
//        /// Строковое представление
//        /// </summary>
//        public override string ToString()
//        {
//            return $"{Name} : {DataType} @ {Address}";
//        }
//    }
//}

using System;
using System.Collections.Generic;

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
        String, // Строка
        UDT,    // Пользовательский тип данных
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
        /// Оптимизированный тег (для DB)
        /// </summary>
        public bool IsOptimized { get; set; }

        /// <summary>
        /// Является ли тег UDT (пользовательский тип данных)
        /// </summary>
        public bool IsUDT { get; set; }

        /// <summary>
        /// Является ли тег Safety (безопасность)
        /// </summary>
        public bool IsSafety { get; set; }

        /// <summary>
        /// Является ли тег тегом блока данных
        /// </summary>
        public bool IsDbTag => GroupName?.StartsWith("DB") == true || Name?.Contains(".") == true;

        /// <summary>
        /// Полное имя тега, включая группу
        /// </summary>
        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(GroupName))
                    return Name;

                // Если имя уже содержит имя группы, возвращаем как есть
                if (Name.StartsWith(GroupName + "."))
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

    /// <summary>
    /// Класс для хранения данных ПЛК
    /// </summary>
    public class PlcData
    {
        /// <summary>
        /// Теги ПЛК
        /// </summary>
        public List<TagDefinition> PlcTags { get; set; } = new List<TagDefinition>();

        /// <summary>
        /// Теги блоков данных
        /// </summary>
        public List<TagDefinition> DbTags { get; set; } = new List<TagDefinition>();

        /// <summary>
        /// Все теги (PlcTags + DbTags)
        /// </summary>
        public List<TagDefinition> AllTags
        {
            get
            {
                var result = new List<TagDefinition>();
                result.AddRange(PlcTags);
                result.AddRange(DbTags);
                return result;
            }
        }
    }
}