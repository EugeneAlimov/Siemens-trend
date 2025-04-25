using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.Communication.TIA;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Улучшенная реализация класса для чтения тегов из проекта TIA Portal
    /// </summary>
    public class TiaPortalTagReader
    {
        private readonly Logger _logger;
        private readonly TiaPortalCommunicationService _tiaService;

        /// <summary>
        /// Конструктор
        /// </summary>
        public TiaPortalTagReader(Logger logger, TiaPortalCommunicationService tiaService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tiaService = tiaService ?? throw new ArgumentNullException(nameof(tiaService));
        }

        /// <summary>
        /// Синхронное чтение всех тегов из проекта
        /// !!! ЭТОТ МЕТОД ТЕПЕРЬ ИСПОЛЬЗУЕТ БЕЗОПАСНУЮ РЕАЛИЗАЦИЮ ReadDataBlocksSafe !!!
        /// </summary>
        public PlcData ReadAllTags()
        {
            var plcData = new PlcData();

            try
            {
                _logger.Info("ReadAllTags: Чтение тегов из проекта TIA Portal...");

                // Получаем программное обеспечение ПЛК
                var plcSoftware = _tiaService.GetPlcSoftware();
                if (plcSoftware == null)
                {
                    _logger.Error("ReadAllTags: Не удалось получить PlcSoftware из проекта");
                    return plcData;
                }

                _logger.Info($"ReadAllTags: PlcSoftware получен успешно: {plcSoftware.Name}");

                // Сначала прочитаем только теги ПЛК в отдельной операции
                try
                {
                    _logger.Info("ReadAllTags: Начало чтения тегов ПЛК...");
                    int plcTagCount = 0;
                    int tableCount = 0;
                    ReadPlcTags(plcSoftware, plcData, ref tableCount, ref plcTagCount);
                    _logger.Info($"ReadAllTags: Теги ПЛК прочитаны успешно, найдено {plcData.PlcTags.Count} тегов");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ReadAllTags: Ошибка при чтении тегов ПЛК: {ex.Message}");
                }

                // Проверяем, что соединение еще активно
                if (!_tiaService.IsConnected || _tiaService.CurrentProject == null)
                {
                    _logger.Error("ReadAllTags: Соединение с TIA Portal потеряно после чтения тегов ПЛК");
                    return plcData; // Возвращаем только прочитанные теги PLC
                }

                // Получаем обновленный plcSoftware после проверки соединения
                plcSoftware = _tiaService.GetPlcSoftware();
                if (plcSoftware == null)
                {
                    _logger.Error("ReadAllTags: Не удалось получить PlcSoftware перед чтением DB");
                    return plcData;
                }

                // Только если соединение активно, попробуем прочитать DB теги
                try
                {
                    _logger.Info("ReadAllTags: Начало чтения тегов DB...");
                    int dbCount = 0;
                    int dbTagCount = 0;

                    // !!! ВАЖНО: Используем ТОЛЬКО безопасную версию метода чтения DB !!!
                    // Удаляем все вызовы ReadDataBlocks и ProcessBlockGroup
                    // и используем только новые безопасные методы
                    ReadDataBlocksSafe(plcSoftware, plcData, ref dbCount, ref dbTagCount);

                    _logger.Info($"ReadAllTags: Теги DB прочитаны успешно, найдено {plcData.DbTags.Count} тегов");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ReadAllTags: Ошибка при чтении тегов DB: {ex.Message}");
                    // Продолжаем выполнение, возвращая хотя бы теги PLC
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ReadAllTags: Общая ошибка при чтении тегов: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"ReadAllTags: Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }

            return plcData;
        }

        /// <summary>
        /// Улучшенный метод безопасного чтения блоков данных
        /// </summary>
        private void ReadDataBlocksSafe(PlcSoftware plcSoftware, PlcData plcData, ref int dbCount, ref int dbTagCount)
        {
            try
            {
                _logger.Info("Чтение блоков данных (безопасный режим)...");

                // Получаем список всех блоков данных напрямую, без рекурсивной обработки групп
                var allDataBlocks = new List<DataBlock>();

                // Рекурсивно собираем только ссылки на блоки данных
                CollectDataBlocks(plcSoftware.BlockGroup, allDataBlocks);

                _logger.Info($"Найдено {allDataBlocks.Count} блоков данных для обработки");

                // Обрабатываем каждый блок данных по отдельности с восстановлением после ошибок
                foreach (var db in allDataBlocks)
                {
                    try
                    {
                        _logger.Info($"Обработка блока данных: {db.Name}");

                        // Запрашиваем свойства каждого блока данных в отдельном try-catch
                        bool isOptimized = false;
                        bool isUDT = false;
                        bool isSafety = false;

                        try { isOptimized = db.MemoryLayout == MemoryLayout.Optimized; }
                        catch (Exception ex) { _logger.Debug($"Ошибка при определении оптимизации: {ex.Message}"); }

                        try { isUDT = db.Name.Contains("UDT") || db.Name.Contains("Type"); }
                        catch (Exception ex) { _logger.Debug($"Ошибка при определении UDT: {ex.Message}"); }

                        try
                        {
                            var programmingLanguage = db.GetAttribute("ProgrammingLanguage")?.ToString();
                            isSafety = programmingLanguage == "F_DB";
                        }
                        catch (Exception ex) { _logger.Debug($"Ошибка при определении Safety: {ex.Message}"); }

                        // Осторожно обрабатываем члены блока данных
                        ProcessDbMembersSafe(db, plcData, isOptimized, isUDT, isSafety, ref dbTagCount);

                        // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                        GC.KeepAlive(db);

                        dbCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при обработке блока данных {db.Name}: {ex.Message}");
                        // Пропускаем проблемный блок и продолжаем с другими
                    }

                    // Проверяем соединение после каждого блока
                    if (!_tiaService.IsConnected)
                    {
                        _logger.Error("Соединение с TIA Portal потеряно во время обработки блоков данных");
                        break;
                    }
                }

                _logger.Info($"Обработано {dbCount} блоков данных, найдено {dbTagCount} тегов DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при безопасном чтении блоков данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Улучшенный метод для сбора всех блоков данных
        /// </summary>
        private void CollectDataBlocks(PlcBlockGroup group, List<DataBlock> dataBlocks)
        {
            try
            {
                // Проверка аргументов
                if (group == null)
                {
                    _logger.Warn("CollectDataBlocks: group не может быть null");
                    return;
                }

                // Логгируем группу
                _logger.Debug($"CollectDataBlocks: Обработка группы блоков: {group.Name}");

                // Собираем блоки данных из текущей группы
                try
                {
                    // Проверка наличия доступа к Blocks
                    if (group.Blocks == null)
                    {
                        _logger.Warn($"CollectDataBlocks: Blocks равен null в группе {group.Name}");
                        return;
                    }

                    _logger.Debug($"CollectDataBlocks: Количество блоков в группе {group.Name}: {group.Blocks.Count}");

                    foreach (var block in group.Blocks)
                    {
                        try
                        {
                            if (block is DataBlock db)
                            {
                                dataBlocks.Add(db);
                                _logger.Debug($"CollectDataBlocks: Добавлен DB: {db.Name}");

                                // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                                GC.KeepAlive(db);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"CollectDataBlocks: Ошибка при обработке блока: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"CollectDataBlocks: Ошибка при доступе к блокам группы {group.Name}: {ex.Message}");
                }

                // Рекурсивно обрабатываем подгруппы
                try
                {
                    // Проверка наличия доступа к Groups
                    if (group.Groups == null)
                    {
                        _logger.Warn($"CollectDataBlocks: Groups равен null в группе {group.Name}");
                        return;
                    }

                    _logger.Debug($"CollectDataBlocks: Количество подгрупп в группе {group.Name}: {group.Groups.Count}");

                    foreach (var subgroup in group.Groups)
                    {
                        try
                        {
                            CollectDataBlocks(subgroup as PlcBlockGroup, dataBlocks);
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"CollectDataBlocks: Пропуск подгруппы из-за ошибки: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"CollectDataBlocks: Ошибка при доступе к подгруппам группы {group.Name}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"CollectDataBlocks: Ошибка при сборе блоков данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Улучшенный метод обработки членов блока данных
        /// </summary>
        private void ProcessDbMembersSafe(DataBlock db, PlcData plcData, bool isOptimized, bool isUDT, bool isSafety, ref int tagCount)
        {
            try
            {
                // Проверка аргументов
                if (db == null)
                {
                    _logger.Warn("ProcessDbMembersSafe: db не может быть null");
                    return;
                }

                // Безопасная обработка интерфейса блока данных
                if (db.Interface == null)
                {
                    _logger.Warn($"ProcessDbMembersSafe: Блок данных {db.Name} не имеет интерфейса");
                    return;
                }

                // Ограничиваем глубину обработки для снижения вероятности ошибок
                ExtractFlattenedDbTags(db, plcData, isOptimized, isUDT, isSafety, ref tagCount);
            }
            catch (Exception ex)
            {
                _logger.Error($"ProcessDbMembersSafe: Ошибка при обработке членов блока данных {db?.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Улучшенный метод извлечения тегов DB
        /// </summary>
        private void ExtractFlattenedDbTags(DataBlock db, PlcData plcData, bool isOptimized, bool isUDT, bool isSafety, ref int tagCount)
        {
            try
            {
                // Проверяем, что интерфейс и члены доступны
                var members = db.Interface.Members;
                if (members == null || !members.Any())
                {
                    _logger.Warn($"ExtractFlattenedDbTags: Интерфейс блока данных {db.Name} не содержит членов");
                    return;
                }

                // Перебираем каждый член
                foreach (var member in members)
                {
                    try
                    {
                        if (member == null) continue;

                        // Получаем имя члена
                        string name = member.Name;

                        // Определяем тип данных
                        string dataTypeString = "Unknown";
                        try
                        {
                            // Пробуем получить тип через свойство или аттрибут
                            var dataTypeObj = member.GetAttribute("DataTypeName");
                            dataTypeString = dataTypeObj?.ToString() ?? "Unknown";
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"ExtractFlattenedDbTags: Ошибка при получении типа данных для {name}: {ex.Message}");
                        }

                        // Формируем полное имя тега с префиксом блока данных
                        string fullName = $"{db.Name}.{name}";

                        // Конвертируем строковый тип данных в TagDataType
                        TagDataType dataType = ConvertToTagDataType(dataTypeString);

                        // Проверяем, поддерживается ли тип данных
                        if (!IsSupportedTagType(dataType))
                        {
                            _logger.Debug($"ExtractFlattenedDbTags: Пропущен тег DB {fullName} с неподдерживаемым типом данных {dataTypeString}");
                            continue;
                        }

                        // Получаем комментарий
                        string comment = "";
                        try
                        {
                            var commentObj = member.GetAttribute("Comment");
                            comment = commentObj?.ToString() ?? "";
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"ExtractFlattenedDbTags: Ошибка при получении комментария для {name}: {ex.Message}");
                        }

                        // Создаем и добавляем тег в плакдату
                        plcData.DbTags.Add(new TagDefinition
                        {
                            Name = fullName,
                            Address = isOptimized ? "Optimized" : "Standard",
                            DataType = dataType,
                            Comment = comment,
                            GroupName = db.Name,
                            IsOptimized = isOptimized,
                            IsUDT = isUDT,
                            IsSafety = isSafety
                        });

                        tagCount++;
                        _logger.Debug($"ExtractFlattenedDbTags: Добавлен тег DB: {fullName} ({dataTypeString})");

                        // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                        GC.KeepAlive(member);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"ExtractFlattenedDbTags: Ошибка при обработке члена: {ex.Message}");
                        // Продолжаем с другими членами
                    }
                }

                // Важно: удерживаем COM-объект в памяти до завершения работы с ним
                GC.KeepAlive(db);
                GC.KeepAlive(db.Interface);
                GC.KeepAlive(members);
            }
            catch (Exception ex)
            {
                _logger.Error($"ExtractFlattenedDbTags: Ошибка при извлечении тегов DB: {ex.Message}");
            }
        }

        /// <summary>
        /// Чтение тегов ПЛК из таблиц тегов
        /// </summary>
        private void ReadPlcTags(PlcSoftware plcSoftware, PlcData plcData, ref int tableCount, ref int tagCount)
        {
            try
            {
                _logger.Info("Чтение тегов ПЛК из таблиц тегов...");

                // Проверка параметров
                if (plcSoftware == null)
                {
                    _logger.Error("ReadPlcTags: plcSoftware не может быть null");
                    return;
                }

                if (plcSoftware.TagTableGroup == null)
                {
                    _logger.Error("ReadPlcTags: TagTableGroup не найдена в plcSoftware");
                    return;
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
                        TagDataType dataType = ConvertToTagDataType(dataTypeString);

                        // Проверяем, поддерживается ли тип данных
                        if (!IsSupportedTagType(dataType))
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

        /// <summary>
        /// Проверка, поддерживается ли тип тега
        /// </summary>
        private bool IsSupportedTagType(TagDataType dataType)
        {
            // По требованиям только bool, int, dint, real
            return dataType == TagDataType.Bool ||
                   dataType == TagDataType.Int ||
                   dataType == TagDataType.DInt ||
                   dataType == TagDataType.Real;
        }

        /// <summary>
        /// Конвертация строкового типа данных в TagDataType
        /// </summary>
        private TagDataType ConvertToTagDataType(string dataTypeString)
        {
            if (string.IsNullOrEmpty(dataTypeString))
                return TagDataType.Other;

            string lowerType = dataTypeString.ToLower();

            if (lowerType.Contains("bool"))
                return TagDataType.Bool;
            else if (lowerType.Contains("int") && !lowerType.Contains("dint"))
                return TagDataType.Int;
            else if (lowerType.Contains("dint"))
                return TagDataType.DInt;
            else if (lowerType.Contains("real"))
                return TagDataType.Real;
            else if (lowerType.Contains("string"))
                return TagDataType.String;
            else if (lowerType.StartsWith("udt_") || lowerType.Contains("type"))
                return TagDataType.UDT;
            else
                return TagDataType.Other;
        }
    }
}
