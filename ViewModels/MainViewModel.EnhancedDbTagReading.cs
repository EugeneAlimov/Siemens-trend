using System;
using System.Threading.Tasks;
using SiemensTrend.Communication.TIA;
using SiemensTrend.Core.Models;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Расширение MainViewModel для работы с улучшенным чтением тегов DB
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// Получение всех тегов проекта с использованием улучшенного безопасного подхода
        /// </summary>
        public async Task GetAllTagsSafeAsync()
        {
            try
            {
                if (_tiaPortalService == null)
                {
                    _logger.Error("GetAllTagsSafeAsync: Сервис TIA Portal не инициализирован");
                    StatusMessage = "Ошибка: сервис TIA Portal не инициализирован";
                    return;
                }

                IsLoading = true;
                StatusMessage = "Получение всех тегов проекта в безопасном режиме...";
                ProgressValue = 10;

                // Используем безопасный режим для получения всех тегов
                var plcData = await _tiaPortalService.GetAllProjectTagsSafeAsync(
                    TiaPortalTagReaderFactory.ReaderMode.SafeMode);

                ProgressValue = 90;

                // Очищаем и заполняем коллекции
                PlcTags.Clear();
                foreach (var tag in plcData.PlcTags)
                {
                    PlcTags.Add(tag);
                }

                DbTags.Clear();
                foreach (var tag in plcData.DbTags)
                {
                    DbTags.Add(tag);
                }

                StatusMessage = $"Получено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB";
                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetAllTagsSafeAsync: Ошибка: {ex.Message}");
                StatusMessage = "Ошибка получения тегов проекта";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Получение тегов DB с использованием улучшенного безопасного подхода
        /// </summary>
        public async Task GetDbTagsSafeAsync()
        {
            try
            {
                if (_tiaPortalService == null)
                {
                    _logger.Error("GetDbTagsSafeAsync: Сервис TIA Portal не инициализирован");
                    StatusMessage = "Ошибка: сервис TIA Portal не инициализирован";
                    return;
                }

                IsLoading = true;
                StatusMessage = "Получение тегов DB в безопасном режиме...";
                ProgressValue = 10;

                // Используем безопасный метод для получения тегов DB
                var dbTags = await _tiaPortalService.GetDbTagsSafeAsync(
                    TiaPortalTagReaderFactory.ReaderMode.SafeMode);

                ProgressValue = 90;

                DbTags.Clear();
                foreach (var tag in dbTags)
                {
                    DbTags.Add(tag);
                }

                StatusMessage = $"Получено {dbTags.Count} тегов DB";
                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetDbTagsSafeAsync: Ошибка: {ex.Message}");
                StatusMessage = "Ошибка получения тегов DB";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Экспорт тегов в XML с улучшенным подходом
        /// </summary>
        public async Task ExportTagsToXmlEnhanced()
        {
            try
            {
                if (_tiaPortalService == null || !IsConnected)
                {
                    StatusMessage = "Необходимо сначала подключиться к TIA Portal";
                    return;
                }

                IsLoading = true;
                StatusMessage = "Экспорт тегов в XML с улучшенным подходом...";
                ProgressValue = 10;

                // Используем улучшенный метод экспорта
                await _tiaPortalService.ExportTagsToXmlEnhanced();

                ProgressValue = 100;
                StatusMessage = "Экспорт тегов в XML завершен";

                _logger.Info("ExportTagsToXmlEnhanced: Экспорт тегов с улучшенным подходом завершен успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXmlEnhanced: Ошибка: {ex.Message}");
                StatusMessage = "Ошибка при экспорте тегов";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Получение информации о доступных режимах чтения тегов
        /// </summary>
        /// <returns>Массив доступных режимов</returns>
        public TiaPortalTagReaderFactory.ReaderMode[] GetAvailableReaderModes()
        {
            return (TiaPortalTagReaderFactory.ReaderMode[])Enum.GetValues(typeof(TiaPortalTagReaderFactory.ReaderMode));
        }

        /// <summary>
        /// Текущий выбранный режим чтения тегов
        /// </summary>
        private TiaPortalTagReaderFactory.ReaderMode _selectedReaderMode = TiaPortalTagReaderFactory.ReaderMode.SafeMode;

        /// <summary>
        /// Свойство для текущего режима чтения тегов
        /// </summary>
        public TiaPortalTagReaderFactory.ReaderMode SelectedReaderMode
        {
            get => _selectedReaderMode;
            set => SetProperty(ref _selectedReaderMode, value);
        }

        /// <summary>
        /// Описание текущего режима чтения тегов
        /// </summary>
        public string CurrentReaderModeDescription
        {
            get
            {
                switch (SelectedReaderMode)
                {
                    case TiaPortalTagReaderFactory.ReaderMode.Standard:
                        return "Стандартный режим: полная обработка тегов";
                    case TiaPortalTagReaderFactory.ReaderMode.SafeMode:
                        return "Безопасный режим: ограниченная обработка для повышения стабильности";
                    case TiaPortalTagReaderFactory.ReaderMode.MinimalMode:
                        return "Минимальный режим: только базовая информация о тегах";
                    default:
                        return "Неизвестный режим";
                }
            }
        }
    }
}