using System;
using System.Collections.Generic;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Storage.Project
{
    /// <summary>
    /// Класс конфигурации проекта
    /// </summary>
    public class ProjectConfiguration
    {
        /// <summary>
        /// Имя проекта
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Описание проекта
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Дата создания проекта
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Дата последнего изменения проекта
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Параметры подключения
        /// </summary>
        public ConnectionSettings ConnectionSettings { get; set; }

        /// <summary>
        /// Список тегов для мониторинга
        /// </summary>
        public List<TagDefinition> MonitoredTags { get; set; }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public ProjectConfiguration()
        {
            Name = "Новый проект";
            Description = "Описание проекта";
            Created = DateTime.Now;
            LastModified = DateTime.Now;
            ConnectionSettings = new ConnectionSettings();
            MonitoredTags = new List<TagDefinition>();
        }
    }

    /// <summary>
    /// Настройки подключения к ПЛК
    /// </summary>
    public class ConnectionSettings
    {
        /// <summary>
        /// IP-адрес ПЛК
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Номер стойки (Rack)
        /// </summary>
        public short Rack { get; set; } = 0;

        /// <summary>
        /// Номер слота (Slot)
        /// </summary>
        public short Slot { get; set; } = 1;

        /// <summary>
        /// Тип соединения
        /// </summary>
        public ConnectionType ConnectionType { get; set; } = ConnectionType.S7;
    }

    /// <summary>
    /// Тип соединения с ПЛК
    /// </summary>
    public enum ConnectionType
    {
        S7,
        TiaPortal,
        OpcUa
    }
}