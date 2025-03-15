using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication
{
    /// <summary>
    /// Аргументы события получения данных
    /// </summary>
    public class TagDataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Полученные точки данных
        /// </summary>
        public List<TagDataPoint> DataPoints { get; }

        public TagDataReceivedEventArgs(List<TagDataPoint> dataPoints)
        {
            DataPoints = dataPoints;
        }
    }

    /// <summary>
    /// Общий интерфейс для сервисов коммуникации с ПЛК
    /// </summary>
    public interface ICommunicationService
    {
        /// <summary>
        /// Событие получения новых данных
        /// </summary>
        event EventHandler<TagDataReceivedEventArgs> DataReceived;

        /// <summary>
        /// Событие изменения состояния соединения
        /// </summary>
        event EventHandler<bool> ConnectionStateChanged;

        /// <summary>
        /// Состояние соединения
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Интервал опроса тегов в миллисекундах
        /// </summary>
        int PollingIntervalMs { get; set; }

        /// <summary>
        /// Подключение к ПЛК
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Отключение от ПЛК
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Чтение значения тега
        /// </summary>
        Task<object> ReadTagAsync(TagDefinition tag);

        /// <summary>
        /// Начало мониторинга тегов
        /// </summary>
        Task StartMonitoringAsync(IEnumerable<TagDefinition> tags);

        /// <summary>
        /// Остановка мониторинга тегов
        /// </summary>
        Task StopMonitoringAsync();
    }
}