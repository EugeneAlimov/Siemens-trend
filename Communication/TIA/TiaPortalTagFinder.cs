using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Blocks.Interface;
using Siemens.Engineering.SW.Tags;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Класс для поиска тегов в проекте TIA Portal
    /// </summary>
    public class TiaPortalTagFinder
    {
        private readonly Logger _logger;
        private readonly TiaPortal _tiaPortal;

        // Кэш для блоков данных и таблиц тегов
        private Dictionary<string, PlcBlock> _dataBlockCache = new Dictionary<string, PlcBlock>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, PlcTagTable> _tagTableCache = new Dictionary<string, PlcTagTable>(StringComparer.OrdinalIgnoreCase);

        private readonly GlobalDbHandler _globalDbHandler;

        /// <summary>
        /// Конструктор
        /// </summary>
        public TiaPortalTagFinder(Logger logger, TiaPortal tiaPortal)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tiaPortal = tiaPortal ?? throw new ArgumentNullException(nameof(tiaPortal));
            _globalDbHandler = new GlobalDbHandler(logger);
        }

        /// <summary>
        /// Метод для поиска тегов по их полным именам
        /// </summary>
        public List<TagDefinition> FindTags(List<string> tagNames)
        {
            List<TagDefinition> results = new List<TagDefinition>();

            try
            {
                _logger.Info($"Начало поиска {tagNames.Count} тегов");

                // Проверка подключения к TIA Portal
                if (_tiaPortal == null || _tiaPortal.Projects.Count == 0)
                {
                    _logger.Error("Нет активного подключения к TIA Portal или нет открытых проектов");
                    return results;
                }

                // Проверка, есть ли теги для поиска
                if (tagNames == null || tagNames.Count == 0)
                {
                    _logger.Warn("Пустой список тегов для поиска");
                    return results;
                }

                // Очищаем кэш перед новым поиском
                _dataBlockCache.Clear();
                _tagTableCache.Clear();

                // Группировка тегов по контейнерам
                var groupedTags = GroupTagsByContainer(tagNames);

                // Вывод в лог для отладки
                foreach (var group in groupedTags)
                {
                    _logger.Info($"Группа тегов для контейнера {group.Key}. " +
                                 $"Количество тегов: {group.Value.Count}");
                }

                // Выполняем поиск для каждой группы
                foreach (var group in groupedTags)
                {
                    string containerName = group.Key;
                    List<string> tagPaths = group.Value;

                    if (string.IsNullOrEmpty(containerName))
                    {
                        _logger.Warn("Пропуск группы с пустым именем контейнера");
                        continue;
                    }

                    _logger.Info($"Поиск тегов в контейнере {containerName}");

                    // Определяем тип контейнера (DB или таблица тегов)
                    bool isDbContainer = TiaTagTypeUtility.IsDbContainer(containerName);

                    _logger.Info($"Контейнер {containerName} определен как {(isDbContainer ? "блок данных" : "таблица тегов")}");

                    if (isDbContainer)
                    {
                        // Ищем теги в блоке данных
                        var foundTags = FindTagsInDbContainer(containerName, tagPaths);
                        if (foundTags.Count > 0)
                        {
                            _logger.Info($"Найдено {foundTags.Count} тегов в блоке данных {containerName}");
                            results.AddRange(foundTags);
                        }
                        else
                        {
                            _logger.Warn($"Не найдено тегов в блоке данных {containerName}");
                        }
                    }
                    else
                    {
                        // Ищем теги в таблице тегов ПЛК
                        var foundTags = FindTagsInPlcContainer(containerName, tagPaths);
                        if (foundTags.Count > 0)
                        {
                            _logger.Info($"Найдено {foundTags.Count} тегов в таблице тегов {containerName}");
                            results.AddRange(foundTags);
                        }
                        else
                        {
                            _logger.Warn($"Не найдено тегов в таблице тегов {containerName}");
                        }
                    }
                }

                _logger.Info($"Найдено {results.Count} тегов из {tagNames.Count} запрошенных");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// Группировка тегов по контейнерам
        /// </summary>
        private Dictionary<string, List<string>> GroupTagsByContainer(List<string> tagNames)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string tagName in tagNames)
            {
                // Используем единую функцию из TiaTagTypeUtility для разбора имени тега
                var parsedTag = TiaTagTypeUtility.ParseTagName(tagName);
                string container = parsedTag.Container;
                string tagPath = parsedTag.TagName;
                bool isDb = parsedTag.IsDB;

                // Проверяем контейнер
                if (string.IsNullOrEmpty(container))
                {
                    _logger.Warn($"Не удалось определить контейнер для тега {tagName}, используем Default");
                    container = "Default";
                }

                // Добавляем в словарь
                if (!result.ContainsKey(container))
                {
                    result[container] = new List<string>();
                }

                result[container].Add(tagPath);
                _logger.Info($"Тег {(isDb ? "DB" : "PLC")}: {container}.{tagPath}");
            }

            return result;
        }

        /// <summary>
        /// Поиск тегов в блоке данных
        /// </summary>
        private List<TagDefinition> FindTagsInDbContainer(string dbName, List<string> tagPaths)
        {
            List<TagDefinition> results = new List<TagDefinition>();

            try
            {
                // Удаляем кавычки из имени блока, если они есть
                string cleanDbName = dbName.Trim('"');
                _logger.Info($"Поиск тегов в контейнере {cleanDbName} (исходное имя: {dbName})");

                // Находим блок данных в проекте TIA Portal
                PlcBlock db;

                // Проверяем кэш
                if (_dataBlockCache.TryGetValue(cleanDbName, out db))
                {
                    _logger.Info($"Блок данных {cleanDbName} найден в кэше");
                }
                else
                {
                    db = FindDataBlock(cleanDbName);
                    if (db != null)
                    {
                        // Добавляем в кэш
                        _dataBlockCache[cleanDbName] = db;
                    }
                }

                if (db == null)
                {
                    _logger.Warn($"Блок данных {cleanDbName} не найден");
                    return results;
                }

                _logger.Info($"Найден блок данных {cleanDbName}");

                // Проверяем, является ли блок данных GlobalDB
                if (_globalDbHandler.IsGlobalDb(db))
                {
                    _logger.Info($"Обнаружен блок данных типа GlobalDB, использую специализированный обработчик");
                    return _globalDbHandler.FindTagsInGlobalDb(db, cleanDbName, tagPaths);
                }

                // Это обычный PlcBlock, используем стандартный подход
                _logger.Info($"Обрабатываю стандартный PlcBlock");

                // Проверяем, оптимизирован ли блок данных
                bool isOptimized = IsDataBlockOptimized(db);
                _logger.Info($"Блок данных {cleanDbName} оптимизирован: {isOptimized}");

                // Получаем интерфейс блока данных
                var dbInterface = db.GetAttribute("Interface") as PlcBlockInterface;
                if (dbInterface == null)
                {
                    _logger.Error($"Не удалось получить интерфейс блока данных {cleanDbName}");
                    return results;
                }

                // Собираем карту всех тегов в DB для быстрого поиска
                var tagMap = BuildDataBlockTagMap(dbInterface, cleanDbName);

                // Для каждого пути к тегу
                foreach (string tagPath in tagPaths)
                {
                    try
                    {
                        _logger.Info($"Поиск тега {tagPath} в блоке данных {cleanDbName}");

                        // Ищем тег в карте
                        if (tagMap.TryGetValue(tagPath, out var tagInfo))
                        {
                            // Создаем объект TagDefinition
                            var tagDefinition = new TagDefinition
                            {
                                Id = Guid.NewGuid(),
                                Name = tagPath,
                                GroupName = cleanDbName,
                                IsDbTag = true,
                                IsOptimized = isOptimized,
                                DataType = tagInfo.DataType,
                                Comment = tagInfo.Comment
                            };

                            // Добавляем тег в результаты
                            results.Add(tagDefinition);

                            _logger.Info($"Тег добавлен в результаты: {cleanDbName}.{tagPath} с типом данных {tagDefinition.DataType}");
                        }
                        else
                        {
                            _logger.Warn($"Тег {tagPath} не найден в блоке данных {cleanDbName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при поиске тега {tagPath} в блоке данных {cleanDbName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов в блоке данных {dbName}: {ex.Message}");
            }

            return results;
        }
        /// <summary>
        /// Строит карту всех тегов в блоке данных
        /// </summary>
        private Dictionary<string, (TagDataType DataType, string Comment)> BuildDataBlockTagMap(
            PlcBlockInterface dbInterface, string dbName)
        {
            var result = new Dictionary<string, (TagDataType, string)>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Собираем все теги рекурсивно
                BuildDataBlockTagMapRecursive(dbInterface.Members, "", result, dbName);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при построении карты тегов для блока данных {dbName}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Рекурсивно строит карту тегов в блоке данных
        /// </summary>
        private void BuildDataBlockTagMapRecursive(
            MemberComposition members,
            string parentPath,
            Dictionary<string, (TagDataType, string)> tagMap,
            string dbName)
        {
            try
            {
                foreach (var member in members)
                {
                    string currentPath = string.IsNullOrEmpty(parentPath)
                        ? member.Name
                        : $"{parentPath}.{member.Name}";

                    // Получаем тип данных
                    string dataTypeName = GetMemberDataTypeName(member);
                    var dataType = TiaTagTypeUtility.ConvertToTagDataType(dataTypeName);
                    string comment = member.GetAttribute("Comment")?.ToString() ?? "";

                    // Добавляем в карту
                    tagMap[currentPath] = (dataType, comment);

                    _logger.Debug($"Добавлен тег в карту: {dbName}.{currentPath}, тип: {dataType}");

                    // Рекурсивно обрабатываем вложенные элементы, если они есть
                    try
                    {
                        // Безопасно получаем дочерние элементы
                        var childMembers = GetChildMembers(member);
                        if (childMembers != null && childMembers.Any())
                        {
                            // Создаем временную коллекцию для дочерних элементов
                            MemberComposition tempMembers = null;

                            // В зависимости от версии API, нам может потребоваться другой подход
                            try
                            {
                                // Пытаемся получить доступ к дочерним элементам через существующий API
                                var memberObj = member.GetType().GetProperty("Members")?.GetValue(member);
                                if (memberObj is MemberComposition memberComp)
                                {
                                    tempMembers = memberComp;
                                }
                            }
                            catch
                            {
                                // Если не получилось, просто используем списочную обработку без композиции
                                foreach (var childMember in childMembers)
                                {
                                    string childPath = $"{currentPath}.{childMember.Name}";
                                    string childTypeName = GetMemberDataTypeName(childMember);
                                    var childType = TiaTagTypeUtility.ConvertToTagDataType(childTypeName);
                                    string childComment = childMember.GetAttribute("Comment")?.ToString() ?? "";

                                    tagMap[childPath] = (childType, childComment);
                                }
                            }

                            // Если удалось получить MemberComposition, используем рекурсивный вызов
                            if (tempMembers != null)
                            {
                                BuildDataBlockTagMapRecursive(tempMembers, currentPath, tagMap, dbName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Не удалось получить дочерние элементы для {currentPath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка в BuildDataBlockTagMapRecursive: {ex.Message}");
            }
        }

        /// <summary>
        /// Поиск тегов в таблице PLC тегов
        /// </summary>
        private List<TagDefinition> FindTagsInPlcContainer(string tableName, List<string> tagNames)
        {
            List<TagDefinition> results = new List<TagDefinition>();

            try
            {
                // Удаляем кавычки из имени таблицы, если они есть
                string cleanTableName = tableName.Trim('"');
                _logger.Info($"Поиск тегов в таблице {cleanTableName} (исходное имя: {tableName})");

                // Находим таблицу тегов в проекте TIA Portal
                PlcTagTable tagTable;

                // Проверяем кэш
                if (_tagTableCache.TryGetValue(cleanTableName, out tagTable))
                {
                    _logger.Info($"Таблица тегов {cleanTableName} найдена в кэше");
                }
                else
                {
                    tagTable = FindPlcTagTable(cleanTableName);
                    if (tagTable != null)
                    {
                        // Добавляем в кэш
                        _tagTableCache[cleanTableName] = tagTable;
                    }
                }

                if (tagTable == null)
                {
                    _logger.Warn($"Таблица тегов {cleanTableName} не найдена");
                    return results;
                }

                _logger.Info($"Найдена таблица тегов {cleanTableName}");

                // Строим карту тегов для быстрого поиска
                var tagMap = BuildPlcTagMap(tagTable);

                // Для каждого имени тега
                foreach (string tagName in tagNames)
                {
                    try
                    {
                        _logger.Info($"Поиск тега {tagName} в таблице тегов {cleanTableName}");

                        // Ищем тег в карте
                        if (tagMap.TryGetValue(tagName, out var tagInfo))
                        {
                            // Создаем объект TagDefinition
                            var tagDefinition = new TagDefinition
                            {
                                Id = Guid.NewGuid(),
                                Name = tagName,
                                GroupName = cleanTableName,
                                IsDbTag = false,
                                DataType = tagInfo.DataType,
                                Comment = tagInfo.Comment,
                                Address = tagInfo.Address
                            };

                            // Добавляем тег в результаты
                            results.Add(tagDefinition);

                            _logger.Info($"Тег добавлен в результаты: {cleanTableName}.{tagName} с типом данных {tagDefinition.DataType}");
                        }
                        else
                        {
                            _logger.Warn($"Тег {tagName} не найден в таблице тегов {cleanTableName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при поиске тега {tagName} в таблице тегов {cleanTableName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов в таблице тегов {tableName}: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Строит карту тегов PLC для быстрого поиска
        /// </summary>
        private Dictionary<string, (TagDataType DataType, string Comment, string Address)> BuildPlcTagMap(PlcTagTable tagTable)
        {
            var result = new Dictionary<string, (TagDataType, string, string)>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var tag in tagTable.Tags)
                {
                    string dataTypeName = tag.DataTypeName;
                    var dataType = TiaTagTypeUtility.ConvertToTagDataType(dataTypeName);
                    string comment = tag.Comment?.ToString() ?? "";
                    string address = tag.LogicalAddress;

                    result[tag.Name] = (dataType, comment, address);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при построении карты тегов PLC: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Безопасно получает дочерние элементы Member
        /// </summary>
        private IEnumerable<Member> GetChildMembers(Member member)
        {
            try
            {
                // Пытаемся получить дочерние элементы через правильный API метод
                var membersProperty = member.GetType().GetProperty("Members");
                if (membersProperty != null)
                {
                    var memberObj = membersProperty.GetValue(member);
                    if (memberObj is MemberComposition memberComposition)
                    {
                        return memberComposition.ToArray();
                    }
                }

                // Если предыдущий подход не сработал, попробуем другой
                // В некоторых версиях API, дочерние элементы могут быть в другом свойстве
                var childMembersProperty = member.GetType().GetProperty("ChildMembers");
                if (childMembersProperty != null)
                {
                    var childMembers = childMembersProperty.GetValue(member);
                    if (childMembers is IEnumerable<Member> members)
                    {
                        return members;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении дочерних элементов для {member.Name}: {ex.Message}");
            }

            return Enumerable.Empty<Member>();
        }

        /// <summary>
        /// Получение имени типа данных для члена
        /// </summary>
        private string GetMemberDataTypeName(Member member)
        {
            try
            {
                // Попытка получить атрибут "DataType" из объекта Member
                var dataTypeAttr = member.GetAttribute("DataType");
                if (dataTypeAttr != null)
                {
                    return dataTypeAttr.ToString();
                }

                // Альтернативный способ: проверка на наличие свойства DataTypeName
                var dataTypeName = member.GetType().GetProperty("DataTypeName")?.GetValue(member)?.ToString();
                if (!string.IsNullOrEmpty(dataTypeName))
                {
                    return dataTypeName;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении типа данных для члена {member.Name}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Метод для нахождения таблицы PLC тегов по имени
        /// </summary>
        private PlcTagTable FindPlcTagTable(string tableName)
        {
            try
            {
                // Удаляем кавычки из имени таблицы, если они есть
                string cleanTableName = tableName.Trim('"');
                _logger.Info($"Поиск таблицы тегов {cleanTableName} в проекте");

                // Ищем во всех PLC устройствах проекта
                foreach (var device in _tiaPortal.Projects.First().Devices)
                {
                    // Проверяем только устройства PLC
                    foreach (var deviceItem in device.DeviceItems)
                    {
                        try
                        {
                            var softwareContainer = deviceItem.GetService<SoftwareContainer>();
                            if (softwareContainer != null)
                            {
                                var plcSoftware = softwareContainer.Software as PlcSoftware;
                                if (plcSoftware != null)
                                {
                                    _logger.Info($"Проверка таблиц тегов в устройстве {device.Name}, элемент {deviceItem.Name}");

                                    // Ищем таблицу тегов по имени с различными критериями
                                    foreach (var tagTable in plcSoftware.TagTableGroup.TagTables)
                                    {
                                        // Критерий 1: Точное совпадение
                                        if (string.Equals(tagTable.Name, cleanTableName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            _logger.Info($"Найдена таблица тегов {tagTable.Name} по точному совпадению");
                                            return tagTable;
                                        }

                                        // Критерий 2: Частичное совпадение
                                        if (tagTable.Name.IndexOf(cleanTableName, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            _logger.Info($"Найдена таблица тегов {tagTable.Name}, содержащая '{cleanTableName}' в имени");
                                            return tagTable;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Ошибка при проверке элемента устройства {deviceItem.Name}: {ex.Message}");
                        }
                    }
                }

                _logger.Warn($"Таблица тегов {cleanTableName} не найдена в проекте");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске таблицы тегов {tableName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Определение, оптимизирован ли блок данных
        /// </summary>
        private bool IsDataBlockOptimized(PlcBlock db)
        {
            try
            {
                _logger.Info($"Проверка оптимизации блока данных {db.Name}");

                // Получаем свойство "Optimized block access"
                var optimizedAccess = db.GetAttribute("Optimized block access");
                if (optimizedAccess is bool isOptimized)
                {
                    _logger.Info($"Блок данных {db.Name} оптимизирован: {isOptimized}");
                    return isOptimized;
                }

                // Если стандартный способ не сработал, попробуем альтернативные
                var properties = db.GetAttributeInfos();
                foreach (var prop in properties)
                {
                    if (prop.Name.Contains("Optimized") || prop.Name.Contains("Оптимизированный"))
                    {
                        var value = db.GetAttribute(prop.Name);
                        if (value is bool boolValue)
                        {
                            _logger.Info($"Блок данных {db.Name} оптимизирован (альтернативный способ): {boolValue}");
                            return boolValue;
                        }
                    }
                }

                _logger.Warn($"Не удалось определить оптимизацию блока данных {db.Name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при проверке оптимизации блока данных {db.Name}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Метод для нахождения блока данных по имени с расширенным поиском 
        /// (с поддержкой GlobalDB и других типов блоков данных)
        /// </summary>
        private PlcBlock FindDataBlock(string dbName)
        {
            try
            {
                // Удаляем кавычки из имени блока, если они есть
                string cleanDbName = dbName.Trim('"');
                _logger.Info($"Расширенный поиск блока данных {cleanDbName} в проекте (исходное имя: {dbName})");

                // Получаем список всех устройств в проекте
                var devices = _tiaPortal.Projects.First().Devices;
                _logger.Info($"Найдено устройств в проекте: {devices.Count}");

                // Перебираем все устройства
                foreach (var device in devices)
                {
                    _logger.Info($"Устройство: {device.Name}, Тип: {device.TypeIdentifier}");

                    // Получаем все DeviceItems
                    var allDeviceItems = GetAllDeviceItems(device);
                    _logger.Info($"Найдено элементов устройства: {allDeviceItems.Count}");

                    // Перебираем все DeviceItems
                    foreach (var deviceItem in allDeviceItems)
                    {
                        try
                        {
                            // Получаем SoftwareContainer
                            var softwareContainer = deviceItem.GetService<SoftwareContainer>();
                            if (softwareContainer != null)
                            {
                                // Получаем PlcSoftware
                                var plcSoftware = softwareContainer.Software as PlcSoftware;
                                if (plcSoftware != null)
                                {
                                    // Получаем все блоки в текущей группе и подгруппах
                                    List<PlcBlock> allBlocks = new List<PlcBlock>();
                                    CollectAllPlcBlocks(plcSoftware.BlockGroup, allBlocks);
                                    _logger.Info($"Всего найдено блоков: {allBlocks.Count}");

                                    // Поиск блока данных с использованием различных критериев
                                    PlcBlock foundBlock = FindDataBlockByVariousCriteria(allBlocks, cleanDbName);
                                    if (foundBlock != null)
                                    {
                                        return foundBlock;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Ошибка при проверке элемента устройства {deviceItem.Name}: {ex.Message}");
                        }
                    }
                }

                _logger.Warn($"Блок данных {cleanDbName} не найден в проекте после расширенного поиска");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске блока данных {dbName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Поиск блока данных по различным критериям
        /// </summary>
        private PlcBlock FindDataBlockByVariousCriteria(IEnumerable<PlcBlock> blocks, string dbName)
        {
            // Нормализуем имя для сравнения (удаляем лишние пробелы и делаем нижний регистр)
            string normalizedName = dbName.ToLower().Trim();

            foreach (var plcBlock in blocks)
            {
                // Проверяем, является ли блок блоком данных (DB)
                bool isDataBlock = plcBlock.ProgrammingLanguage == Siemens.Engineering.SW.Blocks.ProgrammingLanguage.DB;

                if (isDataBlock)
                {
                    string blockName = plcBlock.Name;
                    string normalizedBlockName = blockName.ToLower().Trim();

                    // Критерий 1: Точное совпадение
                    if (normalizedBlockName == normalizedName)
                    {
                        _logger.Info($"Найден блок данных {blockName} по точному совпадению");
                        return plcBlock;
                    }

                    // Критерий 2: Имя блока начинается с искомого имени
                    if (normalizedBlockName.StartsWith(normalizedName))
                    {
                        _logger.Info($"Найден блок данных {blockName}, начинающийся с '{dbName}'");
                        return plcBlock;
                    }

                    // Критерий 3: Имя блока содержит искомое имя с некоторым суффиксом формата DB
                    if (Regex.IsMatch(normalizedBlockName, $"^{Regex.Escape(normalizedName)}\\s*\\[?db\\d*\\]?", RegexOptions.IgnoreCase))
                    {
                        _logger.Info($"Найден блок данных {blockName} по регулярному выражению");
                        return plcBlock;
                    }

                    // Критерий 4: Проверка на наличие числового идентификатора в имени блока
                    var match = Regex.Match(normalizedBlockName, @"^(.*?)(\d+)(.*)$");
                    if (match.Success)
                    {
                        string baseName = match.Groups[1].Value.Trim();
                        if (baseName == normalizedName || normalizedName.StartsWith(baseName))
                        {
                            _logger.Info($"Найден блок данных {blockName} по базовому имени с числовым идентификатором");
                            return plcBlock;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Получает рекурсивно все элементы устройства, включая вложенные
        /// </summary>
        private List<DeviceItem> GetAllDeviceItems(Device device)
        {
            List<DeviceItem> result = new List<DeviceItem>();

            try
            {
                // Получаем элементы устройства на верхнем уровне
                foreach (var item in device.DeviceItems)
                {
                    result.Add(item);

                    // Добавляем вложенные элементы рекурсивно
                    result.AddRange(GetNestedDeviceItems(item));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении элементов устройства {device.Name}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Получает рекурсивно все вложенные элементы устройства
        /// </summary>
        private List<DeviceItem> GetNestedDeviceItems(DeviceItem deviceItem)
        {
            List<DeviceItem> result = new List<DeviceItem>();

            try
            {
                // Получаем вложенные элементы устройства
                foreach (var item in deviceItem.DeviceItems)
                {
                    result.Add(item);

                    // Добавляем вложенные элементы рекурсивно
                    result.AddRange(GetNestedDeviceItems(item));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении вложенных элементов устройства {deviceItem.Name}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Получает рекурсивно все блоки, включая вложенные группы
        /// </summary>
        private void CollectAllPlcBlocks(PlcBlockGroup blockGroup, List<PlcBlock> result)
        {
            try
            {
                // Добавляем блоки из текущей группы
                foreach (var block in blockGroup.Blocks)
                {
                    if (block is PlcBlock plcBlock)
                    {
                        result.Add(plcBlock);
                    }
                }

                // Добавляем блоки из вложенных групп
                foreach (var group in blockGroup.Groups)
                {
                    CollectAllPlcBlocks(group, result);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении блоков из группы: {ex.Message}");
            }
        }
    }
}