using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    /// Сервис для чтения тегов из проекта TIA Portal
    /// </summary>
    public class TiaPortalTagReader
    {
        private readonly Logger _logger;
        private readonly TiaPortalCommunicationService _tiaService;
        private readonly string _exportPath;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logger">Логгер</param>
        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
        /// <param name="exportPath">Путь для экспорта временных данных (опционально)</param>
        public TiaPortalTagReader(Logger logger, TiaPortalCommunicationService tiaService, string exportPath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tiaService = tiaService ?? throw new ArgumentNullException(nameof(tiaService));
            _exportPath = exportPath ?? Path.Combine(Path.GetTempPath(), "SiemensTrend");

            // Создаем директории для экспорта, если они не существуют
            if (!string.IsNullOrEmpty(_exportPath))
            {
                try
                {
                    Directory.CreateDirectory(_exportPath);
                    Directory.CreateDirectory(Path.Combine(_exportPath, "TagTables"));
                    Directory.CreateDirectory(Path.Combine(_exportPath, "DB"));
                }
                catch (Exception ex)
                {
                    _logger.Error($"Ошибка при создании директорий для экспорта: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Чтение всех тегов из проекта
        /// </summary>
        /// <returns>Объект PlcData, содержащий теги ПЛК и DB</returns>
        public async Task<PlcData> ReadAllTagsAsync()
        {
            var plcData = new PlcData();

            try
            {
                _logger.Info("Чтение тегов из проекта TIA Portal...");

                // Получаем программное обеспечение ПЛК
                var plcSoftware = _tiaService.GetPlcSoftware();
                if (plcSoftware == null)
                {
                    _logger.Error("Не удалось получить PlcSoftware из проекта");
                    return plcData;
                }

                // Выполняем чтение тегов в отдельном потоке для избежания блокировки UI
                await Task.Run(() => {
                    try
                    {
                        // Читаем теги ПЛК
                        ReadPlcTags(plcSoftware, plcData);

                        // Читаем теги блоков данных
                        ReadDataBlocks(plcSoftware, plcData);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при чтении тегов в фоновом потоке: {ex.Message}");
                    }
                });

                _logger.Info($"Чтение тегов завершено: {plcData.PlcTags.Count} тегов ПЛК, {plcData.DbTags.Count} тегов DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при чтении тегов: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }

            return plcData;
        }

        /// <summary>
        /// Чтение тегов ПЛК из таблиц тегов
        /// </summary>
        private void ReadPlcTags(PlcSoftware plcSoftware, PlcData plcData)
        {
            _logger.Info("Чтение тегов ПЛК из таблиц тегов...");

            try
            {
                // Получаем и обрабатываем все таблицы тегов
                ProcessTagTableGroup(plcSoftware.TagTableGroup, plcData);

                _logger.Info($"Чтение тегов ПЛК завершено. Найдено {plcData.PlcTags.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при чтении тегов ПЛК: {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивная обработка групп таблиц тегов
        /// </summary>
        private void ProcessTagTableGroup(PlcTagTableGroup group, PlcData plcData, string parentPath = "")
        {
            try
            {
                // Обработка таблиц тегов в текущей группе
                foreach (var tagTable in group.TagTables)
                {
                    ProcessTagTable(tagTable as PlcTagTable, plcData, parentPath);
                }

                // Рекурсивная обработка подгрупп
                foreach (var subgroup in group.Groups)
                {
                    string newPath = string.IsNullOrEmpty(parentPath) ?
                        subgroup.Name : $"{parentPath}/{subgroup.Name}";

                    ProcessTagTableGroup(subgroup as PlcTagTableUserGroup, plcData, newPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке группы таблиц тегов: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка таблицы тегов
        /// </summary>
        private void ProcessTagTable(PlcTagTable tagTable, PlcData plcData, string groupPath)
        {
            if (tagTable == null)
            {
                _logger.Warn("Получена пустая таблица тегов");
                return;
            }

            _logger.Info($"Обработка таблицы тегов: {tagTable.Name}");

            // Полный путь к таблице
            string fullTablePath = string.IsNullOrEmpty(groupPath) ?
                tagTable.Name : $"{groupPath}/{tagTable.Name}";

            try
            {
                // Обрабатываем каждый тег в таблице
                foreach (var tag in tagTable.Tags)
                {
                    try
                    {
                        // Приводим к типу PlcTag
                        var plcTag = tag as Siemens.Engineering.SW.Tags.PlcTag;
                        if (plcTag == null) continue;

                        // Получаем атрибуты тега
                        string name = plcTag.Name;
                        string dataTypeString = "Unknown";
                        string address = "";
                        string comment = "";

                        try { dataTypeString = GetMultilingualText(plcTag.DataTypeName); } catch { }
                        try { address = plcTag.LogicalAddress; } catch { }
                        try { comment = GetMultilingualText(plcTag.Comment); } catch { }

                        // Конвертируем строковый тип данных в TagDataType
                        TagDataType dataType = ConvertToTagDataType(dataTypeString);

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
                        _logger.Debug($"Добавлен тег ПЛК: {name} ({dataTypeString}) @ {address}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при обработке тега: {ex.Message}");
                    }
                }

                // Экспортируем таблицу тегов в XML, если указан путь экспорта
                if (!string.IsNullOrEmpty(_exportPath))
                {
                    ExportTagTableToXml(tagTable, fullTablePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке таблицы тегов {tagTable.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение строкового значения из объекта MultilingualText
        /// </summary>
        private string GetMultilingualText(object multilingualTextObj)
        {
            try
            {
                if (multilingualTextObj == null)
                    return string.Empty;

                // Если это уже string, просто возвращаем его
                if (multilingualTextObj is string textString)
                    return textString;

                // Если это MultilingualText, используем ToString() или другой доступный метод
                if (multilingualTextObj is Siemens.Engineering.MultilingualText mlText)
                {
                    // В зависимости от версии API, доступ к тексту может различаться
                    try
                    {
                        // Пытаемся использовать доступные свойства
                        var itemsProperty = mlText.GetType().GetProperty("Items");
                        if (itemsProperty != null)
                        {
                            // У MultilingualText может быть словарь Items[culture]
                            var items = itemsProperty.GetValue(mlText) as System.Collections.IDictionary;
                            if (items != null && items.Count > 0)
                            {
                                // Берем первый элемент словаря
                                foreach (var key in items.Keys)
                                {
                                    var value = items[key];
                                    if (value != null)
                                        return value.ToString();
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Если не получилось, пробуем простой ToString
                        return mlText.ToString();
                    }
                }

                // Для других объектов просто преобразуем в строку
                return multilingualTextObj.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Чтение блоков данных
        /// </summary>
        private void ReadDataBlocks(PlcSoftware plcSoftware, PlcData plcData)
        {
            _logger.Info("Чтение блоков данных...");

            try
            {
                // Обрабатываем группы блоков данных
                ProcessBlockGroup(plcSoftware.BlockGroup, plcData);

                _logger.Info($"Чтение блоков данных завершено. Найдено {plcData.DbTags.Count} переменных DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при чтении блоков данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивная обработка групп блоков
        /// </summary>
        private void ProcessBlockGroup(PlcBlockGroup group, PlcData plcData, string parentPath = "")
        {
            try
            {
                string groupPath = string.IsNullOrEmpty(parentPath) ?
                    group.Name : $"{parentPath}/{group.Name}";

                _logger.Debug($"Обработка группы блоков: {groupPath}");

                // Обрабатываем блоки в текущей группе
                foreach (var block in group.Blocks)
                {
                    if (block is DataBlock db)
                    {
                        ProcessDataBlock(db, plcData, groupPath);
                    }
                }

                // Рекурсивная обработка подгрупп
                foreach (var subgroup in group.Groups)
                {
                    ProcessBlockGroup(subgroup as PlcBlockGroup, plcData, groupPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке группы блоков: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка блока данных
        /// </summary>
        private void ProcessDataBlock(DataBlock db, PlcData plcData, string groupPath)
        {
            _logger.Info($"Обработка блока данных: {db.Name}");

            try
            {
                // Проверяем наличие интерфейса
                if (db.Interface == null)
                {
                    _logger.Warn($"Блок данных {db.Name} не имеет интерфейса");
                    return;
                }

                // Проверяем наличие членов в блоке данных
                bool hasMembers = false;
                try
                {
                    // В новых версиях TIA Portal доступ к членам осуществляется через свойство Members
                    hasMembers = db.Interface.Members != null && db.Interface.Members.Count() > 0;
                }
                catch
                {
                    _logger.Warn($"Не удалось получить члены блока данных {db.Name}");
                }

                if (!hasMembers)
                {
                    _logger.Warn($"Блок данных {db.Name} не имеет переменных");
                    return;
                }

                // Определяем, является ли блок оптимизированным
                bool isOptimized = db.MemoryLayout == MemoryLayout.Optimized;

                // Определяем, является ли блок UDT или Safety
                bool isUDT = groupPath.Contains("PLC data types");
                bool isSafety = false;

                try
                {
                    var programmingLanguage = GetMultilingualText(db.GetAttribute("ProgrammingLanguage"));
                    isSafety = programmingLanguage == "F_DB";
                }
                catch
                {
                    // Игнорируем ошибки при попытке определить Safety
                }

                // Рекурсивно обрабатываем переменные блока данных
                ProcessDbMembers(db.Interface, db.Name, "", plcData, isOptimized, isUDT, isSafety);

                // Экспортируем блок данных в XML, если указан путь экспорта
                if (!string.IsNullOrEmpty(_exportPath))
                {
                    ExportDataBlockToXml(db, plcData);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке блока данных {db.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивная обработка членов блока данных
        /// </summary>
        private void ProcessDbMembers(IEngineeringObject memberContainer, string dbName, string parentPath,
            PlcData plcData, bool isOptimized, bool isUDT, bool isSafety)
        {
            try
            {
                // Пытаемся получить члены различными способами в зависимости от версии API
                IEnumerable<Member> members = null;

                // Попытка 1: Используем свойство Members для PlcBlockInterface
                if (memberContainer is PlcBlockInterface plcBlockInterface)
                {
                    members = plcBlockInterface.Members;
                }
                // Попытка 2: Используем метод GetMembers() или другой способ для других типов
                else
                {
                    try
                    {
                        // Используем reflection для попытки вызова метода Members или GetMembers
                        var membersProperty = memberContainer.GetType().GetProperty("Members");
                        if (membersProperty != null)
                        {
                            var membersObj = membersProperty.GetValue(memberContainer);
                            if (membersObj is IEnumerable<Member> membersList)
                            {
                                members = membersList;
                            }
                        }
                        else
                        {
                            // Пробуем получить доступ к членам через GetComposition (старый API)
                            try
                            {
                                var getCompositionMethod = memberContainer.GetType().GetMethod("GetComposition");
                                if (getCompositionMethod != null)
                                {
                                    var membersObj = getCompositionMethod.Invoke(memberContainer, new object[] { "Members" });

                                    // Пытаемся преобразовать в список членов через LINQ
                                    if (membersObj != null)
                                    {
                                        var enumerableType = membersObj.GetType();
                                        var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(typeof(Member));
                                        members = (IEnumerable<Member>)castMethod.Invoke(null, new object[] { membersObj });
                                    }
                                }
                            }
                            catch
                            {
                                _logger.Warn("Не удалось получить доступ к членам через GetComposition");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Ошибка при получении членов: {ex.Message}");
                    }
                }

                // Проверяем, получили ли мы члены
                if (members == null || !members.Any())
                {
                    return;
                }

                // Обрабатываем каждый член
                foreach (var member in members)
                {
                    try
                    {
                        if (member == null) continue;

                        string name = member.Name;

                        // Получаем тип данных через различные методы API
                        string dataTypeString = "Unknown";
                        try
                        {
                            var dataTypeNameProp = member.GetType().GetProperty("DataTypeName");
                            if (dataTypeNameProp != null)
                            {
                                var dataTypeObj = dataTypeNameProp.GetValue(member);
                                dataTypeString = GetMultilingualText(dataTypeObj);
                            }
                            else
                            {
                                // Альтернативный подход - через GetAttribute
                                try
                                {
                                    var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
                                    if (getAttributeMethod != null)
                                    {
                                        var dataTypeObj = getAttributeMethod.Invoke(member, new object[] { "DataTypeName" });
                                        dataTypeString = GetMultilingualText(dataTypeObj);
                                    }
                                }
                                catch
                                {
                                    // Игнорируем ошибки
                                }
                            }
                        }
                        catch
                        {
                            // Игнорируем ошибки при получении типа данных
                        }

                        // Полный путь к переменной
                        string memberPath = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}.{name}";

                        // Полное имя переменной с именем блока данных
                        string fullName = $"{dbName}.{memberPath}";

                        // Проверяем, есть ли у члена вложенные элементы
                        bool hasNestedMembers = false;
                        IEngineeringObject nestedMemberContainer = null;

                        try
                        {
                            // Пытаемся найти интерфейс или вложенные члены через reflection
                            var interfaceProp = member.GetType().GetProperty("Interface");
                            if (interfaceProp != null)
                            {
                                nestedMemberContainer = interfaceProp.GetValue(member) as IEngineeringObject;
                                if (nestedMemberContainer != null)
                                {
                                    // Проверяем, есть ли члены в интерфейсе
                                    var nestedMembersProp = nestedMemberContainer.GetType().GetProperty("Members");
                                    if (nestedMembersProp != null)
                                    {
                                        var nestedMembersObj = nestedMembersProp.GetValue(nestedMemberContainer);
                                        hasNestedMembers = nestedMembersObj != null &&
                                                          (nestedMembersObj as IEnumerable<object>)?.Any() == true;
                                    }
                                }
                            }
                            else
                            {
                                // Пробуем получить через атрибуты или другие способы
                                try
                                {
                                    var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
                                    if (getAttributeMethod != null)
                                    {
                                        nestedMemberContainer = getAttributeMethod.Invoke(member, new object[] { "Interface" }) as IEngineeringObject;
                                        if (nestedMemberContainer != null)
                                        {
                                            hasNestedMembers = true; // Предполагаем, что если есть интерфейс, то есть и члены
                                        }
                                    }
                                }
                                catch
                                {
                                    // Игнорируем ошибки
                                }
                            }
                        }
                        catch
                        {
                            // Игнорируем ошибки при попытке определить вложенные члены
                        }

                        if (hasNestedMembers && nestedMemberContainer != null)
                        {
                            // Рекурсивно обрабатываем вложенную структуру
                            ProcessDbMembers(nestedMemberContainer, dbName, memberPath, plcData, isOptimized, isUDT, isSafety);
                        }
                        else
                        {
                            // Получаем комментарий
                            string comment = "";
                            try
                            {
                                var commentProp = member.GetType().GetProperty("Comment");
                                if (commentProp != null)
                                {
                                    var commentObj = commentProp.GetValue(member);
                                    comment = GetMultilingualText(commentObj);
                                }
                                else
                                {
                                    // Альтернативно через GetAttribute
                                    try
                                    {
                                        var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
                                        if (getAttributeMethod != null)
                                        {
                                            var commentObj = getAttributeMethod.Invoke(member, new object[] { "Comment" });
                                            comment = GetMultilingualText(commentObj);
                                        }
                                    }
                                    catch
                                    {
                                        // Игнорируем ошибки
                                    }
                                }
                            }
                            catch
                            {
                                // Игнорируем ошибки при получении комментария
                            }

                            // Создаем и добавляем тег блока данных
                            var dbTag = new TagDefinition
                            {
                                Name = fullName,
                                Address = isOptimized ? "Optimized" : "Standard",
                                DataType = ConvertToTagDataType(dataTypeString),
                                Comment = comment,
                                GroupName = dbName,
                                IsOptimized = isOptimized,
                                IsUDT = isUDT || dataTypeString.StartsWith("UDT_") || dataTypeString.Contains("type"),
                                IsSafety = isSafety
                            };

                            plcData.DbTags.Add(dbTag);
                            _logger.Debug($"Добавлена переменная DB: {fullName} ({dataTypeString})");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при обработке члена блока данных: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке членов блока данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт таблицы тегов в XML
        /// </summary>
        private void ExportTagTableToXml(PlcTagTable tagTable, string tablePath)
        {
            try
            {
                // Создаем путь для файла XML
                string directory = Path.Combine(_exportPath, "TagTables",
                    Path.GetDirectoryName(tablePath.Replace('/', Path.DirectorySeparatorChar)) ?? "");

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string fileName = Path.GetFileName(tablePath);
                string filePath = Path.Combine(directory, $"{fileName}.xml");

                // Создаем XML-документ
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    writer.WriteLine($"<TagTable Name=\"{tagTable.Name}\">");
                    writer.WriteLine("  <Tags>");

                    foreach (var tag in tagTable.Tags)
                    {
                        var plcTag = tag as Siemens.Engineering.SW.Tags.PlcTag;
                        if (plcTag == null) continue;

                        string name = plcTag.Name;
                        string dataType = "Unknown";
                        string address = "";
                        string comment = "";

                        try { dataType = GetMultilingualText(plcTag.DataTypeName); } catch { }
                        try { address = plcTag.LogicalAddress; } catch { }
                        try { comment = GetMultilingualText(plcTag.Comment); } catch { }

                        writer.WriteLine($"    <Tag Name=\"{name}\" DataType=\"{dataType}\" Address=\"{address}\" Comment=\"{comment}\" />");
                    }

                    writer.WriteLine("  </Tags>");
                    writer.WriteLine("</TagTable>");
                }

                _logger.Debug($"Таблица тегов экспортирована: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при экспорте таблицы тегов {tagTable.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт блока данных в XML
        /// </summary>
        private void ExportDataBlockToXml(DataBlock db, PlcData plcData)
        {
            try
            {
                // Создаем путь для файла XML
                string directory = Path.Combine(_exportPath, "DB");

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string filePath = Path.Combine(directory, $"{db.Name}.xml");

                // Получаем теги этого блока данных
                var dbTags = plcData.DbTags.Where(t => t.GroupName == db.Name).ToList();

                // Создаем XML-документ
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    writer.WriteLine($"<DataBlock Name=\"{db.Name}\" Optimized=\"{(db.MemoryLayout == MemoryLayout.Optimized ? "true" : "false")}\">");
                    writer.WriteLine("  <Variables>");

                    foreach (var tag in dbTags)
                    {
                        // Получаем только имя переменной без имени блока данных
                        string varName = tag.Name.StartsWith(db.Name + ".") ?
                            tag.Name.Substring(db.Name.Length + 1) : tag.Name;

                        writer.WriteLine($"    <Variable Name=\"{varName}\" Type=\"{tag.DataType}\" />");
                    }

                    writer.WriteLine("  </Variables>");
                    writer.WriteLine("</DataBlock>");
                }

                _logger.Debug($"Блок данных экспортирован: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при экспорте блока данных {db.Name}: {ex.Message}");
            }
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