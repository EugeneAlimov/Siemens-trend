using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Siemens.Engineering.SW;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.Helpers;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Расширения для класса TiaPortalCommunicationService
    /// с интеграцией улучшенного чтения тегов DB
    /// </summary>
    public partial class TiaPortalCommunicationService
    {
        /// <summary>
        /// Экспорт тегов с улучшенным подходом к блокам данных
        /// </summary>
        /// <param name="tagType">Тип тегов для экспорта</param>
        public async Task ExportTagsToXmlEnhanced(ExportTagType tagType = ExportTagType.All)
        {
            if (!IsConnected || _project == null)
            {
                _logger.Error("ExportTagsToXmlEnhanced: Нет подключения к TIA Portal");
                return;
            }

            var plcSoftware = GetPlcSoftware();
            if (plcSoftware == null)
            {
                _logger.Error("ExportTagsToXmlEnhanced: Не удалось получить PlcSoftware");
                return;
            }

            // Сначала проверяем, настроен ли XML-менеджер для текущего проекта
            if (_project != null && !string.IsNullOrEmpty(_project.Name))
            {
                SetCurrentProjectInXmlManager();
            }

            // Экспортируем в зависимости от типа
            try
            {
                _logger.Info($"ExportTagsToXmlEnhanced: Экспорт {tagType} тегов начат с улучшенным подходом");

                switch (tagType)
                {
                    case ExportTagType.All:
                        // Экспортируем все типы тегов
                        await ExportAllTagsToXmlEnhanced(plcSoftware);
                        break;
                    case ExportTagType.PlcTags:
                        // Только теги ПЛК - используем стандартный метод
                        _xmlManager.ExportTagTablesToXml(plcSoftware.TagTableGroup);
                        break;
                    case ExportTagType.DbTags:
                        // Только теги DB - используем улучшенный метод
                        _xmlManager.ExportEnhancedDataBlocksToXml(plcSoftware.BlockGroup);
                        break;
                    default:
                        _logger.Warn($"ExportTagsToXmlEnhanced: Неизвестный тип тегов: {tagType}");
                        break;
                }

                _logger.Info($"ExportTagsToXmlEnhanced: Экспорт {tagType} тегов завершен");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXmlEnhanced: Ошибка при экспорте {tagType} тегов: {ex.Message}");
            }
        }
        /// <summary>
        /// Экспорт всех типов тегов из PlcSoftware с улучшенным подходом
        /// </summary>
        private async Task ExportAllTagsToXmlEnhanced(PlcSoftware plcSoftware)
        {
            _logger.Info("ExportAllTagsToXmlEnhanced: Начало экспорта всех типов тегов");

            try
            {
                // Экспорт таблиц тегов - стандартный метод
                _xmlManager.ExportTagTablesToXml(plcSoftware.TagTableGroup);

                // Делаем небольшую паузу между операциями
                await Task.Delay(500);

                // Экспорт блоков данных - улучшенный метод
                _xmlManager.ExportEnhancedDataBlocksToXml(plcSoftware.BlockGroup);

                // Ждем небольшую паузу для завершения операций
                await Task.Delay(500);

                _logger.Info("ExportAllTagsToXmlEnhanced: Экспорт завершен успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportAllTagsToXmlEnhanced: Ошибка при экспорте: {ex.Message}");
                throw; // Пробрасываем исключение для обработки в вызывающем методе
            }
        }


        public async Task ExportTagsToXml(ExportTagType tagType = ExportTagType.All)
        {
            if (!IsConnected || _project == null)
            {
                _logger.Error("ExportTagsToXml: Нет подключения к TIA Portal");
                return;
            }

            var plcSoftware = GetPlcSoftware();
            if (plcSoftware == null)
            {
                _logger.Error("ExportTagsToXml: Не удалось получить PlcSoftware");
                return;
            }

            // Сначала проверяем, настроен ли XML-менеджер для текущего проекта
            if (_project != null && !string.IsNullOrEmpty(_project.Name))
            {
                SetCurrentProjectInXmlManager();
            }

            // Экспортируем в зависимости от типа
            try
            {
                _logger.Info($"ExportTagsToXml: Экспорт {tagType} тегов начат");

                switch (tagType)
                {
                    case ExportTagType.All:
                        // Экспортируем все типы тегов
                        await ExportAllTagsToXmlEnhanced(plcSoftware);
                        break;
                    case ExportTagType.PlcTags:
                        // Только теги ПЛК
                        _xmlManager.ExportTagTablesToXml(plcSoftware.TagTableGroup);
                        break;
                    case ExportTagType.DbTags:
                        // Только теги DB - используем улучшенный метод вместо обычного
                        _xmlManager.ExportEnhancedDataBlocksToXml(plcSoftware.BlockGroup);
                        break;
                    default:
                        _logger.Warn($"ExportTagsToXml: Неизвестный тип тегов: {tagType}");
                        break;
                }

                _logger.Info($"ExportTagsToXml: Экспорт {tagType} тегов завершен");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXml: Ошибка при экспорте {tagType} тегов: {ex.Message}");
            }
        }
    }
}