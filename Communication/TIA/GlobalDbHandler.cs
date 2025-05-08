using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Siemens.Engineering;
using Siemens.Engineering.SW.Blocks;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Класс для хранения информации о теге
    /// </summary>
    public class TagInfo
    {
        public TagDataType DataType { get; set; }
        public string Comment { get; set; }
        public string Address { get; set; }

        public TagInfo(TagDataType dataType, string comment, string address = "")
        {
            DataType = dataType;
            Comment = comment;
            Address = address;
        }
    }

    /// <summary>
    /// Специализированный класс для работы с GlobalDB в TIA Portal
    /// </summary>
    public class GlobalDbHandler
    {
        private readonly Logger _logger;

        public GlobalDbHandler(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Проверяет, является ли блок данных типом GlobalDB
        /// </summary>
        public bool IsGlobalDb(PlcBlock block)
        {
            return block != null && block.GetType().FullName.Contains("GlobalDB");
        }

        /// <summary>
        /// Ищет теги в GlobalDB по указанным путям
        /// </summary>
        public List<TagDefinition> FindTagsInGlobalDb(PlcBlock block, string dbName, List<string> tagPaths)
        {
            List<TagDefinition> results = new List<TagDefinition>();

            try
            {
                _logger.Info($"Начинаем специализированный поиск тегов в GlobalDB {dbName}");

                // Разбираем пути к тегам на составные части для более гибкого поиска
                var parsedPaths = ParseTagPaths(tagPaths);
                _logger.Info($"Разобрано {parsedPaths.Count} путей к тегам");

                // Получаем структуру GlobalDB через отражение
                var dbMembers = GetGlobalDbMembers(block);
                if (dbMembers == null || !dbMembers.Any())
                {
                    _logger.Error($"Не удалось получить члены GlobalDB {dbName}");
                    return results;
                }

                _logger.Info($"Получено {dbMembers.Count} членов GlobalDB {dbName}");

                // Строим карту тегов GlobalDB
                var tagMap = BuildGlobalDbTagMap(block, dbMembers);
                _logger.Info($"Построена карта тегов GlobalDB, содержащая {tagMap.Count} элементов");

                // Теперь ищем запрошенные теги в карте
                foreach (var parsedPath in parsedPaths)
                {
                    string fullPath = string.Join(".", parsedPath);

                    // Проверяем прямое совпадение пути
                    if (tagMap.TryGetValue(fullPath, out var tagInfo))
                    {
                        var tagDefinition = new TagDefinition
                        {
                            Id = Guid.NewGuid(),
                            Name = fullPath,
                            GroupName = dbName,
                            IsDbTag = true,
                            IsOptimized = false, // GlobalDB обычно не оптимизированы
                            DataType = tagInfo.DataType,
                            Comment = tagInfo.Comment
                        };

                        results.Add(tagDefinition);
                        _logger.Info($"Найден тег с точным совпадением: {dbName}.{fullPath}, тип: {tagInfo.DataType}");
                        continue;
                    }

                    // Если точного совпадения нет, ищем по частям пути
                    var foundTag = FindTagByPathParts(tagMap, parsedPath, dbName);
                    if (foundTag != null)
                    {
                        results.Add(foundTag);
                        _logger.Info($"Найден тег по частям пути: {dbName}.{fullPath}, тип: {foundTag.DataType}");
                        continue;
                    }

                    // Если и это не помогло, пробуем эвристический поиск
                    var inferredTag = InferTagFromPath(parsedPath, dbName);
                    if (inferredTag != null)
                    {
                        results.Add(inferredTag);
                        _logger.Info($"Определен тег эвристически: {dbName}.{fullPath}, тип: {inferredTag.DataType}");
                    }
                    else
                    {
                        _logger.Warn($"Не удалось найти тег {fullPath} в блоке данных {dbName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов в GlobalDB {dbName}: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Разбивает пути к тегам на составные части для более гибкого поиска
        /// </summary>
        private List<string[]> ParseTagPaths(List<string> tagPaths)
        {
            var result = new List<string[]>();

            foreach (var path in tagPaths)
            {
                var parts = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                result.Add(parts);

                _logger.Info($"Разбор пути {path} на части: {string.Join(", ", parts)}");
            }

            return result;
        }

        /// <summary>
        /// Получает члены GlobalDB через отражение
        /// </summary>
        private List<object> GetGlobalDbMembers(PlcBlock block)
        {
            try
            {
                // Типичные имена свойств/методов для получения членов GlobalDB
                string[] memberPropertyNames = new[] { "Members", "TagList", "Tags", "Variables", "Elements" };

                // Получаем тип блока данных
                Type dbType = block.GetType();

                // Пробуем получить члены через свойство
                foreach (var propertyName in memberPropertyNames)
                {
                    var property = dbType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (property != null)
                    {
                        var value = property.GetValue(block);
                        if (value is System.Collections.IEnumerable enumerable)
                        {
                            var result = new List<object>();
                            foreach (var item in enumerable)
                            {
                                result.Add(item);
                            }

                            if (result.Any())
                            {
                                _logger.Info($"Получены члены GlobalDB через свойство {propertyName}, количество: {result.Count}");
                                return result;
                            }
                        }
                    }
                }

                // Если свойства не сработали, пробуем методы
                string[] methodNames = new[] { "GetMembers", "GetTags", "GetVariables", "GetElements" };
                foreach (var methodName in methodNames)
                {
                    var method = dbType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                    if (method != null)
                    {
                        var value = method.Invoke(block, null);
                        if (value is System.Collections.IEnumerable enumerable)
                        {
                            var result = new List<object>();
                            foreach (var item in enumerable)
                            {
                                result.Add(item);
                            }

                            if (result.Any())
                            {
                                _logger.Info($"Получены члены GlobalDB через метод {methodName}, количество: {result.Count}");
                                return result;
                            }
                        }
                    }
                }

                // Если и методы не работают, пробуем получить через интерфейс блока
                try
                {
                    var interfaceProperty = dbType.GetProperty("Interface");
                    if (interfaceProperty != null)
                    {
                        var blockInterface = interfaceProperty.GetValue(block);
                        if (blockInterface != null)
                        {
                            var interfaceType = blockInterface.GetType();
                            var membersProperty = interfaceType.GetProperty("Members");
                            if (membersProperty != null)
                            {
                                var members = membersProperty.GetValue(blockInterface);
                                if (members is System.Collections.IEnumerable enumerable)
                                {
                                    var result = new List<object>();
                                    foreach (var item in enumerable)
                                    {
                                        result.Add(item);
                                    }

                                    if (result.Any())
                                    {
                                        _logger.Info($"Получены члены GlobalDB через Interface.Members, количество: {result.Count}");
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Ошибка при попытке получить члены GlobalDB через интерфейс: {ex.Message}");
                }

                // Если все способы не сработали, выводим диагностическую информацию
                _logger.Warn("Не удалось получить члены GlobalDB через стандартные подходы. Вывод доступных свойств:");
                foreach (var property in dbType.GetProperties())
                {
                    _logger.Info($"Свойство: {property.Name}, Тип: {property.PropertyType.Name}");

                    try
                    {
                        var value = property.GetValue(block);
                        _logger.Info($"  Значение: {(value != null ? value.ToString() : "null")}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Info($"  Ошибка при получении значения: {ex.Message}");
                    }
                }

                _logger.Warn("Доступные методы GlobalDB:");
                foreach (var method in dbType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    _logger.Info($"Метод: {method.Name}, Возвращаемый тип: {method.ReturnType.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении членов GlobalDB через отражение: {ex.Message}");
            }

            return new List<object>();
        }

        /// <summary>
        /// Строит карту тегов GlobalDB на основе полученных членов
        /// </summary>
        private Dictionary<string, TagInfo> BuildGlobalDbTagMap(
            PlcBlock block, List<object> members)
        {
            var result = new Dictionary<string, TagInfo>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Для каждого члена GlobalDB
                foreach (var member in members)
                {
                    try
                    {
                        // Получаем имя члена
                        string memberName = GetMemberName(member);
                        if (string.IsNullOrEmpty(memberName))
                            continue;

                        // Получаем тип данных
                        string dataTypeName = GetMemberDataTypeName(member);
                        var dataType = TiaTagTypeUtility.ConvertToTagDataType(dataTypeName);

                        // Получаем комментарий
                        string comment = GetMemberComment(member);

                        // Добавляем в карту
                        result[memberName] = new TagInfo(dataType, comment);
                        _logger.Info($"Добавлен тег в карту: {memberName}, тип: {dataType}, комментарий: {comment}");

                        // Если это структура (UDT), добавляем и её элементы
                        if (dataType == TagDataType.UDT)
                        {
                            // Пытаемся получить дочерние элементы
                            var childMembers = GetMemberChildren(member);
                            if (childMembers != null && childMembers.Any())
                            {
                                AddStructureElementsToMap(result, childMembers, memberName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при обработке члена GlobalDB: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при построении карты тегов GlobalDB: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Получает имя члена через отражение
        /// </summary>
        private string GetMemberName(object member)
        {
            try
            {
                // Типичные имена свойств для имени элемента
                string[] namePropertyNames = new[] { "Name", "TagName", "VariableName", "Identifier" };

                foreach (var propertyName in namePropertyNames)
                {
                    var property = member.GetType().GetProperty(propertyName);
                    if (property != null)
                    {
                        var value = property.GetValue(member);
                        if (value != null)
                        {
                            return value.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении имени члена GlobalDB: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Получает тип данных члена через отражение
        /// </summary>
        private string GetMemberDataTypeName(object member)
        {
            try
            {
                // Типичные имена свойств для типа данных
                string[] typePropertyNames = new[] { "DataType", "DataTypeName", "Type", "TypeName" };

                foreach (var propertyName in typePropertyNames)
                {
                    var property = member.GetType().GetProperty(propertyName);
                    if (property != null)
                    {
                        var value = property.GetValue(member);
                        if (value != null)
                        {
                            return value.ToString();
                        }
                    }
                }

                // Альтернативный подход: попробовать метод GetAttribute("DataType")
                var getAttributeMethod = member.GetType().GetMethod("GetAttribute", new[] { typeof(string) });
                if (getAttributeMethod != null)
                {
                    var dataTypeAttr = getAttributeMethod.Invoke(member, new object[] { "DataType" });
                    if (dataTypeAttr != null)
                    {
                        return dataTypeAttr.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении типа данных члена GlobalDB: {ex.Message}");
            }

            return "Unknown";
        }

        /// <summary>
        /// Получает комментарий члена через отражение
        /// </summary>
        private string GetMemberComment(object member)
        {
            try
            {
                // Типичные имена свойств для комментария
                string[] commentPropertyNames = new[] { "Comment", "Description" };

                foreach (var propertyName in commentPropertyNames)
                {
                    var property = member.GetType().GetProperty(propertyName);
                    if (property != null)
                    {
                        var value = property.GetValue(member);
                        if (value != null)
                        {
                            return value.ToString();
                        }
                    }
                }

                // Альтернативный подход: попробовать метод GetAttribute("Comment")
                var getAttributeMethod = member.GetType().GetMethod("GetAttribute", new[] { typeof(string) });
                if (getAttributeMethod != null)
                {
                    var commentAttr = getAttributeMethod.Invoke(member, new object[] { "Comment" });
                    if (commentAttr != null)
                    {
                        return commentAttr.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении комментария члена GlobalDB: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Получает дочерние элементы члена (для структур) через отражение
        /// </summary>
        private List<object> GetMemberChildren(object member)
        {
            try
            {
                // Типичные имена свойств/методов для получения дочерних элементов
                string[] childPropertyNames = new[] { "Members", "Children", "Elements", "Fields" };

                foreach (var propertyName in childPropertyNames)
                {
                    var property = member.GetType().GetProperty(propertyName);
                    if (property != null)
                    {
                        var value = property.GetValue(member);
                        if (value is System.Collections.IEnumerable enumerable)
                        {
                            var result = new List<object>();
                            foreach (var item in enumerable)
                            {
                                result.Add(item);
                            }

                            if (result.Any())
                            {
                                return result;
                            }
                        }
                    }
                }

                // Если свойства не сработали, пробуем методы
                string[] methodNames = new[] { "GetMembers", "GetChildren", "GetElements", "GetFields" };
                foreach (var methodName in methodNames)
                {
                    var method = member.GetType().GetMethod(methodName, Type.EmptyTypes);
                    if (method != null)
                    {
                        var value = method.Invoke(member, null);
                        if (value is System.Collections.IEnumerable enumerable)
                        {
                            var result = new List<object>();
                            foreach (var item in enumerable)
                            {
                                result.Add(item);
                            }

                            if (result.Any())
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении дочерних элементов члена GlobalDB: {ex.Message}");
            }

            return new List<object>();
        }

        /// <summary>
        /// Добавляет элементы структуры в карту тегов
        /// </summary>
        private void AddStructureElementsToMap(
            Dictionary<string, TagInfo> map,
            List<object> structElements,
            string parentPath)
        {
            foreach (var element in structElements)
            {
                try
                {
                    string elementName = GetMemberName(element);
                    if (string.IsNullOrEmpty(elementName))
                        continue;

                    string fullPath = $"{parentPath}.{elementName}";
                    string dataTypeName = GetMemberDataTypeName(element);
                    var dataType = TiaTagTypeUtility.ConvertToTagDataType(dataTypeName);
                    string comment = GetMemberComment(element);

                    // Добавляем элемент в карту
                    map[fullPath] = new TagInfo(dataType, comment);
                    _logger.Info($"Добавлен структурный элемент в карту: {fullPath}, тип: {dataType}");

                    // Рекурсивно обрабатываем вложенные структуры
                    if (dataType == TagDataType.UDT)
                    {
                        var childElements = GetMemberChildren(element);
                        if (childElements != null && childElements.Any())
                        {
                            AddStructureElementsToMap(map, childElements, fullPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Ошибка при добавлении элемента структуры в карту: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Ищет тег по частям пути
        /// </summary>
        private TagDefinition FindTagByPathParts(
            Dictionary<string, TagInfo> tagMap,
            string[] pathParts,
            string dbName)
        {
            try
            {
                // Обработка случая, когда первый элемент - родительская структура
                if (pathParts.Length > 1)
                {
                    string parentName = pathParts[0];

                    // Проверяем наличие родительского элемента в карте
                    if (tagMap.TryGetValue(parentName, out var parentInfo))
                    {
                        // Если родитель - структура
                        if (parentInfo.DataType == TagDataType.UDT || parentInfo.DataType == TagDataType.Other)
                        {
                            // Пробуем собрать полный путь с разным количеством элементов
                            for (int i = 1; i <= pathParts.Length; i++)
                            {
                                string partialPath = string.Join(".", pathParts.Take(i));

                                // Проверяем наличие в карте
                                if (tagMap.TryGetValue(partialPath, out var tagInfo))
                                {
                                    // Если нашли структуру и не все элементы пути обработаны,
                                    // продолжаем поиск
                                    if (i < pathParts.Length &&
                                        (tagInfo.DataType == TagDataType.UDT || tagInfo.DataType == TagDataType.Other))
                                    {
                                        continue;
                                    }

                                    // Если нашли непустую структуру - возвращаем тег
                                    var tagDefinition = new TagDefinition
                                    {
                                        Id = Guid.NewGuid(),
                                        Name = string.Join(".", pathParts),
                                        GroupName = dbName,
                                        IsDbTag = true,
                                        IsOptimized = false,
                                        DataType = tagInfo.DataType,
                                        Comment = tagInfo.Comment
                                    };

                                    return tagDefinition;
                                }
                            }

                            // Если в карте нет полного пути к тегу, но родительский элемент - структура,
                            // пробуем предположить тип данных по последнему элементу пути
                            string lastPart = pathParts.Last();
                            TagDataType inferredType = InferDataTypeFromName(lastPart);

                            var inferredTagDefinition = new TagDefinition
                            {
                                Id = Guid.NewGuid(),
                                Name = string.Join(".", pathParts),
                                GroupName = dbName,
                                IsDbTag = true,
                                IsOptimized = false,
                                DataType = inferredType,
                                Comment = $"Эвристически определенный элемент структуры {parentName}"
                            };

                            return inferredTagDefinition;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тега по частям пути: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Эвристически определяет тег на основе пути
        /// </summary>
        private TagDefinition InferTagFromPath(string[] pathParts, string dbName)
        {
            try
            {
                // Если путь достаточно специфичен, пробуем определить тип данных
                if (pathParts.Length >= 2)
                {
                    string fullPath = string.Join(".", pathParts);
                    string lastPart = pathParts.Last();

                    // Определяем тип данных по последней части пути
                    TagDataType inferredType = InferDataTypeFromName(lastPart);

                    var tagDefinition = new TagDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = fullPath,
                        GroupName = dbName,
                        IsDbTag = true,
                        IsOptimized = false,
                        DataType = inferredType,
                        Comment = "Эвристически определенный тег"
                    };

                    return tagDefinition;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при эвристическом определении тега: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Предполагает тип данных на основе имени элемента
        /// </summary>
        private TagDataType InferDataTypeFromName(string elementName)
        {
            string lowerName = elementName.ToLower();

            // Логические типы
            if (lowerName.Contains("bool") || lowerName.Contains("flag") ||
                lowerName.Contains("status") || lowerName.Contains("enable") ||
                lowerName.Contains("alarm") || lowerName.EndsWith("on") ||
                lowerName.EndsWith("off") || lowerName.EndsWith("active"))
            {
                return TagDataType.Bool;
            }

            // Типы с плавающей точкой
            if (lowerName.Contains("real") || lowerName.Contains("float") ||
                lowerName.Contains("value") || lowerName.Contains("temperature") ||
                lowerName.Contains("temp") || lowerName.Contains("pressure") ||
                lowerName.Contains("speed") || lowerName.Contains("position") ||
                lowerName.Contains("level") || lowerName.Contains("flow") ||
                lowerName.Contains("f") && Regex.IsMatch(lowerName, @"f\d+"))
            {
                return TagDataType.Real;
            }

            // Целочисленные типы
            if (lowerName.Contains("int") || lowerName.Contains("count") ||
                lowerName.Contains("index") || lowerName.Contains("number") ||
                lowerName.Contains("id") || lowerName.Contains("mode") ||
                lowerName.Contains("state") ||
                Regex.IsMatch(lowerName, @"^\d+$"))
            {
                // Определяем размер int
                if (lowerName.Contains("dint") || lowerName.Contains("long"))
                {
                    return TagDataType.DInt;
                }
                return TagDataType.Int;
            }

            // Структурные типы
            if (lowerName.Contains("struct") || lowerName.Contains("udt") ||
                lowerName.EndsWith("data") || lowerName.EndsWith("info") ||
                lowerName.Contains("config") || lowerName.Contains("params") ||
                lowerName.Length <= 2) // Короткие имена (e1, e2, R1, etc) часто структуры
            {
                return TagDataType.UDT;
            }

            // По умолчанию - другой тип
            return TagDataType.Other;
        }
    }
}