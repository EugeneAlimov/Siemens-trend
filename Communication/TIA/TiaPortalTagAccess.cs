using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Siemens.Engineering.SW;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Часть сервиса TiaPortalCommunicationService для работы с тегами
    /// </summary>
    public partial class TiaPortalCommunicationService
    {
        /// <summary>
        /// Экспорт всех типов тегов из PlcSoftware
        /// </summary>
        private async Task ExportAllTagsToXml(PlcSoftware plcSoftware)
        {
            _logger.Info("ExportAllTagsToXml: Начало экспорта всех типов тегов");

            try
            {
                // Экспорт таблиц тегов
                _xmlManager.ExportTagTablesToXml(plcSoftware.TagTableGroup);

                // Экспорт блоков данных
                _xmlManager.ExportDataBlocksToXml(plcSoftware.BlockGroup);

                // Ждем небольшую паузу для завершения операций
                await Task.Delay(100);

                _logger.Info("ExportAllTagsToXml: Экспорт завершен успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportAllTagsToXml: Ошибка при экспорте: {ex.Message}");
                throw; // Пробрасываем исключение для обработки в вызывающем методе
            }
        }

        /// <summary>
        /// Получение только тегов ПЛК из проекта
        /// </summary>
        public async Task<List<TagDefinition>> GetPlcTagsAsync()
        {
            try
            {
                // Убедимся, что текущий проект установлен в XML менеджере
                if (_project != null && !string.IsNullOrEmpty(_project.Name))
                {
                    SetCurrentProjectInXmlManager();
                }

                // Проверка наличия XML-файлов
                var tagsFromXml = _xmlManager.LoadPlcTagsFromXml();
                if (tagsFromXml.Count > 0)
                {
                    _logger.Info($"GetPlcTagsAsync: Загружено {tagsFromXml.Count} тегов из XML");
                    return tagsFromXml;
                }

                // Если XML нет или они пустые, экспортируем и затем загружаем
                if (IsConnected && _project != null)
                {
                    await ExportTagsToXml(ExportTagType.PlcTags); // Только теги ПЛК
                    tagsFromXml = _xmlManager.LoadPlcTagsFromXml();
                    _logger.Info($"GetPlcTagsAsync: Экспортировано и загружено {tagsFromXml.Count} тегов");
                    return tagsFromXml;
                }

                _logger.Error("GetPlcTagsAsync: Нет подключения к TIA Portal и отсутствуют XML");
                return new List<TagDefinition>();
            }
            catch (Exception ex)
            {
                _logger.Error($"GetPlcTagsAsync: Ошибка: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Загрузка и возврат всех тегов проекта
        /// </summary>
        public async Task<PlcData> GetAllProjectTagsAsync()
        {
            if (!IsConnected || _project == null)
            {
                _logger.Error("GetAllProjectTagsAsync: Попытка получения тегов без подключения к TIA Portal");
                return new PlcData();
            }

            try
            {
                _logger.Info("GetAllProjectTagsAsync: Запуск чтения всех тегов проекта");

                // Создаем читатель тегов с помощью фабрики
                var tagReader = TiaPortalTagReaderFactory.CreateTagReader(_logger, this);

                // ВАЖНО: Не используем Task.Run для Openness API!
                // Вместо этого используем Task.FromResult для асинхронной обертки синхронного метода
                var plcData = await Task.FromResult(tagReader.ReadAllTags());

                _logger.Info($"GetAllProjectTagsAsync: Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");
                return plcData;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetAllProjectTagsAsync: Ошибка при получении всех тегов проекта: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"GetAllProjectTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
                }
                return new PlcData();
            }
        }
        /// <summary>
        /// Получение только тегов блоков данных из проекта
        /// </summary>
        public async Task<List<TagDefinition>> GetDbTagsAsync()
        {
            try
            {
                // Убедимся, что текущий проект установлен в XML менеджере
                if (_project != null && !string.IsNullOrEmpty(_project.Name))
                {
                    SetCurrentProjectInXmlManager();
                }

                // Проверка наличия XML-файлов
                var dbsFromXml = _xmlManager.LoadDbTagsFromXml();
                if (dbsFromXml.Count > 0)
                {
                    _logger.Info($"GetDbTagsAsync: Загружено {dbsFromXml.Count} блоков данных из XML");
                    return dbsFromXml;
                }

                // Если XML нет или они пустые, экспортируем и затем загружаем
                if (IsConnected && _project != null)
                {
                    await ExportTagsToXml(ExportTagType.DbTags); // Только теги DB
                    dbsFromXml = _xmlManager.LoadDbTagsFromXml();
                    _logger.Info($"GetDbTagsAsync: Экспортировано и загружено {dbsFromXml.Count} блоков данных");
                    return dbsFromXml;
                }

                _logger.Error("GetDbTagsAsync: Нет подключения к TIA Portal и отсутствуют XML");
                return new List<TagDefinition>();
            }
            catch (Exception ex)
            {
                _logger.Error($"GetDbTagsAsync: Ошибка: {ex.Message}");
                return new List<TagDefinition>();
            }
        }
    }
}