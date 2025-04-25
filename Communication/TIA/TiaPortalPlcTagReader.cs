using System;
using System.Linq;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Класс для чтения тегов ПЛК из проекта TIA Portal
    /// </summary>
    public class TiaPortalPlcTagReader : TiaPortalTagReaderBase
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        public TiaPortalPlcTagReader(Logger logger, TiaPortalCommunicationService tiaService)
            : base(logger, tiaService)
        {
        }

        /// <summary>
        /// Чтение тегов ПЛК из проекта
        /// </summary>
        /// <param name="plcData">Объект для сохранения тегов</param>
        /// <returns>Количество прочитанных тегов</returns>
        public override int ReadPlcTags(PlcData plcData)
        {
            int tableCount = 0;
            int tagCount = 0;

            try
            {
                _logger.Info("Чтение тегов ПЛК из таблиц тегов...");

                // Получаем программное обеспечение ПЛК
                var plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    return 0;
                }

                if (plcSoftware.TagTableGroup == null)
                {
                    _logger.Error("ReadPlcTags: TagTableGroup не найдена в plcSoftware");
                    return 0;
                }

                // Добавим подробное логирование
                _logger.Info($"ReadPlcTags: TagTableGroup найдена: {plcSoftware.TagTableGroup.Name}");

                try
                {
                    // Логируем количество таблиц и групп перед обработкой
                    _logger.Info($"ReadPlcTags: Количество таблиц тегов: {plcSoftware.TagTableGroup.TagTables.Count}");
                    _logger.Info($"ReadPlcTags: Количество групп таблиц: {plcSoftware.TagTableGroup.Groups.Count}");

                    ProcessTagTableGroup(plcSoftware.TagTableGroup, plcData, ref tableCount, ref tagCount);

                    _logger.Info($"ReadPlcTags: Обработано {tableCount} таблиц тегов, найдено {tagCount} тегов ПЛК");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ReadPlcTags: Ошибка при обработке группы таблиц тегов: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error($"ReadPlcTags: Внутренняя ошибка: {ex.InnerException.Message}");
                    }
                }

                _logger.Info($"ReadPlcTags: Чтение тегов ПЛК завершено. Найдено {plcData.PlcTags.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"ReadPlcTags: Ошибка при чтении тегов ПЛК: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"ReadPlcTags: Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }

            return tagCount;
        }

        /// <summary>
        /// Пустая реализация метода чтения блоков данных (не используется в этом классе)
        /// </summary>
        public override int ReadDataBlocks(PlcData plcData)
        {
            // Этот метод не используется в данном классе, 
            // он реализован в TiaPortalDbTagReader
            return 0;
        }

        /// <summary>
        /// Рекурсивная обработка групп таблиц тегов
        /// </summary>
        private void ProcessTagTableGroup(PlcTagTableGroup group, PlcData plcData, ref int tableCount, ref int tagCount, string parentPath = "")
        {
            try
            {
                // Проверка параметров
                if (group == null)
                {
                    _logger.Error("ProcessTagTableGroup: group не может быть null");
                    return;
                }

                string groupName = string.IsNullOrEmpty(group.Name) ? "Default" : group.Name;
                _logger.Info($"ProcessTagTableGroup: Обработка группы таблиц тегов: {groupName}");

                // Обработка таблиц тегов в текущей группе
                if (group.TagTables != null)
                {
                    foreach (var tagTable in group.TagTables)
                    {
                        try
                        {
                            var plcTagTable = tagTable as PlcTagTable;
                            if (plcTagTable != null)
                            {
                                ProcessTagTable(plcTagTable, plcData, parentPath, ref tagCount);
                                tableCount++;
                            }
                            else
                            {
                                _logger.Warn($"ProcessTagTableGroup: Таблица тегов не может быть приведена к типу PlcTagTable");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"ProcessTagTableGroup: Ошибка при обработке таблицы тегов: {ex.Message}");
                        }
                    }
                }
                else
                {
                    _logger.Warn($"ProcessTagTableGroup: TagTables равен null в группе {groupName}");
                }

                // Рекурсивная обработка подгрупп
                if (group.Groups != null)
                {
                    foreach (var subgroup in group.Groups)
                    {
                        string newPath = string.IsNullOrEmpty(parentPath) ?
                            subgroup.Name : $"{parentPath}/{subgroup.Name}";

                        var userGroup = subgroup as PlcTagTableUserGroup;
                        if (userGroup != null)
                        {
                            ProcessTagTableGroup(userGroup, plcData, ref tableCount, ref tagCount, newPath);
                        }
                        else
                        {
                            _logger.Warn($"ProcessTagTableGroup: Подгруппа не может быть приведена к типу PlcTagTableUserGroup");
                        }
                    }
                }
                else
                {
                    _logger.Warn($"ProcessTagTableGroup: Groups равен null в группе {groupName}");
                }

                // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                GC.KeepAlive(group);
            }
            catch (Exception ex)
            {
                _logger.Error($"ProcessTagTableGroup: Ошибка при обработке группы таблиц тегов: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка таблицы тегов
        /// </summary>
        private void ProcessTagTable(PlcTagTable tagTable, PlcData plcData, string groupPath, ref int tagCount)
        {
            if (tagTable == null)
            {
                _logger.Warn("ProcessTagTable: Получена пустая таблица тегов");
                return;
            }

            _logger.Info($"ProcessTagTable: Обработка таблицы тегов: {tagTable.Name}");

            try
            {
                // Проверяем наличие тегов в таблице
                if (tagTable.Tags == null || tagTable.Tags.Count == 0)
                {
                    _logger.Warn($"ProcessTagTable: Таблица тегов {tagTable.Name} не содержит тегов");
                    return;
                }

                _logger.Info($"ProcessTagTable: Количество тегов в таблице {tagTable.Name}: {tagTable.Tags.Count}");

                // Обрабатываем каждый тег в таблице
                foreach (var tag in tagTable.Tags)
                {
                    try
                    {
                        // Приводим к типу PlcTag
                        var plcTag = tag as Siemens.Engineering.SW.Tags.PlcTag;
                        if (plcTag == null)
                        {
                            _logger.Debug($"ProcessTagTable: Тег не может быть приведен к типу PlcTag, пропускаем");
                            continue;
                        }

                        // Получаем атрибуты тега безопасным способом
                        string name = plcTag.Name;
                        string dataTypeString = "Unknown";
                        string address = "";
                        string comment = "";

                        try { dataTypeString = plcTag.DataTypeName?.ToString() ?? "Unknown"; }
                        catch (Exception ex) { _logger.Debug($"ProcessTagTable: Ошибка при получении типа данных: {ex.Message}"); }

                        try { address = plcTag.LogicalAddress; }
                        catch (Exception ex) { _logger.Debug($"ProcessTagTable: Ошибка при получении адреса: {ex.Message}"); }

                        try { comment = plcTag.Comment?.ToString() ?? ""; }
                        catch (Exception ex) { _logger.Debug($"ProcessTagTable: Ошибка при получении комментария: {ex.Message}"); }

                        // Конвертируем строковый тип данных в TagDataType
                        TagDataType dataType = TiaTagTypeUtility.ConvertToTagDataType(dataTypeString);

                        // Проверяем, поддерживается ли тип данных
                        if (!TiaTagTypeUtility.IsSupportedTagType(dataType))
                        {
                            _logger.Debug($"ProcessTagTable: Пропущен тег {name} с неподдерживаемым типом данных {dataTypeString}");
                            continue;
                        }

                        // Создаем и добавляем тег в коллекцию
                        var tagDefinition = new TagDefinition
                        {
                            Name = name,
                            Address = address,
                            DataType = dataType,
                            Comment = comment,
                            GroupName = tagTable.Name,
                            IsOptimized = false,  // Теги ПЛК не бывают оптимизированными
                            IsUDT = dataTypeString.StartsWith("UDT_") || dataTypeString.Contains("type")
                        };

                        plcData.PlcTags.Add(tagDefinition);
                        tagCount++;
                        _logger.Debug($"ProcessTagTable: Добавлен тег ПЛК: {name} ({dataTypeString}) @ {address}");

                        // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                        GC.KeepAlive(plcTag);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"ProcessTagTable: Ошибка при обработке тега: {ex.Message}");
                    }
                }

                // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                GC.KeepAlive(tagTable);
            }
            catch (Exception ex)
            {
                _logger.Error($"ProcessTagTable: Ошибка при обработке таблицы тегов {tagTable.Name}: {ex.Message}");
            }
        }
    }
}