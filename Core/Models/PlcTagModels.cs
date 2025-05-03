using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Конструктор по умолчанию для сериализации
        /// </summary>
        public TagDefinition()
    {
        Id = Guid.NewGuid();
    }
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

        /// <summary>
        /// Очистка данных
        /// </summary>
        public void Clear()
        {
            PlcTags.Clear();
            DbTags.Clear();
        }

        /// <summary>
        /// Получение тега по имени
        /// </summary>
        public TagDefinition GetTagByName(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return null;

            // Ищем сначала в PlcTags
            var tag = PlcTags.FirstOrDefault(t => t.Name == tagName);
            if (tag != null)
                return tag;

            // Затем в DbTags
            return DbTags.FirstOrDefault(t => t.Name == tagName);
        }

        /// <summary>
        /// Поиск тегов по частичному совпадению имени
        /// </summary>
        public List<TagDefinition> FindTagsByPartialName(string partialName)
        {
            if (string.IsNullOrEmpty(partialName))
                return new List<TagDefinition>();

            string lowercasePartialName = partialName.ToLower();

            var result = new List<TagDefinition>();
            result.AddRange(PlcTags.Where(t => t.Name.ToLower().Contains(lowercasePartialName)));
            result.AddRange(DbTags.Where(t => t.Name.ToLower().Contains(lowercasePartialName)));
            return result;
        }
    }
}

