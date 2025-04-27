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
        /// Получение всех тегов проекта с использованием улучшенного подхода
        /// </summary>
        /// <param name="readerMode">Режим чтения тегов</param>
        /// <returns>Объект с данными ПЛК</returns>
        public async Task<PlcData> GetAllProjectTagsSafeAsync(
            TiaPortalTagReaderFactory.ReaderMode readerMode = TiaPortalTagReaderFactory.ReaderMode.SafeMode)
        {
            if (!IsConnected || _project == null)
            {
                _logger.Error("GetAllProjectTagsSafeAsync: Попытка получения тегов без подключения к TIA Portal");
                return new PlcData();
            }

            try
            {
                _logger.Info($"GetAllProjectTagsSafeAsync: Запуск безопасного чтения всех тегов проекта в режиме {readerMode}");

                // Создаем читатель тегов с помощью фабрики, указывая режим чтения
                var tagReader = TiaPortalTagReaderFactory.CreateTagReader(_logger, this, readerMode);

                // Асинхронная обертка для синхронного метода
                var plcData = await Task.Run(() => tagReader.ReadAllTags());

                _logger.Info($"GetAllProjectTagsSafeAsync: Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");
                return plcData;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetAllProjectTagsSafeAsync: Ошибка при получении всех тегов проекта: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"GetAllProjectTagsSafeAsync: Внутренняя ошибка: {ex.InnerException.Message}");
                }
                return new PlcData();
            }
        }

        /// <summary>
        /// Получение только тегов блоков данных с улучшенным подходом
        /// </summary>
        /// <param name="readerMode">Режим чтения тегов</param>
        /// <returns>Список тегов DB</returns>
        public async Task<List<TagDefinition>> GetDbTagsSafeAsync(
            TiaPortalTagReaderFactory.ReaderMode readerMode = TiaPortalTagReaderFactory.ReaderMode.SafeMode)
        {
            try
            {
                _logger.Info($"GetDbTagsSafeAsync: Начало улучшенного чтения тегов DB в режиме {readerMode}");

                // Если возможно прямое чтение из проекта
                if (IsConnected && _project != null)
                {
                    _logger.Info("GetDbTagsSafeAsync: Активное подключение к TIA Portal, пробуем прямое чтение");

                    // Создаем специализированный читатель DB-тегов
                    var dbTagReader = TiaPortalTagReaderFactory.CreateDbTagReader(_logger, this, readerMode);

                    // Создаем пустой объект для хранения данных
                    var plcData = new PlcData();

                    // Читаем только блоки данных
                    await Task.Run(() => dbTagReader.ReadDataBlocks(plcData));

                    _logger.Info($"GetDbTagsSafeAsync: Прямое чтение из TIA Portal выполнено, получено {plcData.DbTags.Count} тегов DB");
                    return plcData.DbTags;
                }

                // Убедимся, что текущий проект установлен в XML менеджере
                if (_project != null && !string.IsNullOrEmpty(_project.Name))
                {
                    SetCurrentProjectInXmlManager();
                }

                // Проверка наличия XML-файлов в кэше
                var dbsFromXml = _xmlManager.LoadDbTagsFromXml();
                if (dbsFromXml.Count > 0)
                {
                    _logger.Info($"GetDbTagsSafeAsync: Загружено {dbsFromXml.Count} тегов блоков данных из XML");
                    return dbsFromXml;
                }

                // Если XML нет или они пустые, и есть подключение, экспортируем 
                if (IsConnected && _project != null)
                {
                    _logger.Info("GetDbTagsSafeAsync: XML не найдены, выполняем экспорт");
                    
                    // Получаем PlcSoftware
                    var plcSoftware = GetPlcSoftware();
                    if (plcSoftware != null)
                    {
                        // Используем улучшенный метод XML-менеджера для создания XML-файлов с расширенной информацией
                        _xmlManager.ExportEnhancedDataBlocksToXml(plcSoftware.BlockGroup);
                        
                        // Загружаем созданные XML
                        dbsFromXml = _xmlManager.LoadDbTagsFromXml();
                        _logger.Info($"GetDbTagsSafeAsync: После экспорта загружено {dbsFromXml.Count} тегов блоков данных");
                        return dbsFromXml;
                    }
                }

                _logger.Error("GetDbTagsSafeAsync: Не удалось получить теги DB ни одним из способов");
                return new List<TagDefinition>();
            }
            catch (Exception ex)
            {
                _logger.Error($"GetDbTagsSafeAsync: Ошибка: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

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
        /// <summary>
        /// Проверка наличия кэшированных данных для проекта
        /// </summary>
        /// <param name="projectName">Имя проекта</param>
        /// <returns>True, если есть кэшированные данные</returns>
        public bool HasCachedDbTags(string projectName)
        {
            try
            {
                if (string.IsNullOrEmpty(projectName))
                {
                    _logger.Warn("HasCachedDbTags: Пустое имя проекта");
                    return false;
                }
                
                return _xmlManager.HasExportedDataForProject(projectName);
            }
            catch (Exception ex)
            {
                _logger.Error($"HasCachedDbTags: Ошибка при проверке кэша: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Очистка кэша тегов DB для текущего проекта
        /// </summary>
        /// <returns>True, если кэш успешно очищен</returns>
        public bool ClearDbTagsCache()
        {
            try
            {
                if (_project == null || string.IsNullOrEmpty(_project.Name))
                {
                    _logger.Warn("ClearDbTagsCache: Нет активного проекта");
                    return false;
                }
                
                string projectName = _project.Name;
                bool result = _xmlManager.ClearProjectCache(projectName);
                
                _logger.Info($"ClearDbTagsCache: Кэш для проекта {projectName} {(result ? "успешно очищен" : "не удалось очистить")}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"ClearDbTagsCache: Ошибка при очистке кэша: {ex.Message}");
                return false;
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