using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
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

                // Группировка тегов по контейнерам
                var groupedTags = GroupTagsByContainer(tagNames);

                // Выполняем поиск для каждой группы
                foreach (var group in groupedTags)
                {
                    string containerName = group.Key;
                    List<string> tagPaths = group.Value;

                    _logger.Info($"Поиск тегов в контейнере {containerName}");

                    // Определяем тип контейнера (DB или таблица тегов)
                    bool isDbContainer = IsDbContainer(containerName);

                    if (isDbContainer)
                    {
                        // Ищем теги в блоке данных
                        var foundTags = FindTagsInDbContainer(containerName, tagPaths);
                        results.AddRange(foundTags);
                    }
                    else
                    {
                        // Ищем теги в таблице тегов ПЛК
                        var foundTags = FindTagsInPlcContainer(containerName, tagPaths);
                        results.AddRange(foundTags);
                    }
                }

                _logger.Info($"Найдено {results.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов: {ex.Message}");
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
                    if (!result.ContainsKey(parsedTag.Container))
                    {
                        result[parsedTag.Container] = new List<string>();
                    }

                    result[parsedTag.Container].Add(parsedTag.TagName);
                }
                else
                {
                    // Если контейнер не определен, используем имя тега как есть
                    _logger.Warn($"Не удалось определить контейнер для тега {tagName}");
                }
            }

            return result;
        }

        /// <summary>
        /// Определение типа контейнера
        /// </summary>
        private bool IsDbContainer(string containerName)
        {
            // Проверяем, является ли контейнер блоком данных
            // Обычно DB имеют имена, начинающиеся с "DB" или содержащие "_DB"
            return containerName.StartsWith("DB", StringComparison.OrdinalIgnoreCase) ||
                   containerName.Contains("_DB", StringComparison.OrdinalIgnoreCase);
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
            // Результат по умолчанию
            ParsedTag result = new ParsedTag
            {
                Container = string.Empty,
                TagName = fullTagName,
                IsDB = false
            };

            try
            {
                // Проверяем наличие кавычек
                if (fullTagName.Contains("\""))
                {
                    // Ищем закрывающую кавычку
                    int endQuoteIndex = fullTagName.IndexOf("\"", 1);

                    if (endQuoteIndex > 0)
                    {
                        // Извлекаем контейнер (часть в кавычках)
                        result.Container = fullTagName.Substring(1, endQuoteIndex - 1);

                        // Проверяем, есть ли точка после закрывающей кавычки
                        if (fullTagName.Length > endQuoteIndex + 1 && fullTagName[endQuoteIndex + 1] == '.')
                        {
                            // Это тег DB, так как есть часть после точки
                            result.IsDB = true;
                            result.TagName = fullTagName.Substring(endQuoteIndex + 2); // Пропускаем кавычку и точку
                        }
                        else
                        {
                            // Это тег PLC, так как нет части после точки
                            result.IsDB = false;
                            // TagName остается полным именем, так как мы не можем определить только имя тега
                        }
                    }
                }
                else if (fullTagName.Contains("."))
                {
                    // Формат без кавычек, но с точкой
                    var parts = fullTagName.Split(new[] { '.' }, 2);

                    if (parts.Length >= 2)
                    {
                        result.Container = parts[0];
                        result.TagName = parts[1];
                        // Предполагаем, что это DB, если есть точка
                        result.IsDB = true;
                    }
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
                // Это зависит от конкретной реализации API TIA Portal
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

                if (lowerType.Contains("bool"))
                    return TagDataType.Bool;
                else if (lowerType.Equals("int") || lowerType.Contains("int16") || lowerType.Contains("word"))
                    return TagDataType.Int;
                else if (lowerType.Equals("dint") || lowerType.Contains("int32") || lowerType.Contains("dword"))
                    return TagDataType.DInt;
                else if (lowerType.Contains("real") || lowerType.Contains("float"))
                    return TagDataType.Real;
                else if (lowerType.Contains("string"))
                    return TagDataType.String;
                else if (lowerType.StartsWith("udt_") || lowerType.Contains("struct") || lowerType.Contains("udt"))
                    return TagDataType.UDT;
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
                // Находим блок данных в проекте TIA Portal
                var db = FindDataBlock(dbName);

                if (db == null)
                {
                    _logger.Warn($"Блок данных {dbName} не найден");
                    return results;
                }

                // Проверяем, оптимизирован ли блок данных
                bool isOptimized = IsDataBlockOptimized(db);

                // Для каждого пути к тегу
                foreach (string tagPath in tagPaths)
                {
                    try
                    {
                        // Находим тег по пути
                        var tagMember = FindTagMemberInDataBlock(db, tagPath);

                        if (tagMember != null)
                        {
                            // Получаем тип данных
                            string dataTypeName = GetMemberDataTypeName(tagMember);

                            // Создаем объект TagDefinition
                            var tagDefinition = new TagDefinition
                            {
                                Id = Guid.NewGuid(),
                                Name = tagPath,
                                GroupName = dbName,
                                IsDbTag = true,
                                IsOptimized = isOptimized,
                                DataType = GetTagDataType(dataTypeName, tagPath, dbName),
                                Comment = tagMember.GetAttribute("Comment")?.ToString()
                            };

                            // Добавляем тег в результаты
                            results.Add(tagDefinition);

                            _logger.Info($"Найден тег в DB: {dbName}.{tagPath} с типом данных {tagDefinition.DataType}");
                        }
                        else
                        {
                            _logger.Warn($"Тег {tagPath} не найден в блоке данных {dbName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при поиске тега {tagPath} в блоке данных {dbName}: {ex.Message}");
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

                // Для каждого имени тега
                foreach (string tagName in tagNames)
                {
                    try
                    {
                        // Находим тег по имени
                        var plcTag = FindPlcTagInTable(tagTable, tagName);

                        if (plcTag != null)
                        {
                            // Получаем тип данных
                            string dataTypeName = plcTag.DataTypeName;

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

                            _logger.Info($"Найден PLC тег: {tableName}.{tagName} с типом данных {tagDefinition.DataType}");
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
        private PlcBlock FindDataBlock(string dbName)
        {
            try
            {
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
                                // Ищем блок данных по имени
                                foreach (var block in plcSoftware.BlockGroup.Blocks)
                                {
                                    if (block is PlcBlock plcBlock &&
                                        string.Equals(plcBlock.Name, dbName, StringComparison.OrdinalIgnoreCase))
                                    {
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
                _logger.Error($"Ошибка при поиске блока данных {dbName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Метод для нахождения таблицы PLC тегов по имени
        /// </summary>
        private PlcTagTable FindPlcTagTable(string tableName)
        {
            try
            {
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
                                // Ищем таблицу тегов по имени
                                foreach (var tagTable in plcSoftware.TagTableGroup.TagTables)
                                {
                                    if (string.Equals(tagTable.Name, tableName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        return tagTable;
                                    }
                                }
                            }
                        }
                    }
                }
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
                // Получаем свойство "Optimized block access"
                var properties = db.GetAttributeInfos();
                var optimizedAccessProperty = properties.FirstOrDefault(p => p.Name == "Optimized block access");

                if (optimizedAccessProperty != null)
                {
                    // Use the attribute directly as a boolean
                    var attribute = db.GetAttribute("Optimized block access");
                    if (attribute is bool isOptimized)
                    {
                        return isOptimized;
                    }
                }
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
                // Получаем интерфейс блока данных
                // В TIA Portal Openness может использоваться PlcBlockInterface вместо BlockInterface
                var dbInterface = db.Interface;
                if (dbInterface == null)
                    return null;

                // Разбиваем путь на части
                string[] pathParts = tagPath.Split('.');

                // Начинаем поиск с корневого элемента
                Member currentMember = null;
                var members = dbInterface.Members;

                // Проходим по каждой части пути
                foreach (string part in pathParts)
                {
                    // Ищем член с указанным именем
                    currentMember = members.FirstOrDefault(m =>
                        string.Equals(m.Name, part, StringComparison.OrdinalIgnoreCase));

                    // Если часть пути не найдена, возвращаем null
                    if (currentMember == null)
                        return null;

                    // Переходим к дочерним элементам для следующей итерации
                    // В API TIA Portal дочерние элементы могут быть доступны через свойство Children или Members
                    members = GetChildMembers(currentMember);
                }

                return currentMember;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тега {tagPath} в блоке данных: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Вспомогательный метод для получения дочерних элементов Member
        /// </summary>
        /// <param name="member">Элемент, для которого нужно получить дочерние элементы</param>
        private IEnumerable<Member> GetChildMembers(Member member)
        {
            try
            {
                // Пытаемся получить дочерние элементы через свойство Members или Children
                // В разных версиях TIA Portal API могут быть разные способы доступа

                // Вариант 1: Проверяем свойство Members
                var membersProperty = member.GetType().GetProperty("Members");
                if (membersProperty != null)
                {
                    var members = membersProperty.GetValue(member) as IEnumerable<Member>;
                    if (members != null)
                    {
                        return members;
                    }
                }

                // Вариант 2: Проверяем свойство Children
                var childrenProperty = member.GetType().GetProperty("Children");
                if (childrenProperty != null)
                {
                    var children = childrenProperty.GetValue(member) as IEnumerable<Member>;
                    if (children != null)
                    {
                        return children;
                    }
                }

                // Вариант 3: Если не удалось найти дочерние элементы, возвращаем пустой список
                _logger.Warn($"Не удалось получить дочерние элементы для тега {member.Name}");
                return new List<Member>();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении дочерних элементов Member: {ex.Message}");
                return new List<Member>();
            }
        }

        /// <summary>
        /// Поиск тега в таблице PLC тегов по имени
        /// </summary>
        private PlcTag FindPlcTagInTable(PlcTagTable tagTable, string tagName)
        {
            try
            {
                // Ищем тег по имени
                foreach (var tag in tagTable.Tags)
                {
                    if (string.Equals(tag.Name, tagName, StringComparison.OrdinalIgnoreCase))
                    {
                        return tag;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тега {tagName} в таблице тегов: {ex.Message}");
            }

            return null;
        }
    }
}