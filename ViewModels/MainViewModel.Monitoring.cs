using System;
using System.Threading.Tasks;
using SiemensTrend.Core.Models;
using System.Linq;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Часть MainViewModel для работы с мониторингом тегов
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// Отключение от ПЛК
        /// </summary>
        public void Disconnect()
        {
            if (IsLoading || !IsConnected)
                return;

            try
            {
                _communicationService.Disconnect();

                // Если есть TIA Portal сервис, отключаем и его
                _tiaPortalService?.Disconnect();

                StatusMessage = "Отключено от ПЛК";

                // Очищаем списки тегов
                AvailableTags.Clear();
                MonitoredTags.Clear();
                PlcTags.Clear();
                DbTags.Clear();

                IsConnected = false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отключении: {ex.Message}");
                StatusMessage = "Ошибка при отключении";
            }
        }

        /// <summary>
        /// Начало мониторинга выбранных тегов
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            if (!IsConnected || MonitoredTags.Count == 0)
                return;

            try
            {
                StatusMessage = "Запуск мониторинга...";
                await _communicationService.StartMonitoringAsync(MonitoredTags);
                StatusMessage = $"Мониторинг запущен для {MonitoredTags.Count} тегов";
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при запуске мониторинга: {ex.Message}");
                StatusMessage = "Ошибка при запуске мониторинга";
            }
        }

        /// <summary>
        /// Остановка мониторинга
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            if (!IsConnected)
                return;

            try
            {
                StatusMessage = "Остановка мониторинга...";
                await _communicationService.StopMonitoringAsync();
                StatusMessage = "Мониторинг остановлен";
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при остановке мониторинга: {ex.Message}");
                StatusMessage = "Ошибка при остановке мониторинга";
            }
        }
    }
}