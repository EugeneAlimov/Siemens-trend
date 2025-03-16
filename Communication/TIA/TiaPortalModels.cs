using System.Collections.Generic;
using Siemens.Engineering;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Информация о проекте TIA Portal
    /// </summary>
    public class TiaProjectInfo
    {
        /// <summary>
        /// Имя проекта
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Путь к проекту
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Процесс TIA Portal
        /// </summary>
        public TiaPortalProcess TiaProcess { get; set; }

        /// <summary>
        /// Экземпляр TIA Portal
        /// </summary>
        public TiaPortal TiaPortalInstance { get; set; }

        /// <summary>
        /// Объект проекта
        /// </summary>
        public Project Project { get; set; }

        /// <summary>
        /// Строковое представление
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Коллекция тегов PLC
    /// </summary>
    public class PlcTagCollection
    {
        /// <summary>
        /// Имя таблицы тегов
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Список тегов таблицы
        /// </summary>
        public List<PlcTag> Tags { get; set; } = new List<PlcTag>();
    }

    /// <summary>
    /// Тег PLC
    /// </summary>
    public class PlcTag
    {
        /// <summary>
        /// Имя тега
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Имя таблицы тегов
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Адрес тега
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Тип данных тега
        /// </summary>
        public TagDataType DataType { get; set; }

        /// <summary>
        /// Комментарий к тегу
        /// </summary>
        public string Comment { get; set; }
    }

    /// <summary>
    /// Коллекция тегов DB
    /// </summary>
    public class DbTagCollection
    {
        /// <summary>
        /// Имя блока данных
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Флаг оптимизированного блока данных
        /// </summary>
        public bool IsOptimized { get; set; }

        /// <summary>
        /// Список тегов блока данных
        /// </summary>
        public List<DbTag> Tags { get; set; } = new List<DbTag>();
    }

    /// <summary>
    /// Тег DB
    /// </summary>
    public class DbTag
    {
        /// <summary>
        /// Имя тега
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Полное имя тега включая DB
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Имя блока данных
        /// </summary>
        public string DbName { get; set; }

        /// <summary>
        /// Путь к тегу в структуре данных
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Адрес тега
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Тип данных тега
        /// </summary>
        public TagDataType DataType { get; set; }

        /// <summary>
        /// Флаг оптимизированного размещения
        /// </summary>
        public bool IsOptimized { get; set; }

        /// <summary>
        /// Смещение тега в блоке данных
        /// </summary>
        public int Offset { get; set; }
    }
}