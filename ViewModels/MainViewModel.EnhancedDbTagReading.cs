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