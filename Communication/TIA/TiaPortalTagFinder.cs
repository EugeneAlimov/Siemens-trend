using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
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

        /// <summary>
        /// Конструктор
        /// </summary>
        public TiaPortalTagFinder(Logger logger, TiaPortal tiaPortal)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tiaPortal = tiaPortal ?? throw new ArgumentNullException(nameof(tiaPortal));
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

                    _logger.Info($"Поиск тегов в контейнере {containerName}");

                    // Определяем тип контейнера (DB или таблица тегов)
                    // Проверяем, является ли контейнер блоком данных
                    var parsedTag = ParseTagName(tagNames[0]); // Используем первый тег для определения типа
                    bool isDbContainer = parsedTag.IsDB;

                    _logger.Info($"Контейнер {containerName} является {(isDbContainer ? "блоком данных" : "таблицей тегов")}");

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
            var result = new Dictionary<string, List<string>>();

            foreach (string tagName in tagNames)
            {
                var parsedTag = ParseTagName(tagName);

                if (!string.IsNullOrEmpty(parsedTag.Container))
                {
                    // Используем оригинальное имя контейнера с кавычками, если они есть
                    if (!result.ContainsKey(parsedTag.Container))
                    {
                        result[parsedTag.Container] = new List<string>();
                    }

                    if (parsedTag.IsDB)
                    {
                        // Для DB добавляем только часть имени после контейнера
                        result[parsedTag.Container].Add(parsedTag.TagName);
                        _logger.Info($"Тег DB: {parsedTag.Container}.{parsedTag.TagName}");
                    }
                    else
                    {
                        // Для PLC тегов добавляем полное имя
                        result[parsedTag.Container].Add(parsedTag.TagName);
                        _logger.Info($"Тег PLC: {parsedTag.Container} → {parsedTag.TagName}");
                    }
                }
                else
                {
                    // Если контейнер не определен, добавляем в дефолтный контейнер
                    _logger.Warn($"Не удалось определить контейнер для тега {tagName}");

                    if (!result.ContainsKey("Default"))
                    {
                        result["Default"] = new List<string>();
                    }

                    result["Default"].Add(tagName);
                }
            }

            return result;
        }
        /// <summary>
        /// Определение типа контейнера
        /// </summary>
        private bool IsDbContainer(string containerName)
        {
            // Проверяем варианты форматов имени DB
            if (string.IsNullOrEmpty(containerName))
                return false;

            // Точные совпадения с префиксом DB
            if (containerName.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
                return true;

            // Совпадения с _DB в имени
            if (containerName.Contains("_DB", StringComparison.OrdinalIgnoreCase))
                return true;

            // Регулярное выражение для проверки DB+цифры
            if (Regex.IsMatch(containerName, @"DB\d+"))
                return true;

            // Специфичные имена контейнеров, которые являются DB
            if (containerName == "S1" || containerName == "Exchanger_DB")
                return true;

            // По умолчанию считаем, что это не DB
            return false;
        }

        /// <summary>
        /// Структура для разобранного имени тега
        /// </summary>
        private struct ParsedTag
        {
            public string Container;
            public string TagName;
            public bool IsDB;
        }

        /// <summary>
        /// Разбор полного имени тега на части
        /// </summary>
        private ParsedTag ParseTagName(string fullTagName)
        {
            // Проверка на null или пустую строку
            if (string.IsNullOrEmpty(fullTagName))
            {
                _logger.Warn("Пустое имя тега");
                return new ParsedTag { Container = string.Empty, TagName = string.Empty, IsDB = false };
            }

            _logger.Info($"Разбор имени тега: {fullTagName}");

            // Результат по умолчанию
            ParsedTag result = new ParsedTag
            {
                Container = string.Empty,
                TagName = fullTagName,
                IsDB = false
            };

            try
            {
                // Формат с кавычками "ContainerName".TagPath или "TagName"
                if (fullTagName.Contains("\""))
                {
                    int startQuoteIndex = fullTagName.IndexOf("\"");
                    int endQuoteIndex = fullTagName.IndexOf("\"", startQuoteIndex + 1);

                    if (startQuoteIndex >= 0 && endQuoteIndex > startQuoteIndex)
                    {
                        // Извлекаем имя контейнера (с кавычками)
                        result.Container = fullTagName.Substring(startQuoteIndex, endQuoteIndex - startQuoteIndex + 1);

                        // Также сохраняем имя контейнера без кавычек для удобства
                        string containerNameWithoutQuotes = fullTagName.Substring(startQuoteIndex + 1, endQuoteIndex - startQuoteIndex - 1);

                        // Проверяем, есть ли что-то после закрывающей кавычки
                        if (fullTagName.Length > endQuoteIndex + 1)
                        {
                            // Ищем точку после закрывающей кавычки
                            if (fullTagName[endQuoteIndex + 1] == '.')
                            {
                                // Это формат DB - есть путь после контейнера
                                result.IsDB = true;
                                result.TagName = fullTagName.Substring(endQuoteIndex + 2); // Пропускаем кавычку и точку
                                _logger.Info($"Распознан тег DB: контейнер={containerNameWithoutQuotes}, тег={result.TagName}");
                            }
                            else
                            {
                                // Необычный формат, логируем для отладки
                                _logger.Warn($"Необычный формат имени тега: {fullTagName}");
                                // Оставляем как PLC тег с полным именем
                                result.TagName = fullTagName;
                            }
                        }
                        else
                        {
                            // Только имя контейнера в кавычках - это PLC тег
                            _logger.Info($"Распознан тег PLC: контейнер={containerNameWithoutQuotes}");
                            // Для PLC тегов в этом формате TagName остается равным имени контейнера
                            result.TagName = containerNameWithoutQuotes;
                            result.IsDB = false;
                        }
                    }
                }
                // Формат с точкой, но без кавычек: ContainerName.TagPath
                else if (fullTagName.Contains("."))
                {
                    var parts = fullTagName.Split(new[] { '.' }, 2);

                    if (parts.Length >= 2)
                    {
                        result.Container = parts[0];
                        result.TagName = parts[1];

                        // Проверяем, является ли контейнер блоком данных
                        result.IsDB = IsDbContainer(result.Container);

                        _logger.Info($"Распознан тег с точкой: контейнер={result.Container}, тег={result.TagName}, IsDB={result.IsDB}");
                    }
                }
                // Формат PLC тега без точек и кавычек
                else
                {
                    // Предполагаем, что это PLC тег без явного указания контейнера
                    result.Container = "Default";
                    result.TagName = fullTagName;
                    result.IsDB = false;

                    _logger.Info($"Распознан простой тег: {fullTagName}");
                }

                // Дополнительная проверка после разбора
                if (string.IsNullOrEmpty(result.Container))
                {
                    _logger.Warn($"Не удалось определить контейнер для тега: {fullTagName}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при разборе имени тега {fullTagName}: {ex.Message}");
            }

            return result;
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
        /// Конвертация строкового типа данных в TagDataType
        /// </summary>
        private TagDataType GetTagDataType(string plcDataType, string tagName, string containerName)
        {
            // Используем только тип из API TIA Portal
            if (!string.IsNullOrEmpty(plcDataType))
            {
                string lowerType = plcDataType.ToLower();

                // Подробная логика определения типа
                // Логические типы
                if (lowerType.Contains("bool"))
                    return TagDataType.Bool;

                // Целочисленные типы - 16 бит
                else if (lowerType == "int" || lowerType == "int16" || lowerType == "uint16" ||
                         lowerType == "word" || lowerType.Contains("_int"))
                    return TagDataType.Int;

                // Целочисленные типы - 32 бит
                else if (lowerType == "dint" || lowerType == "int32" || lowerType == "uint32" ||
                         lowerType == "dword" || lowerType == "udint")
                    return TagDataType.DInt;

                // Типы с плавающей точкой
                else if (lowerType.Contains("real") || lowerType.Contains("float"))
                    return TagDataType.Real;

                // Строковые типы
                else if (lowerType.Contains("string") || lowerType.Contains("wstring") ||
                         lowerType.Contains("char"))
                    return TagDataType.String;

                // Пользовательские типы данных
                else if (lowerType.StartsWith("udt") || lowerType.StartsWith("\"udt") ||
                         lowerType.Contains("struct") || lowerType.Contains("tag_udt"))
                    return TagDataType.UDT;

                // По умолчанию
                else
                    return TagDataType.Other;
            }

            // Если тип не определен, используем Other и логируем предупреждение
            _logger.Warn($"Не удалось определить тип данных для тега {containerName}.{tagName}. " +
                         $"Тип из TIA Portal: '{plcDataType}'. Установлен тип Other.");
            return TagDataType.Other;
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
                var db = FindDataBlock(dbName);

                if (db == null)
                {
                    _logger.Warn($"Блок данных {cleanDbName} не найден");
                    return results;
                }

                _logger.Info($"Найден блок данных {cleanDbName}");

                // Проверяем, оптимизирован ли блок данных
                bool isOptimized = IsDataBlockOptimized(db);
                _logger.Info($"Блок данных {cleanDbName} оптимизирован: {isOptimized}");

                // Для каждого пути к тегу
                foreach (string tagPath in tagPaths)
                {
                    try
                    {
                        _logger.Info($"Поиск тега {tagPath} в блоке данных {cleanDbName}");

                        // Находим тег по пути
                        var tagMember = FindTagMemberInDataBlock(db, tagPath);

                        if (tagMember != null)
                        {
                            // Получаем тип данных
                            string dataTypeName = GetMemberDataTypeName(tagMember);
                            _logger.Info($"Найден тег {tagPath} в блоке данных {cleanDbName}, тип данных: {dataTypeName}");

                            // Создаем объект TagDefinition
                            var tagDefinition = new TagDefinition
                            {
                                Id = Guid.NewGuid(),
                                Name = tagPath,
                                GroupName = cleanDbName,
                                IsDbTag = true,
                                IsOptimized = isOptimized,
                                DataType = GetTagDataType(dataTypeName, tagPath, cleanDbName),
                                Comment = tagMember.GetAttribute("Comment")?.ToString()
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
        /// Поиск тегов в таблице PLC тегов
        /// </summary>
        private List<TagDefinition> FindTagsInPlcContainer(string tableName, List<string> tagNames)
        {
            List<TagDefinition> results = new List<TagDefinition>();

            try
            {
                // Находим таблицу тегов в проекте TIA Portal
                var tagTable = FindPlcTagTable(tableName);

                if (tagTable == null)
                {
                    _logger.Warn($"Таблица тегов {tableName} не найдена");
                    return results;
                }

                _logger.Info($"Найдена таблица тегов {tableName}");

                // Для каждого имени тега
                foreach (string tagName in tagNames)
                {
                    try
                    {
                        _logger.Info($"Поиск тега {tagName} в таблице тегов {tableName}");

                        // Находим тег по имени
                        var plcTag = FindPlcTagInTable(tagTable, tagName);

                        if (plcTag != null)
                        {
                            // Получаем тип данных
                            string dataTypeName = plcTag.DataTypeName;
                            _logger.Info($"Найден тег {tagName} в таблице {tableName}, тип данных: {dataTypeName}");

                            // Создаем объект TagDefinition
                            var tagDefinition = new TagDefinition
                            {
                                Id = Guid.NewGuid(),
                                Name = tagName,
                                GroupName = tableName,
                                IsDbTag = false,
                                DataType = GetTagDataType(dataTypeName, tagName, tableName),
                                Comment = plcTag.Comment?.ToString()
                            };

                            // Добавляем тег в результаты
                            results.Add(tagDefinition);

                            _logger.Info($"Тег добавлен в результаты: {tableName}.{tagName} с типом данных {tagDefinition.DataType}");
                        }
                        else
                        {
                            _logger.Warn($"Тег {tagName} не найден в таблице тегов {tableName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при поиске тега {tagName} в таблице тегов {tableName}: {ex.Message}");
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
        /// Метод для нахождения блока данных по имени
        /// </summary>
        //private PlcBlock FindDataBlock(string dbName)
        //{
        //    try
        //    {
        //        // Удаляем кавычки из имени блока, если они есть
        //        string cleanDbName = dbName.Trim('"');
        //        _logger.Info($"Поиск блока данных {cleanDbName} в проекте (исходное имя: {dbName})");

        //        // Ищем во всех PLC устройствах проекта
        //        foreach (var device in _tiaPortal.Projects.First().Devices)
        //        {
        //            // Проверяем только устройства PLC
        //            foreach (var deviceItem in device.DeviceItems)
        //            {
        //                var softwareContainer = deviceItem.GetService<SoftwareContainer>();
        //                if (softwareContainer != null)
        //                {
        //                    var plcSoftware = softwareContainer.Software as PlcSoftware;
        //                    if (plcSoftware != null)
        //                    {
        //                        _logger.Info($"Проверка блоков данных в устройстве {device.Name}, элемент {deviceItem.Name}");

        //                        // Логируем все доступные блоки для отладки
        //                        _logger.Info("Список доступных блоков в устройстве:");
        //                        foreach (var block in plcSoftware.BlockGroup.Blocks)
        //                        {
        //                            if (block is PlcBlock plcBlock)
        //                            {
        //                                _logger.Info($"  - Блок: {plcBlock.Name}, Тип: {plcBlock.ProgrammingLanguage}");
        //                            }
        //                        }

        //                        // Ищем блок данных по имени с несколькими вариантами сопоставления
        //                        foreach (var block in plcSoftware.BlockGroup.Blocks)
        //                        {
        //                            if (block is PlcBlock plcBlock && plcBlock.ProgrammingLanguage == Siemens.Engineering.SW.Blocks.ProgrammingLanguage.DB)
        //                            {
        //                                // Проверка по полному точному имени
        //                                if (string.Equals(plcBlock.Name, cleanDbName, StringComparison.OrdinalIgnoreCase) ||
        //                                    string.Equals(plcBlock.Name, dbName, StringComparison.OrdinalIgnoreCase))
        //                                {
        //                                    _logger.Info($"Найден блок данных {plcBlock.Name} по точному имени");
        //                                    return plcBlock;
        //                                }

        //                                // Проверка по имени, игнорируя номер DB
        //                                if (plcBlock.Name.StartsWith(cleanDbName + " [DB", StringComparison.OrdinalIgnoreCase))
        //                                {
        //                                    _logger.Info($"Найден блок данных {plcBlock.Name} по началу имени");
        //                                    return plcBlock;
        //                                }

        //                                // Проверка, входит ли имя как часть полного имени блока
        //                                if (plcBlock.Name.IndexOf(cleanDbName, StringComparison.OrdinalIgnoreCase) >= 0)
        //                                {
        //                                    _logger.Info($"Найден блок данных {plcBlock.Name}, содержащий '{cleanDbName}' в имени");
        //                                    return plcBlock;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        _logger.Warn($"Блок данных {cleanDbName} не найден в проекте");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"Ошибка при поиске блока данных {dbName}: {ex.Message}");
        //    }

        //    return null;
        //}
        /// <summary>
        /// Метод для нахождения таблицы PLC тегов по имени
        /// </summary>
        private PlcTagTable FindPlcTagTable(string tableName)
        {
            try
            {
                _logger.Info($"Поиск таблицы тегов {tableName} в проекте");

                // Ищем во всех PLC устройствах проекта
                foreach (var device in _tiaPortal.Projects.First().Devices)
                {
                    // Проверяем только устройства PLC
                    foreach (var deviceItem in device.DeviceItems)
                    {
                        var softwareContainer = deviceItem.GetService<SoftwareContainer>();
                        if (softwareContainer != null)
                        {
                            var plcSoftware = softwareContainer.Software as PlcSoftware;
                            if (plcSoftware != null)
                            {
                                _logger.Info($"Проверка таблиц тегов в устройстве {device.Name}, элемент {deviceItem.Name}");

                                // Ищем таблицу тегов по имени
                                foreach (var tagTable in plcSoftware.TagTableGroup.TagTables)
                                {
                                    if (string.Equals(tagTable.Name, tableName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        _logger.Info($"Найдена таблица тегов {tableName}");
                                        return tagTable;
                                    }
                                }
                            }
                        }
                    }
                }

                _logger.Warn($"Таблица тегов {tableName} не найдена в проекте");
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
                var properties = db.GetAttributeInfos();
                var optimizedAccessProperty = properties.FirstOrDefault(p => p.Name == "Optimized block access");

                if (optimizedAccessProperty != null)
                {
                    // Получаем значение атрибута
                    var attribute = db.GetAttribute("Optimized block access");
                    if (attribute is bool isOptimized)
                    {
                        _logger.Info($"Блок данных {db.Name} оптимизирован: {isOptimized}");
                        return isOptimized;
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
        /// Поиск члена в блоке данных по пути
        /// </summary>
        private Member FindTagMemberInDataBlock(PlcBlock db, string tagPath)
        {
            try
            {
                _logger.Info($"Поиск тега {tagPath} в блоке данных {db.Name}");

                // Получаем интерфейс блока данных
                var dbInterface = db.GetAttribute("Interface") as PlcBlockInterface;
                if (dbInterface == null)
                {
                    _logger.Error($"Не удалось получить интерфейс блока данных {db.Name}");
                    return null;
                }

                // Разбиваем путь на части
                string[] pathParts = tagPath.Split('.');

                // Начинаем с корневых элементов
                var currentMembers = dbInterface.Members;
                Member currentMember = null;

                // Проходим по каждой части пути
                foreach (string part in pathParts)
                {
                    bool foundPart = false;
                    // Ищем член с указанным именем
                    foreach (var member in currentMembers)
                    {
                        if (string.Equals(member.Name, part, StringComparison.OrdinalIgnoreCase))
                        {
                            currentMember = member;

                            // Пробуем получить дочерние элементы для следующего шага
                            try
                            {
                                var membersProperty = member.GetType().GetProperty("Members");
                                if (membersProperty != null)
                                {
                                    var memberObj = membersProperty.GetValue(member);
                                    if (memberObj is MemberComposition nextMembers)
                                    {
                                        currentMembers = nextMembers;
                                        foundPart = true;
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn($"Не удалось получить дочерние элементы для {part}: {ex.Message}");
                            }

                            // Если не получилось, это может быть конечный элемент
                            foundPart = true;
                            break;
                        }
                    }

                    // Если часть пути не найдена, возвращаем null
                    if (!foundPart)
                    {
                        _logger.Warn($"Часть пути '{part}' не найдена в блоке данных {db.Name}");
                        return null;
                    }
                }

                _logger.Info($"Найден тег {tagPath} в блоке данных {db.Name}");
                return currentMember;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тега {tagPath} в блоке данных: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Поиск тега в таблице PLC тегов по имени
        /// </summary>
        private PlcTag FindPlcTagInTable(PlcTagTable tagTable, string tagName)
        {
            try
            {
                _logger.Info($"Поиск тега {tagName} в таблице тегов {tagTable.Name}");

                // Ищем тег по имени
                foreach (var tag in tagTable.Tags)
                {
                    if (string.Equals(tag.Name, tagName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Info($"Найден тег {tagName} в таблице тегов {tagTable.Name}");
                        return tag;
                    }
                }

                _logger.Warn($"Тег {tagName} не найден в таблице тегов {tagTable.Name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тега {tagName} в таблице тегов: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Метод для нахождения блока данных по имени с расширенным поиском
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
                            _logger.Info($"Проверка элемента устройства: {deviceItem.Name}");

                            // Получаем SoftwareContainer
                            var softwareContainer = deviceItem.GetService<SoftwareContainer>();
                            if (softwareContainer != null)
                            {
                                _logger.Info("Найден SoftwareContainer");

                                // Получаем PlcSoftware
                                var plcSoftware = softwareContainer.Software as PlcSoftware;
                                if (plcSoftware != null)
                                {
                                    _logger.Info("Найден PlcSoftware");

                                    // Получаем все блоки рекурсивно, включая вложенные группы
                                    var allBlocks = GetAllBlocksRecursively(plcSoftware.BlockGroup);
                                    _logger.Info($"Всего найдено блоков: {allBlocks.Count}");

                                    // Логируем все найденные блоки
                                    _logger.Info("Список всех доступных блоков:");
                                    foreach (var block in allBlocks)
                                    {
                                        if (block is PlcBlock plcBlock)
                                        {
                                            string blockType = plcBlock.ProgrammingLanguage.ToString();
                                            _logger.Info($"  - Блок: {plcBlock.Name}, Тип: {blockType}");
                                        }
                                    }

                                    // Поиск блока данных по имени
                                    foreach (var block in allBlocks)
                                    {
                                        if (block is PlcBlock plcBlock)
                                        {
                                            // Проверяем, является ли блок блоком данных (DB)
                                            bool isDataBlock = plcBlock.ProgrammingLanguage == Siemens.Engineering.SW.Blocks.ProgrammingLanguage.DB;

                                            if (isDataBlock)
                                            {
                                                _logger.Info($"Проверка блока данных: {plcBlock.Name}");

                                                // Проверка по полному точному имени
                                                if (string.Equals(plcBlock.Name, cleanDbName, StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(plcBlock.Name, dbName, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    _logger.Info($"Найден блок данных {plcBlock.Name} по точному имени");
                                                    return plcBlock;
                                                }

                                                // Проверка по частичному имени (без номера DB)
                                                if (plcBlock.Name.StartsWith(cleanDbName + " [DB", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    _logger.Info($"Найден блок данных {plcBlock.Name}, начинающийся с '{cleanDbName} [DB'");
                                                    return plcBlock;
                                                }

                                                // Проверка по имени без пробела перед [DB]
                                                if (plcBlock.Name.StartsWith(cleanDbName + "[DB", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    _logger.Info($"Найден блок данных {plcBlock.Name}, начинающийся с '{cleanDbName}[DB'");
                                                    return plcBlock;
                                                }

                                                // Проверка по началу имени
                                                if (plcBlock.Name.StartsWith(cleanDbName, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    _logger.Info($"Найден блок данных {plcBlock.Name}, начинающийся с '{cleanDbName}'");
                                                    return plcBlock;
                                                }
                                            }
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

                _logger.Warn($"Блок данных {cleanDbName} не найден в проекте после расширенного поиска");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске блока данных {dbName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Получает рекурсивно все элементы устройства, включая вложенные
        /// </summary>
        private List<DeviceItem> GetAllDeviceItems(Device device)
        {
            List<DeviceItem> result = new List<DeviceItem>();

            // Получаем элементы устройства на верхнем уровне
            foreach (var item in device.DeviceItems)
            {
                result.Add(item);

                // Добавляем вложенные элементы рекурсивно
                result.AddRange(GetNestedDeviceItems(item));
            }

            return result;
        }

        /// <summary>
        /// Получает рекурсивно все вложенные элементы устройства
        /// </summary>
        private List<DeviceItem> GetNestedDeviceItems(DeviceItem deviceItem)
        {
            List<DeviceItem> result = new List<DeviceItem>();

            // Получаем вложенные элементы устройства
            foreach (var item in deviceItem.DeviceItems)
            {
                result.Add(item);

                // Добавляем вложенные элементы рекурсивно
                result.AddRange(GetNestedDeviceItems(item));
            }

            return result;
        }

        /// <summary>
        /// Получает рекурсивно все блоки, включая вложенные группы
        /// </summary>
        private List<IBlock> GetAllBlocksRecursively(IBlockGroup blockGroup)
        {
            List<IBlock> result = new List<IBlock>();

            // Добавляем блоки из текущей группы
            foreach (var block in blockGroup.Blocks)
            {
                result.Add(block);
            }

            // Добавляем блоки из вложенных групп
            foreach (var group in blockGroup.Groups)
            {
                result.AddRange(GetAllBlocksRecursively(group));
            }

            return result;
        }
    }
}