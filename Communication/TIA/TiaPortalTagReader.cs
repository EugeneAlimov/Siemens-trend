//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Siemens.Engineering;
//using Siemens.Engineering.HW;
//using Siemens.Engineering.HW.Features;
//using Siemens.Engineering.SW;
//using Siemens.Engineering.SW.Blocks;
//using Siemens.Engineering.SW.Blocks.Interface;
//using Siemens.Engineering.SW.Tags;
//using SiemensTrend.Core.Logging;
//using SiemensTrend.Core.Models;

//namespace SiemensTrend.Communication.TIA
//{
//    /// <summary>
//    /// Сервис для чтения тегов из проекта TIA Portal
//    /// </summary>
//    public class TiaPortalTagReader
//    {
//        private readonly Logger _logger;
//        private readonly TiaPortalCommunicationService _tiaService;
//        private readonly string _exportPath;

//        /// <summary>
//        /// Конструктор
//        /// </summary>
//        /// <param name="logger">Логгер</param>
//        /// <param name="tiaService">Сервис коммуникации с TIA Portal</param>
//        /// <param name="exportPath">Путь для экспорта временных данных (опционально)</param>
//        public TiaPortalTagReader(Logger logger, TiaPortalCommunicationService tiaService, string exportPath = null)
//        {
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            _tiaService = tiaService ?? throw new ArgumentNullException(nameof(tiaService));
//            _exportPath = exportPath ?? Path.Combine(Path.GetTempPath(), "SiemensTrend");

//            // Создаем директории для экспорта, если они не существуют
//            if (!string.IsNullOrEmpty(_exportPath))
//            {
//                try
//                {
//                    Directory.CreateDirectory(_exportPath);
//                    Directory.CreateDirectory(Path.Combine(_exportPath, "TagTables"));
//                    Directory.CreateDirectory(Path.Combine(_exportPath, "DB"));
//                }
//                catch (Exception ex)
//                {
//                    _logger.Error($"Ошибка при создании директорий для экспорта: {ex.Message}");
//                }
//            }
//        }

//        /// <summary>
//        /// Чтение всех тегов из проекта
//        /// </summary>
//        /// <returns>Объект PlcData, содержащий теги ПЛК и DB</returns>
//        public async Task<PlcData> ReadAllTagsAsync()
//        {
//            var plcData = new PlcData();

//            try
//            {
//                _logger.Info("Чтение тегов из проекта TIA Portal...");

//                // Получаем программное обеспечение ПЛК
//                var plcSoftware = _tiaService.GetPlcSoftware();
//                if (plcSoftware == null)
//                {
//                    _logger.Error("Не удалось получить PlcSoftware из проекта");
//                    return plcData;
//                }

//                // Выполняем чтение тегов в отдельном потоке для избежания блокировки UI
//                await Task.Run(() => {
//                    try
//                    {
//                        // Читаем теги ПЛК
//                        ReadPlcTags(plcSoftware, plcData);

//                        // Читаем теги блоков данных
//                        ReadDataBlocks(plcSoftware, plcData);
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при чтении тегов в фоновом потоке: {ex.Message}");
//                    }
//                });

//                _logger.Info($"Чтение тегов завершено: {plcData.PlcTags.Count} тегов ПЛК, {plcData.DbTags.Count} тегов DB");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при чтении тегов: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//            }

//            return plcData;
//        }

//        /// <summary>
//        /// Чтение тегов ПЛК из таблиц тегов
//        /// </summary>
//        private void ReadPlcTags(PlcSoftware plcSoftware, PlcData plcData)
//        {
//            _logger.Info("Чтение тегов ПЛК из таблиц тегов...");

//            try
//            {
//                // Получаем и обрабатываем все таблицы тегов
//                ProcessTagTableGroup(plcSoftware.TagTableGroup, plcData);

//                _logger.Info($"Чтение тегов ПЛК завершено. Найдено {plcData.PlcTags.Count} тегов");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при чтении тегов ПЛК: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Рекурсивная обработка групп таблиц тегов
//        /// </summary>
//        private void ProcessTagTableGroup(PlcTagTableGroup group, PlcData plcData, string parentPath = "")
//        {
//            try
//            {
//                // Обработка таблиц тегов в текущей группе
//                foreach (var tagTable in group.TagTables)
//                {
//                    ProcessTagTable(tagTable as PlcTagTable, plcData, parentPath);
//                }

//                // Рекурсивная обработка подгрупп
//                foreach (var subgroup in group.Groups)
//                {
//                    string newPath = string.IsNullOrEmpty(parentPath) ?
//                        subgroup.Name : $"{parentPath}/{subgroup.Name}";

//                    ProcessTagTableGroup(subgroup as PlcTagTableUserGroup, plcData, newPath);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке группы таблиц тегов: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Обработка таблицы тегов
//        /// </summary>
//        private void ProcessTagTable(PlcTagTable tagTable, PlcData plcData, string groupPath)
//        {
//            if (tagTable == null)
//            {
//                _logger.Warn("Получена пустая таблица тегов");
//                return;
//            }

//            _logger.Info($"Обработка таблицы тегов: {tagTable.Name}");

//            // Полный путь к таблице
//            string fullTablePath = string.IsNullOrEmpty(groupPath) ?
//                tagTable.Name : $"{groupPath}/{tagTable.Name}";

//            try
//            {
//                // Обрабатываем каждый тег в таблице
//                foreach (var tag in tagTable.Tags)
//                {
//                    try
//                    {
//                        // Приводим к типу PlcTag
//                        var plcTag = tag as Siemens.Engineering.SW.Tags.PlcTag;
//                        if (plcTag == null) continue;

//                        // Получаем атрибуты тега
//                        string name = plcTag.Name;
//                        string dataTypeString = "Unknown";
//                        string address = "";
//                        string comment = "";

//                        try { dataTypeString = GetMultilingualText(plcTag.DataTypeName); } catch { }
//                        try { address = plcTag.LogicalAddress; } catch { }
//                        try { comment = GetMultilingualText(plcTag.Comment); } catch { }

//                        // Конвертируем строковый тип данных в TagDataType
//                        TagDataType dataType = ConvertToTagDataType(dataTypeString);

//                        // Создаем и добавляем тег в коллекцию
//                        var tagDefinition = new TagDefinition
//                        {
//                            Name = name,
//                            Address = address,
//                            DataType = dataType,
//                            Comment = comment,
//                            GroupName = tagTable.Name,
//                            IsOptimized = false,  // Теги ПЛК не бывают оптимизированными
//                            IsUDT = dataTypeString.StartsWith("UDT_") || dataTypeString.Contains("type")
//                        };

//                        plcData.PlcTags.Add(tagDefinition);
//                        _logger.Debug($"Добавлен тег ПЛК: {name} ({dataTypeString}) @ {address}");
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при обработке тега: {ex.Message}");
//                    }
//                }

//                // Экспортируем таблицу тегов в XML, если указан путь экспорта
//                if (!string.IsNullOrEmpty(_exportPath))
//                {
//                    ExportTagTableToXml(tagTable, fullTablePath);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке таблицы тегов {tagTable.Name}: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Получение строкового значения из объекта MultilingualText
//        /// </summary>
//        private string GetMultilingualText(object multilingualTextObj)
//        {
//            try
//            {
//                if (multilingualTextObj == null)
//                    return string.Empty;

//                // Если это уже string, просто возвращаем его
//                if (multilingualTextObj is string textString)
//                    return textString;

//                // Если это MultilingualText, используем ToString() или другой доступный метод
//                if (multilingualTextObj is Siemens.Engineering.MultilingualText mlText)
//                {
//                    // В зависимости от версии API, доступ к тексту может различаться
//                    try
//                    {
//                        // Пытаемся использовать доступные свойства
//                        var itemsProperty = mlText.GetType().GetProperty("Items");
//                        if (itemsProperty != null)
//                        {
//                            // У MultilingualText может быть словарь Items[culture]
//                            var items = itemsProperty.GetValue(mlText) as System.Collections.IDictionary;
//                            if (items != null && items.Count > 0)
//                            {
//                                // Берем первый элемент словаря
//                                foreach (var key in items.Keys)
//                                {
//                                    var value = items[key];
//                                    if (value != null)
//                                        return value.ToString();
//                                    break;
//                                }
//                            }
//                        }
//                    }
//                    catch
//                    {
//                        // Если не получилось, пробуем простой ToString
//                        return mlText.ToString();
//                    }
//                }

//                // Для других объектов просто преобразуем в строку
//                return multilingualTextObj.ToString();
//            }
//            catch
//            {
//                return string.Empty;
//            }
//        }

//        /// <summary>
//        /// Чтение блоков данных
//        /// </summary>
//        private void ReadDataBlocks(PlcSoftware plcSoftware, PlcData plcData)
//        {
//            _logger.Info("Чтение блоков данных...");

//            try
//            {
//                // Обрабатываем группы блоков данных
//                ProcessBlockGroup(plcSoftware.BlockGroup, plcData);

//                _logger.Info($"Чтение блоков данных завершено. Найдено {plcData.DbTags.Count} переменных DB");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при чтении блоков данных: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Рекурсивная обработка групп блоков
//        /// </summary>
//        private void ProcessBlockGroup(PlcBlockGroup group, PlcData plcData, string parentPath = "")
//        {
//            try
//            {
//                string groupPath = string.IsNullOrEmpty(parentPath) ?
//                    group.Name : $"{parentPath}/{group.Name}";

//                _logger.Debug($"Обработка группы блоков: {groupPath}");

//                // Обрабатываем блоки в текущей группе
//                foreach (var block in group.Blocks)
//                {
//                    if (block is DataBlock db)
//                    {
//                        ProcessDataBlock(db, plcData, groupPath);
//                    }
//                }

//                // Рекурсивная обработка подгрупп
//                foreach (var subgroup in group.Groups)
//                {
//                    ProcessBlockGroup(subgroup as PlcBlockGroup, plcData, groupPath);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке группы блоков: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Обработка блока данных
//        /// </summary>
//        private void ProcessDataBlock(DataBlock db, PlcData plcData, string groupPath)
//        {
//            _logger.Info($"Обработка блока данных: {db.Name}");

//            try
//            {
//                // Проверяем наличие интерфейса
//                if (db.Interface == null)
//                {
//                    _logger.Warn($"Блок данных {db.Name} не имеет интерфейса");
//                    return;
//                }

//                // Проверяем наличие членов в блоке данных
//                bool hasMembers = false;
//                try
//                {
//                    // В новых версиях TIA Portal доступ к членам осуществляется через свойство Members
//                    hasMembers = db.Interface.Members != null && db.Interface.Members.Count() > 0;
//                }
//                catch
//                {
//                    _logger.Warn($"Не удалось получить члены блока данных {db.Name}");
//                }

//                if (!hasMembers)
//                {
//                    _logger.Warn($"Блок данных {db.Name} не имеет переменных");
//                    return;
//                }

//                // Определяем, является ли блок оптимизированным
//                bool isOptimized = db.MemoryLayout == MemoryLayout.Optimized;

//                // Определяем, является ли блок UDT или Safety
//                bool isUDT = groupPath.Contains("PLC data types");
//                bool isSafety = false;

//                try
//                {
//                    var programmingLanguage = GetMultilingualText(db.GetAttribute("ProgrammingLanguage"));
//                    isSafety = programmingLanguage == "F_DB";
//                }
//                catch
//                {
//                    // Игнорируем ошибки при попытке определить Safety
//                }

//                // Рекурсивно обрабатываем переменные блока данных
//                ProcessDbMembers(db.Interface, db.Name, "", plcData, isOptimized, isUDT, isSafety);

//                // Экспортируем блок данных в XML, если указан путь экспорта
//                if (!string.IsNullOrEmpty(_exportPath))
//                {
//                    ExportDataBlockToXml(db, plcData);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке блока данных {db.Name}: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Рекурсивная обработка членов блока данных
//        /// </summary>
//        private void ProcessDbMembers(IEngineeringObject memberContainer, string dbName, string parentPath,
//            PlcData plcData, bool isOptimized, bool isUDT, bool isSafety)
//        {
//            try
//            {
//                // Пытаемся получить члены различными способами в зависимости от версии API
//                IEnumerable<Member> members = null;

//                // Попытка 1: Используем свойство Members для PlcBlockInterface
//                if (memberContainer is PlcBlockInterface plcBlockInterface)
//                {
//                    members = plcBlockInterface.Members;
//                }
//                // Попытка 2: Используем метод GetMembers() или другой способ для других типов
//                else
//                {
//                    try
//                    {
//                        // Используем reflection для попытки вызова метода Members или GetMembers
//                        var membersProperty = memberContainer.GetType().GetProperty("Members");
//                        if (membersProperty != null)
//                        {
//                            var membersObj = membersProperty.GetValue(memberContainer);
//                            if (membersObj is IEnumerable<Member> membersList)
//                            {
//                                members = membersList;
//                            }
//                        }
//                        else
//                        {
//                            // Пробуем получить доступ к членам через GetComposition (старый API)
//                            try
//                            {
//                                var getCompositionMethod = memberContainer.GetType().GetMethod("GetComposition");
//                                if (getCompositionMethod != null)
//                                {
//                                    var membersObj = getCompositionMethod.Invoke(memberContainer, new object[] { "Members" });

//                                    // Пытаемся преобразовать в список членов через LINQ
//                                    if (membersObj != null)
//                                    {
//                                        var enumerableType = membersObj.GetType();
//                                        var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(typeof(Member));
//                                        members = (IEnumerable<Member>)castMethod.Invoke(null, new object[] { membersObj });
//                                    }
//                                }
//                            }
//                            catch
//                            {
//                                _logger.Warn("Не удалось получить доступ к членам через GetComposition");
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Warn($"Ошибка при получении членов: {ex.Message}");
//                    }
//                }

//                // Проверяем, получили ли мы члены
//                if (members == null || !members.Any())
//                {
//                    return;
//                }

//                // Обрабатываем каждый член
//                foreach (var member in members)
//                {
//                    try
//                    {
//                        if (member == null) continue;

//                        string name = member.Name;

//                        // Получаем тип данных через различные методы API
//                        string dataTypeString = "Unknown";
//                        try
//                        {
//                            var dataTypeNameProp = member.GetType().GetProperty("DataTypeName");
//                            if (dataTypeNameProp != null)
//                            {
//                                var dataTypeObj = dataTypeNameProp.GetValue(member);
//                                dataTypeString = GetMultilingualText(dataTypeObj);
//                            }
//                            else
//                            {
//                                // Альтернативный подход - через GetAttribute
//                                try
//                                {
//                                    var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
//                                    if (getAttributeMethod != null)
//                                    {
//                                        var dataTypeObj = getAttributeMethod.Invoke(member, new object[] { "DataTypeName" });
//                                        dataTypeString = GetMultilingualText(dataTypeObj);
//                                    }
//                                }
//                                catch
//                                {
//                                    // Игнорируем ошибки
//                                }
//                            }
//                        }
//                        catch
//                        {
//                            // Игнорируем ошибки при получении типа данных
//                        }

//                        // Полный путь к переменной
//                        string memberPath = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}.{name}";

//                        // Полное имя переменной с именем блока данных
//                        string fullName = $"{dbName}.{memberPath}";

//                        // Проверяем, есть ли у члена вложенные элементы
//                        bool hasNestedMembers = false;
//                        IEngineeringObject nestedMemberContainer = null;

//                        try
//                        {
//                            // Пытаемся найти интерфейс или вложенные члены через reflection
//                            var interfaceProp = member.GetType().GetProperty("Interface");
//                            if (interfaceProp != null)
//                            {
//                                nestedMemberContainer = interfaceProp.GetValue(member) as IEngineeringObject;
//                                if (nestedMemberContainer != null)
//                                {
//                                    // Проверяем, есть ли члены в интерфейсе
//                                    var nestedMembersProp = nestedMemberContainer.GetType().GetProperty("Members");
//                                    if (nestedMembersProp != null)
//                                    {
//                                        var nestedMembersObj = nestedMembersProp.GetValue(nestedMemberContainer);
//                                        hasNestedMembers = nestedMembersObj != null &&
//                                                          (nestedMembersObj as IEnumerable<object>)?.Any() == true;
//                                    }
//                                }
//                            }
//                            else
//                            {
//                                // Пробуем получить через атрибуты или другие способы
//                                try
//                                {
//                                    var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
//                                    if (getAttributeMethod != null)
//                                    {
//                                        nestedMemberContainer = getAttributeMethod.Invoke(member, new object[] { "Interface" }) as IEngineeringObject;
//                                        if (nestedMemberContainer != null)
//                                        {
//                                            hasNestedMembers = true; // Предполагаем, что если есть интерфейс, то есть и члены
//                                        }
//                                    }
//                                }
//                                catch
//                                {
//                                    // Игнорируем ошибки
//                                }
//                            }
//                        }
//                        catch
//                        {
//                            // Игнорируем ошибки при попытке определить вложенные члены
//                        }

//                        if (hasNestedMembers && nestedMemberContainer != null)
//                        {
//                            // Рекурсивно обрабатываем вложенную структуру
//                            ProcessDbMembers(nestedMemberContainer, dbName, memberPath, plcData, isOptimized, isUDT, isSafety);
//                        }
//                        else
//                        {
//                            // Получаем комментарий
//                            string comment = "";
//                            try
//                            {
//                                var commentProp = member.GetType().GetProperty("Comment");
//                                if (commentProp != null)
//                                {
//                                    var commentObj = commentProp.GetValue(member);
//                                    comment = GetMultilingualText(commentObj);
//                                }
//                                else
//                                {
//                                    // Альтернативно через GetAttribute
//                                    try
//                                    {
//                                        var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
//                                        if (getAttributeMethod != null)
//                                        {
//                                            var commentObj = getAttributeMethod.Invoke(member, new object[] { "Comment" });
//                                            comment = GetMultilingualText(commentObj);
//                                        }
//                                    }
//                                    catch
//                                    {
//                                        // Игнорируем ошибки
//                                    }
//                                }
//                            }
//                            catch
//                            {
//                                // Игнорируем ошибки при получении комментария
//                            }

//                            // Создаем и добавляем тег блока данных
//                            var dbTag = new TagDefinition
//                            {
//                                Name = fullName,
//                                Address = isOptimized ? "Optimized" : "Standard",
//                                DataType = ConvertToTagDataType(dataTypeString),
//                                Comment = comment,
//                                GroupName = dbName,
//                                IsOptimized = isOptimized,
//                                IsUDT = isUDT || dataTypeString.StartsWith("UDT_") || dataTypeString.Contains("type"),
//                                IsSafety = isSafety
//                            };

//                            plcData.DbTags.Add(dbTag);
//                            _logger.Debug($"Добавлена переменная DB: {fullName} ({dataTypeString})");
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при обработке члена блока данных: {ex.Message}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке членов блока данных: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Экспорт таблицы тегов в XML
//        /// </summary>
//        private void ExportTagTableToXml(PlcTagTable tagTable, string tablePath)
//        {
//            try
//            {
//                // Создаем путь для файла XML
//                string directory = Path.Combine(_exportPath, "TagTables",
//                    Path.GetDirectoryName(tablePath.Replace('/', Path.DirectorySeparatorChar)) ?? "");

//                if (!Directory.Exists(directory))
//                {
//                    Directory.CreateDirectory(directory);
//                }

//                string fileName = Path.GetFileName(tablePath);
//                string filePath = Path.Combine(directory, $"{fileName}.xml");

//                // Создаем XML-документ
//                using (var writer = new StreamWriter(filePath))
//                {
//                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
//                    writer.WriteLine($"<TagTable Name=\"{tagTable.Name}\">");
//                    writer.WriteLine("  <Tags>");

//                    foreach (var tag in tagTable.Tags)
//                    {
//                        var plcTag = tag as Siemens.Engineering.SW.Tags.PlcTag;
//                        if (plcTag == null) continue;

//                        string name = plcTag.Name;
//                        string dataType = "Unknown";
//                        string address = "";
//                        string comment = "";

//                        try { dataType = GetMultilingualText(plcTag.DataTypeName); } catch { }
//                        try { address = plcTag.LogicalAddress; } catch { }
//                        try { comment = GetMultilingualText(plcTag.Comment); } catch { }

//                        writer.WriteLine($"    <Tag Name=\"{name}\" DataType=\"{dataType}\" Address=\"{address}\" Comment=\"{comment}\" />");
//                    }

//                    writer.WriteLine("  </Tags>");
//                    writer.WriteLine("</TagTable>");
//                }

//                _logger.Debug($"Таблица тегов экспортирована: {filePath}");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при экспорте таблицы тегов {tagTable.Name}: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Экспорт блока данных в XML
//        /// </summary>
//        private void ExportDataBlockToXml(DataBlock db, PlcData plcData)
//        {
//            try
//            {
//                // Создаем путь для файла XML
//                string directory = Path.Combine(_exportPath, "DB");

//                if (!Directory.Exists(directory))
//                {
//                    Directory.CreateDirectory(directory);
//                }

//                string filePath = Path.Combine(directory, $"{db.Name}.xml");

//                // Получаем теги этого блока данных
//                var dbTags = plcData.DbTags.Where(t => t.GroupName == db.Name).ToList();

//                // Создаем XML-документ
//                using (var writer = new StreamWriter(filePath))
//                {
//                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
//                    writer.WriteLine($"<DataBlock Name=\"{db.Name}\" Optimized=\"{(db.MemoryLayout == MemoryLayout.Optimized ? "true" : "false")}\">");
//                    writer.WriteLine("  <Variables>");

//                    foreach (var tag in dbTags)
//                    {
//                        // Получаем только имя переменной без имени блока данных
//                        string varName = tag.Name.StartsWith(db.Name + ".") ?
//                            tag.Name.Substring(db.Name.Length + 1) : tag.Name;

//                        writer.WriteLine($"    <Variable Name=\"{varName}\" Type=\"{tag.DataType}\" />");
//                    }

//                    writer.WriteLine("  </Variables>");
//                    writer.WriteLine("</DataBlock>");
//                }

//                _logger.Debug($"Блок данных экспортирован: {filePath}");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при экспорте блока данных {db.Name}: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Конвертация строкового типа данных в TagDataType
//        /// </summary>
//        private TagDataType ConvertToTagDataType(string dataTypeString)
//        {
//            if (string.IsNullOrEmpty(dataTypeString))
//                return TagDataType.Other;

//            string lowerType = dataTypeString.ToLower();

//            if (lowerType.Contains("bool"))
//                return TagDataType.Bool;
//            else if (lowerType.Contains("int") && !lowerType.Contains("dint"))
//                return TagDataType.Int;
//            else if (lowerType.Contains("dint"))
//                return TagDataType.DInt;
//            else if (lowerType.Contains("real"))
//                return TagDataType.Real;
//            else if (lowerType.Contains("string"))
//                return TagDataType.String;
//            else if (lowerType.StartsWith("udt_") || lowerType.Contains("type"))
//                return TagDataType.UDT;
//            else
//                return TagDataType.Other;
//        }
//    }
//}

//======================================================================
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Siemens.Engineering;
//using Siemens.Engineering.HW;
//using Siemens.Engineering.HW.Features;
//using Siemens.Engineering.SW;
//using Siemens.Engineering.SW.Blocks;
//using Siemens.Engineering.SW.Blocks.Interface;
//using Siemens.Engineering.SW.Tags;
//using Siemens.Engineering.SW.Types;
//using SiemensTrend.Core.Logging;
//using SiemensTrend.Core.Models;

//namespace SiemensTrend.Communication.TIA
//{
//    /// <summary>
//    /// Сервис для чтения тегов из проекта TIA Portal
//    /// </summary>
//    public class TiaPortalTagReader
//    {
//        private readonly Logger _logger;
//        private readonly TiaPortalCommunicationService _tiaService;
//        private readonly string _exportPath;

//        /// <summary>
//        /// Конструктор
//        /// </summary>
//        public TiaPortalTagReader(Logger logger, TiaPortalCommunicationService tiaService, string exportPath = null)
//        {
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            _tiaService = tiaService ?? throw new ArgumentNullException(nameof(tiaService));
//            _exportPath = exportPath ?? Path.Combine(Path.GetTempPath(), "SiemensTrend");

//            // Создаем директории для экспорта, если они не существуют
//            if (!string.IsNullOrEmpty(_exportPath))
//            {
//                try
//                {
//                    Directory.CreateDirectory(_exportPath);
//                    Directory.CreateDirectory(Path.Combine(_exportPath, "TagTables"));
//                    Directory.CreateDirectory(Path.Combine(_exportPath, "DB"));
//                }
//                catch (Exception ex)
//                {
//                    _logger.Error($"Ошибка при создании директорий для экспорта: {ex.Message}");
//                }
//            }
//        }

//        /// <summary>
//        /// Чтение всех тегов из проекта
//        /// </summary>
//        public async Task<PlcData> ReadAllTagsAsync()
//        {
//            var plcData = new PlcData();

//            try
//            {
//                _logger.Info("Чтение тегов из проекта TIA Portal...");

//                // Получаем программное обеспечение ПЛК
//                var plcSoftware = _tiaService.GetPlcSoftware();
//                if (plcSoftware == null)
//                {
//                    _logger.Error("Не удалось получить PlcSoftware из проекта");
//                    return plcData;
//                }

//                // Выполняем чтение тегов в отдельном потоке для избежания блокировки UI
//                //await Task.Run(() => {
//                    try
//                    {
//                        // Читаем теги ПЛК
//                        ReadPlcTags(plcSoftware, plcData);

//                        // Читаем теги блоков данных
//                        ReadDataBlocks(plcSoftware, plcData);

//                        // Читаем пользовательские типы данных
//                        ReadUserDataTypes(plcSoftware, plcData);
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при чтении тегов в фоновом потоке: {ex.Message}");
//                    }
//                //});

//                _logger.Info($"Чтение тегов завершено: {plcData.PlcTags.Count} тегов ПЛК, {plcData.DbTags.Count} тегов DB");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при чтении тегов: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//            }

//            return plcData;
//        }

//        /// <summary>
//        /// Чтение тегов ПЛК из таблиц тегов
//        /// </summary>
//        private void ReadPlcTags(PlcSoftware plcSoftware, PlcData plcData)
//        {
//            _logger.Info("Чтение тегов ПЛК из таблиц тегов...");

//            try
//            {
//                // Получаем и обрабатываем все таблицы тегов
//                ProcessTagTableGroup(plcSoftware.TagTableGroup, plcData);

//                _logger.Info($"Чтение тегов ПЛК завершено. Найдено {plcData.PlcTags.Count} тегов");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при чтении тегов ПЛК: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Рекурсивная обработка групп таблиц тегов
//        /// </summary>
//        private void ProcessTagTableGroup(PlcTagTableGroup group, PlcData plcData, string parentPath = "")
//        {
//            try
//            {
//                // Обработка таблиц тегов в текущей группе
//                foreach (var tagTable in group.TagTables)
//                {
//                    ProcessTagTable(tagTable as PlcTagTable, plcData, parentPath);
//                }

//                // Рекурсивная обработка подгрупп
//                foreach (var subgroup in group.Groups)
//                {
//                    string newPath = string.IsNullOrEmpty(parentPath) ?
//                        subgroup.Name : $"{parentPath}/{subgroup.Name}";

//                    ProcessTagTableGroup(subgroup as PlcTagTableUserGroup, plcData, newPath);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке группы таблиц тегов: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Обработка таблицы тегов
//        /// </summary>
//        private void ProcessTagTable(PlcTagTable tagTable, PlcData plcData, string groupPath)
//        {
//            if (tagTable == null)
//            {
//                _logger.Warn("Получена пустая таблица тегов");
//                return;
//            }

//            _logger.Info($"Обработка таблицы тегов: {tagTable.Name}");

//            // Полный путь к таблице
//            string fullTablePath = string.IsNullOrEmpty(groupPath) ?
//                tagTable.Name : $"{groupPath}/{tagTable.Name}";

//            try
//            {
//                // Обрабатываем каждый тег в таблице
//                foreach (var tag in tagTable.Tags)
//                {
//                    try
//                    {
//                        // Приводим к типу PlcTag
//                        var plcTag = tag as Siemens.Engineering.SW.Tags.PlcTag;
//                        if (plcTag == null) continue;

//                        // Получаем атрибуты тега
//                        string name = plcTag.Name;
//                        string dataTypeString = "Unknown";
//                        string address = "";
//                        string comment = "";

//                        try { dataTypeString = GetMultilingualText(plcTag.DataTypeName); } catch { }
//                        try { address = plcTag.LogicalAddress; } catch { }
//                        try { comment = GetMultilingualText(plcTag.Comment); } catch { }

//                        // Конвертируем строковый тип данных в TagDataType
//                        TagDataType dataType = ConvertToTagDataType(dataTypeString);

//                        // Создаем и добавляем тег в коллекцию
//                        var tagDefinition = new TagDefinition
//                        {
//                            Name = name,
//                            Address = address,
//                            DataType = dataType,
//                            Comment = comment,
//                            GroupName = tagTable.Name,
//                            IsOptimized = false,  // Теги ПЛК не бывают оптимизированными
//                            IsUDT = dataTypeString.StartsWith("UDT_") ||
//                                    dataTypeString.Contains("type") ||
//                                    !IsBasicDataType(dataTypeString)
//                        };

//                        plcData.PlcTags.Add(tagDefinition);
//                        _logger.Debug($"Добавлен тег ПЛК: {name} ({dataTypeString}) @ {address}");
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при обработке тега: {ex.Message}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке таблицы тегов {tagTable.Name}: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Проверка, является ли тип данных базовым
//        /// </summary>
//        private bool IsBasicDataType(string dataType)
//        {
//            if (string.IsNullOrEmpty(dataType))
//                return false;

//            string lowerType = dataType.ToLower();
//            string[] basicTypes = { "bool", "byte", "word", "dword", "lword", "char",
//                                   "int", "dint", "lint", "uint", "udint", "ulint",
//                                   "real", "lreal", "time", "date", "time_of_day", "date_and_time",
//                                   "string", "wstring", "s5time", "timer", "counter" };

//            return basicTypes.Any(t => lowerType.Contains(t));
//        }

//        /// <summary>
//        /// Получение строкового значения из объекта MultilingualText
//        /// </summary>
//        private string GetMultilingualText(object multilingualTextObj)
//        {
//            try
//            {
//                if (multilingualTextObj == null)
//                    return string.Empty;

//                // Если это уже string, просто возвращаем его
//                if (multilingualTextObj is string textString)
//                    return textString;

//                // Если это MultilingualText, используем ToString() или другой доступный метод
//                if (multilingualTextObj is Siemens.Engineering.MultilingualText mlText)
//                {
//                    // В зависимости от версии API, доступ к тексту может различаться
//                    try
//                    {
//                        // Пытаемся использовать доступные свойства через рефлексию
//                        var itemsProperty = mlText.GetType().GetProperty("Items");
//                        if (itemsProperty != null)
//                        {
//                            // У MultilingualText может быть словарь Items[culture]
//                            var items = itemsProperty.GetValue(mlText) as System.Collections.IDictionary;
//                            if (items != null && items.Count > 0)
//                            {
//                                // Берем первый элемент словаря
//                                foreach (var key in items.Keys)
//                                {
//                                    var value = items[key];
//                                    if (value != null)
//                                        return value.ToString();
//                                    break;
//                                }
//                            }
//                        }
//                    }
//                    catch
//                    {
//                        // Если не получилось, пробуем простой ToString
//                        return mlText.ToString();
//                    }
//                }

//                // Для других объектов просто преобразуем в строку
//                return multilingualTextObj.ToString();
//            }
//            catch
//            {
//                return string.Empty;
//            }
//        }

//        /// <summary>
//        /// Чтение блоков данных
//        /// </summary>
//        private void ReadDataBlocks(PlcSoftware plcSoftware, PlcData plcData)
//        {
//            _logger.Info("Чтение блоков данных...");

//            try
//            {
//                // Обрабатываем группы блоков данных
//                ProcessBlockGroup(plcSoftware.BlockGroup, plcData);

//                _logger.Info($"Чтение блоков данных завершено. Найдено {plcData.DbTags.Count} переменных DB");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при чтении блоков данных: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Чтение пользовательских типов данных (UDT)
//        /// </summary>
//        private void ReadUserDataTypes(PlcSoftware plcSoftware, PlcData plcData)
//        {
//            _logger.Info("Чтение пользовательских типов данных (UDT)...");

//            try
//            {
//                // Получаем группу пользовательских типов данных
//                if (plcSoftware.TypeGroup != null)
//                {
//                    foreach (var plcType in plcSoftware.TypeGroup.Types)
//                    {
//                        ProcessPlcType(plcType, plcData);
//                    }
//                }

//                _logger.Info("Чтение пользовательских типов данных завершено");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при чтении пользовательских типов данных: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Обработка пользовательского типа данных
//        /// </summary>
//        private void ProcessPlcType(PlcType plcType, PlcData plcData)
//        {
//            try
//            {
//                _logger.Debug($"Обработка пользовательского типа данных: {plcType.Name}");

//                // Для типов UDT мы не добавляем их напрямую в список тегов,
//                // но сохраняем информацию о них для использования при обработке тегов DB
//                // При необходимости здесь можно добавить дополнительную логику
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке пользовательского типа данных {plcType.Name}: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Рекурсивная обработка групп блоков
//        /// </summary>
//        private void ProcessBlockGroup(PlcBlockGroup group, PlcData plcData, string parentPath = "")
//        {
//            try
//            {
//                string groupPath = string.IsNullOrEmpty(parentPath) ?
//                    group.Name : $"{parentPath}/{group.Name}";

//                _logger.Debug($"Обработка группы блоков: {groupPath}");

//                // Обрабатываем блоки в текущей группе
//                foreach (var block in group.Blocks)
//                {
//                    if (block is DataBlock db)
//                    {
//                        ProcessDataBlock(db, plcData, groupPath);
//                    }
//                }

//                // Рекурсивная обработка подгрупп
//                foreach (var subgroup in group.Groups)
//                {
//                    ProcessBlockGroup(subgroup as PlcBlockGroup, plcData, groupPath);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке группы блоков: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Обработка блока данных
//        /// </summary>
//        private void ProcessDataBlock(DataBlock db, PlcData plcData, string groupPath)
//        {
//            _logger.Info($"Обработка блока данных: {db.Name}");

//            try
//            {
//                // Проверяем наличие интерфейса
//                if (db.Interface == null)
//                {
//                    _logger.Warn($"Блок данных {db.Name} не имеет интерфейса");
//                    return;
//                }

//                // Проверяем наличие членов в блоке данных
//                bool hasMembers = false;
//                try
//                {
//                    // В новых версиях TIA Portal доступ к членам осуществляется через свойство Members
//                    hasMembers = db.Interface.Members != null && db.Interface.Members.Count() > 0;
//                }
//                catch
//                {
//                    _logger.Warn($"Не удалось получить члены блока данных {db.Name}");
//                }

//                if (!hasMembers)
//                {
//                    _logger.Warn($"Блок данных {db.Name} не имеет переменных");
//                    return;
//                }

//                // Определяем, является ли блок оптимизированным
//                bool isOptimized = IsOptimizedBlock(db);

//                // Определяем, является ли блок UDT или Safety
//                bool isUDT = groupPath.Contains("PLC data types") || IsUDTBlock(db);
//                bool isSafety = IsSafetyBlock(db);

//                _logger.Info($"Блок данных {db.Name} - {(isOptimized ? "оптимизированный" : "стандартный")}, " +
//                            $"UDT: {isUDT}, Safety: {isSafety}");

//                // Рекурсивно обрабатываем переменные блока данных
//                ProcessDbMembers(db.Interface, db.Name, "", plcData, isOptimized, isUDT, isSafety);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке блока данных {db.Name}: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Проверка, является ли блок данных оптимизированным
//        /// </summary>
//        private bool IsOptimizedBlock(DataBlock db)
//        {
//            try
//            {
//                return db.MemoryLayout == MemoryLayout.Optimized;
//            }
//            catch
//            {
//                // Используем альтернативный способ определения через атрибуты
//                try
//                {
//                    var memoryLayoutObj = db.GetAttribute("MemoryLayout");
//                    if (memoryLayoutObj != null)
//                    {
//                        string memoryLayout = memoryLayoutObj.ToString();
//                        return memoryLayout.Contains("Optimized");
//                    }
//                }
//                catch { }

//                return false;
//            }
//        }

//        /// <summary>
//        /// Проверка, является ли блок данных UDT
//        /// </summary>
//        private bool IsUDTBlock(DataBlock db)
//        {
//            try
//            {
//                // Проверяем по имени типа данных
//                var dataTypeObj = db.GetAttribute("DataTypeName");
//                if (dataTypeObj != null)
//                {
//                    string dataType = GetMultilingualText(dataTypeObj);
//                    return dataType.StartsWith("UDT_") || dataType.Contains("type");
//                }
//            }
//            catch { }

//            return false;
//        }

//        /// <summary>
//        /// Проверка, является ли блок данных Safety
//        /// </summary>
//        private bool IsSafetyBlock(DataBlock db)
//        {
//            try
//            {
//                // Проверяем по языку программирования
//                var programmingLanguage = GetMultilingualText(db.GetAttribute("ProgrammingLanguage"));
//                return programmingLanguage == "F_DB";
//            }
//            catch { }

//            // Альтернативный способ - по имени блока
//            return db.Name.Contains("_F_") || db.Name.EndsWith("_F");
//        }

//        /// <summary>
//        /// Рекурсивная обработка членов блока данных
//        /// </summary>
//        private void ProcessDbMembers(IEngineeringObject memberContainer, string dbName, string parentPath,
//            PlcData plcData, bool isOptimized, bool isUDT, bool isSafety)
//        {
//            try
//            {
//                // Пытаемся получить члены различными способами в зависимости от версии API
//                IEnumerable<Member> members = null;

//                // Попытка 1: Используем свойство Members для PlcBlockInterface
//                if (memberContainer is PlcBlockInterface plcBlockInterface)
//                {
//                    try
//                    {
//                        members = plcBlockInterface.Members;
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Debug($"Не удалось получить члены напрямую: {ex.Message}");
//                    }
//                }

//                // Попытка 2: Используем метод GetMembers() или другой способ для других типов
//                if (members == null)
//                {
//                    try
//                    {
//                        // Используем reflection для попытки вызова метода Members или GetMembers
//                        var membersProperty = memberContainer.GetType().GetProperty("Members");
//                        if (membersProperty != null)
//                        {
//                            var membersObj = membersProperty.GetValue(memberContainer);
//                            if (membersObj is IEnumerable<Member> membersList)
//                            {
//                                members = membersList;
//                            }
//                        }
//                        else
//                        {
//                            // Пробуем получить доступ к членам через GetComposition (старый API)
//                            try
//                            {
//                                var getCompositionMethod = memberContainer.GetType().GetMethod("GetComposition");
//                                if (getCompositionMethod != null)
//                                {
//                                    var membersObj = getCompositionMethod.Invoke(memberContainer, new object[] { "Members" });

//                                    // Пытаемся преобразовать в список членов через LINQ
//                                    if (membersObj != null)
//                                    {
//                                        var enumerableType = membersObj.GetType();
//                                        var castMethod = typeof(System.Linq.Enumerable).GetMethod("Cast").MakeGenericMethod(typeof(Member));
//                                        members = (IEnumerable<Member>)castMethod.Invoke(null, new object[] { membersObj });
//                                    }
//                                }
//                            }
//                            catch (Exception ex)
//                            {
//                                _logger.Debug($"Не удалось получить членов через GetComposition: {ex.Message}");
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Debug($"Не удалось получить членов через рефлексию: {ex.Message}");
//                    }
//                }

//                // Проверяем, получили ли мы члены
//                if (members == null || !members.Any())
//                {
//                    _logger.Debug($"Не найдено членов для {memberContainer.GetType().Name}");
//                    return;
//                }

//                // Обрабатываем каждый член
//                foreach (var member in members)
//                {
//                    try
//                    {
//                        if (member == null) continue;

//                        string name = member.Name;

//                        // Получаем тип данных через различные методы API
//                        string dataTypeString = "Unknown";
//                        try
//                        {
//                            var dataTypeNameProp = member.GetType().GetProperty("DataTypeName");
//                            if (dataTypeNameProp != null)
//                            {
//                                var dataTypeObj = dataTypeNameProp.GetValue(member);
//                                dataTypeString = GetMultilingualText(dataTypeObj);
//                            }
//                            else
//                            {
//                                // Альтернативный подход - через GetAttribute
//                                try
//                                {
//                                    var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
//                                    if (getAttributeMethod != null)
//                                    {
//                                        var dataTypeObj = getAttributeMethod.Invoke(member, new object[] { "DataTypeName" });
//                                        dataTypeString = GetMultilingualText(dataTypeObj);
//                                    }
//                                }
//                                catch
//                                {
//                                    _logger.Debug($"Не удалось получить тип данных через GetAttribute для {name}");
//                                }
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.Debug($"Ошибка при получении типа данных для {name}: {ex.Message}");
//                        }

//                        // Полный путь к переменной
//                        string memberPath = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}.{name}";

//                        // Полное имя переменной с именем блока данных
//                        string fullName = $"{dbName}.{memberPath}";

//                        // Проверяем, есть ли у члена вложенные элементы (структура или UDT)
//                        bool hasNestedMembers = false;
//                        IEngineeringObject nestedMemberContainer = null;

//                        try
//                        {
//                            // Пытаемся найти интерфейс или вложенные члены через reflection
//                            var interfaceProp = member.GetType().GetProperty("Interface");
//                            if (interfaceProp != null)
//                            {
//                                nestedMemberContainer = interfaceProp.GetValue(member) as IEngineeringObject;
//                                if (nestedMemberContainer != null)
//                                {
//                                    // Проверяем, есть ли члены в интерфейсе
//                                    var nestedMembersProp = nestedMemberContainer.GetType().GetProperty("Members");
//                                    if (nestedMembersProp != null)
//                                    {
//                                        var nestedMembersObj = nestedMembersProp.GetValue(nestedMemberContainer);
//                                        hasNestedMembers = nestedMembersObj != null &&
//                                                          (nestedMembersObj as System.Collections.IEnumerable)?.GetEnumerator().MoveNext() == true;
//                                    }
//                                }
//                            }
//                            else
//                            {
//                                // Пробуем получить через атрибуты или другие способы
//                                try
//                                {
//                                    var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
//                                    if (getAttributeMethod != null)
//                                    {
//                                        nestedMemberContainer = getAttributeMethod.Invoke(member, new object[] { "Interface" }) as IEngineeringObject;
//                                        if (nestedMemberContainer != null)
//                                        {
//                                            hasNestedMembers = true; // Предполагаем, что если есть интерфейс, то есть и члены
//                                        }
//                                    }
//                                }
//                                catch
//                                {
//                                    _logger.Debug($"Не удалось получить интерфейс через GetAttribute для {name}");
//                                }
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.Debug($"Ошибка при проверке вложенных членов для {name}: {ex.Message}");
//                        }

//                        if (hasNestedMembers && nestedMemberContainer != null)
//                        {
//                            // Рекурсивно обрабатываем вложенную структуру
//                            ProcessDbMembers(nestedMemberContainer, dbName, memberPath, plcData, isOptimized, isUDT, isSafety);
//                        }
//                        else
//                        {
//                            // Получаем комментарий
//                            string comment = "";
//                            try
//                            {
//                                var commentProp = member.GetType().GetProperty("Comment");
//                                if (commentProp != null)
//                                {
//                                    var commentObj = commentProp.GetValue(member);
//                                    comment = GetMultilingualText(commentObj);
//                                }
//                                else
//                                {
//                                    // Альтернативно через GetAttribute
//                                    try
//                                    {
//                                        var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
//                                        if (getAttributeMethod != null)
//                                        {
//                                            var commentObj = getAttributeMethod.Invoke(member, new object[] { "Comment" });
//                                            comment = GetMultilingualText(commentObj);
//                                        }
//                                    }
//                                    catch
//                                    {
//                                        _logger.Debug($"Не удалось получить комментарий через GetAttribute для {name}");
//                                    }
//                                }
//                            }
//                            catch (Exception ex)
//                            {
//                                _logger.Debug($"Ошибка при получении комментария для {name}: {ex.Message}");
//                            }

//                            // Для оптимизированных DB не включаем теги, которые не имеют поддерживаемого типа данных
//                            if (isOptimized && !IsSupportedTagType(dataTypeString))
//                            {
//                                _logger.Debug($"Пропускаем тег {fullName} с неподдерживаемым типом данных {dataTypeString}");
//                                continue;
//                            }

//                            // Создаем и добавляем тег блока данных
//                            var dbTag = new TagDefinition
//                            {
//                                Name = fullName,
//                                Address = isOptimized ? "Optimized" : GetMemberAddress(member, dbName),
//                                DataType = ConvertToTagDataType(dataTypeString),
//                                Comment = comment,
//                                GroupName = dbName,
//                                IsOptimized = isOptimized,
//                                IsUDT = isUDT || dataTypeString.StartsWith("UDT_") || dataTypeString.Contains("type"),
//                                IsSafety = isSafety
//                            };

//                            plcData.DbTags.Add(dbTag);
//                            _logger.Debug($"Добавлена переменная DB: {fullName} ({dataTypeString})");
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при обработке члена блока данных: {ex.Message}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при обработке членов блока данных: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Проверка, поддерживается ли тип данных для мониторинга
//        /// </summary>
//        private bool IsSupportedTagType(string dataTypeString)
//        {
//            if (string.IsNullOrEmpty(dataTypeString))
//                return false;

//            string lowerType = dataTypeString.ToLower();

//            // Поддерживаемые типы: bool, int, dint, real
//            return lowerType.Contains("bool") ||
//                   lowerType.Contains("int") ||
//                   lowerType.Contains("dint") ||
//                   lowerType.Contains("real");
//        }

//        /// <summary>
//        /// Получение адреса члена блока данных
//        /// </summary>
//        private string GetMemberAddress(Member member, string dbName)
//        {
//            try
//            {
//                // Пытаемся получить смещение (offset) члена
//                var offsetProp = member.GetType().GetProperty("Offset");
//                if (offsetProp != null)
//                {
//                    var offsetObj = offsetProp.GetValue(member);
//                    if (offsetObj != null)
//                    {
//                        int offset = Convert.ToInt32(offsetObj);
//                        return $"{dbName}.DBX{offset}.0"; // Для bool
//                    }
//                }
//                else
//                {
//                    // Альтернативный подход через GetAttribute
//                    try
//                    {
//                        var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
//                        if (getAttributeMethod != null)
//                        {
//                            var offsetObj = getAttributeMethod.Invoke(member, new object[] { "Offset" });
//                            if (offsetObj != null)
//                            {
//                                int offset = Convert.ToInt32(offsetObj);
//                                return $"{dbName}.DBX{offset}.0"; // Для bool
//                            }
//                        }
//                    }
//                    catch
//                    {
//                        // Игнорируем ошибки
//                    }
//                }
//            }
//            catch
//            {
//                // Игнорируем ошибки при получении адреса
//            }

//            return "Unknown";
//        }

//        /// <summary>
//        /// Конвертация строкового типа данных в TagDataType
//        /// </summary>
//        private TagDataType ConvertToTagDataType(string dataTypeString)
//        {
//            if (string.IsNullOrEmpty(dataTypeString))
//                return TagDataType.Other;

//            string lowerType = dataTypeString.ToLower();

//            if (lowerType.Contains("bool"))
//                return TagDataType.Bool;
//            else if (lowerType.Contains("int") && !lowerType.Contains("dint"))
//                return TagDataType.Int;
//            else if (lowerType.Contains("dint"))
//                return TagDataType.DInt;
//            else if (lowerType.Contains("real"))
//                return TagDataType.Real;
//            else if (lowerType.Contains("string"))
//                return TagDataType.String;
//            else if (lowerType.StartsWith("udt_") || lowerType.Contains("type"))
//                return TagDataType.UDT;
//            else
//                return TagDataType.Other;
//        }
//    }
//}

//=============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Улучшенная реализация для чтения тегов из проекта TIA Portal
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

                // Теперь проверяем, что соединение еще активно
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

                    // Используем защищенное чтение блоков данных
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

                // Обрабатываем каждый блок данных по отдельности
                foreach (var db in allDataBlocks)
                {
                    try
                    {
                        _logger.Info($"Обработка блока данных: {db.Name}");

                        // Запрос свойств каждого блока данных в отдельном try-catch
                        bool isOptimized = false;
                        bool isUDT = false;
                        bool isSafety = false;

                        try { isOptimized = db.MemoryLayout == MemoryLayout.Optimized; }
                        catch (Exception ex) { _logger.Debug($"Ошибка при определении оптимизации: {ex.Message}"); }

                        try { isUDT = db.Name.Contains("UDT") || db.Name.Contains("Type"); }
                        catch (Exception ex) { _logger.Debug($"Ошибка при определении UDT: {ex.Message}"); }

                        // Осторожно обрабатываем члены блока данных
                        ProcessDbMembersSafe(db, plcData, isOptimized, isUDT, isSafety, ref dbTagCount);

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

        private void CollectDataBlocks(PlcBlockGroup group, List<DataBlock> dataBlocks)
        {
            try
            {
                // Собираем блоки данных из текущей группы
                foreach (var block in group.Blocks)
                {
                    if (block is DataBlock db)
                    {
                        dataBlocks.Add(db);
                    }
                }

                // Рекурсивно обрабатываем подгруппы
                foreach (var subgroup in group.Groups)
                {
                    try
                    {
                        CollectDataBlocks(subgroup as PlcBlockGroup, dataBlocks);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Пропуск подгруппы из-за ошибки: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Ошибка при сборе блоков данных: {ex.Message}");
            }
        }

        private void ProcessDbMembersSafe(DataBlock db, PlcData plcData, bool isOptimized, bool isUDT, bool isSafety, ref int tagCount)
        {
            // Безопасная обработка интерфейса блока данных
            if (db.Interface == null)
            {
                _logger.Warn($"Блок данных {db.Name} не имеет интерфейса");
                return;
            }

            try
            {
                // Ограничиваем глубину обработки для снижения вероятности ошибок
                ExtractFlattenedDbTags(db, plcData, isOptimized, isUDT, isSafety, ref tagCount);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке членов блока данных {db.Name}: {ex.Message}");
            }
        }

        private void ExtractFlattenedDbTags(DataBlock db, PlcData plcData, bool isOptimized, bool isUDT, bool isSafety, ref int tagCount)
        {
            try
            {
                var members = db.Interface.Members;
                if (members == null || !members.Any())
                {
                    return;
                }

                foreach (var member in members)
                {
                    try
                    {
                        if (member == null) continue;

                        string name = member.Name;
                        string dataTypeString = "Unknown";

                        // Безопасное получение типа данных с использованием Reflection/GetAttribute
                        try
                        {
                            // Пробуем получить тип через рефлексию
                            var prop = member.GetType().GetProperty("DataTypeName");
                            if (prop != null)
                            {
                                var value = prop.GetValue(member);
                                dataTypeString = GetMultilingualText(value);
                            }
                            else
                            {
                                // Альтернативно используем GetAttribute метод если он существует
                                var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
                                if (getAttributeMethod != null)
                                {
                                    var value = getAttributeMethod.Invoke(member, new object[] { "DataTypeName" });
                                    dataTypeString = GetMultilingualText(value);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"Ошибка при получении типа данных для {name}: {ex.Message}");
                        }

                        // Формируем имя тега
                        string fullName = $"{db.Name}.{name}";

                        // Определяем тип тега
                        TagDataType dataType = ConvertToTagDataType(dataTypeString);

                        // Проверяем, поддерживается ли тип данных
                        if (!IsSupportedTagType(dataType))
                        {
                            _logger.Debug($"Пропущен тег DB {fullName} с неподдерживаемым типом данных {dataTypeString}");
                            continue;
                        }

                        // Добавляем тег в список DB тегов
                        plcData.DbTags.Add(new TagDefinition
                        {
                            Name = fullName,
                            Address = isOptimized ? "Optimized" : "Standard",
                            DataType = dataType,
                            Comment = GetMemberComment(member),
                            GroupName = db.Name,
                            IsOptimized = isOptimized,
                            IsUDT = isUDT,
                            IsSafety = isSafety
                        });

                        tagCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Ошибка при обработке члена: {ex.Message}");
                        // Продолжаем с другими членами
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при извлечении тегов DB: {ex.Message}");
            }
        }

        // Вспомогательный метод для получения комментария
        private string GetMemberComment(object member)
        {
            try
            {
                // Пробуем получить комментарий через рефлексию
                var prop = member.GetType().GetProperty("Comment");
                if (prop != null)
                {
                    var value = prop.GetValue(member);
                    return GetMultilingualText(value);
                }

                // Альтернативно используем GetAttribute
                var getAttributeMethod = member.GetType().GetMethod("GetAttribute");
                if (getAttributeMethod != null)
                {
                    var value = getAttributeMethod.Invoke(member, new object[] { "Comment" });
                    return GetMultilingualText(value);
                }
            }
            catch
            {
                // Игнорируем ошибки при получении комментария
            }

            return string.Empty;
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
                    _logger.Error("Ошибка: plcSoftware не может быть null");
                    return;
                }

                if (plcSoftware.TagTableGroup == null)
                {
                    _logger.Error("Ошибка: TagTableGroup не найдена в plcSoftware");
                    return;
                }

                // Добавим подробное логирование
                _logger.Info($"TagTableGroup найдена: {plcSoftware.TagTableGroup.Name}");

                try
                {
                    // Логируем количество таблиц и групп перед обработкой
                    _logger.Info($"Количество таблиц тегов: {plcSoftware.TagTableGroup.TagTables.Count}");
                    _logger.Info($"Количество групп таблиц: {plcSoftware.TagTableGroup.Groups.Count}");

                    ProcessTagTableGroup(plcSoftware.TagTableGroup, plcData, ref tableCount, ref tagCount);

                    _logger.Info($"Обработано {tableCount} таблиц тегов, найдено {tagCount} тегов ПЛК");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Ошибка при обработке группы таблиц тегов: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                    }
                }

                _logger.Info($"Чтение тегов ПЛК завершено. Найдено {plcData.PlcTags.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при чтении тегов ПЛК: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
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
                    _logger.Error("Ошибка: group не может быть null");
                    return;
                }

                string groupName = string.IsNullOrEmpty(group.Name) ? "Default" : group.Name;
                _logger.Info($"Обработка группы таблиц тегов: {groupName}");

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
                                _logger.Warn($"Таблица тегов не может быть приведена к типу PlcTagTable");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Ошибка при обработке таблицы тегов: {ex.Message}");
                        }
                    }
                }
                else
                {
                    _logger.Warn($"TagTables равен null в группе {groupName}");
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
                            _logger.Warn($"Подгруппа не может быть приведена к типу PlcTagTableUserGroup");
                        }
                    }
                }
                else
                {
                    _logger.Warn($"Groups равен null в группе {groupName}");
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
        private void ProcessTagTable(PlcTagTable tagTable, PlcData plcData, string groupPath, ref int tagCount)
        {
            if (tagTable == null)
            {
                _logger.Warn("Получена пустая таблица тегов");
                return;
            }

            _logger.Info($"Обработка таблицы тегов: {tagTable.Name}");

            try
            {
                // Проверяем наличие тегов в таблице
                if (tagTable.Tags == null || tagTable.Tags.Count == 0)
                {
                    _logger.Warn($"Таблица тегов {tagTable.Name} не содержит тегов");
                    return;
                }

                _logger.Info($"Количество тегов в таблице {tagTable.Name}: {tagTable.Tags.Count}");

                // Обрабатываем каждый тег в таблице
                foreach (var tag in tagTable.Tags)
                {
                    try
                    {
                        // Приводим к типу PlcTag
                        var plcTag = tag as Siemens.Engineering.SW.Tags.PlcTag;
                        if (plcTag == null)
                        {
                            _logger.Debug($"Тег не может быть приведен к типу PlcTag, пропускаем");
                            continue;
                        }

                        // Получаем атрибуты тега
                        string name = plcTag.Name;
                        string dataTypeString = "Unknown";
                        string address = "";
                        string comment = "";

                        try { dataTypeString = GetMultilingualText(plcTag.DataTypeName); } catch (Exception ex) { _logger.Debug($"Ошибка при получении типа данных: {ex.Message}"); }
                        try { address = plcTag.LogicalAddress; } catch (Exception ex) { _logger.Debug($"Ошибка при получении адреса: {ex.Message}"); }
                        try { comment = GetMultilingualText(plcTag.Comment); } catch (Exception ex) { _logger.Debug($"Ошибка при получении комментария: {ex.Message}"); }

                        // Конвертируем строковый тип данных в TagDataType
                        TagDataType dataType = ConvertToTagDataType(dataTypeString);

                        // Проверяем, поддерживается ли тип данных
                        if (!IsSupportedTagType(dataType))
                        {
                            _logger.Debug($"Пропущен тег {name} с неподдерживаемым типом данных {dataTypeString}");
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
                        _logger.Debug($"Добавлен тег ПЛК: {name} ({dataTypeString}) @ {address}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при обработке тега: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке таблицы тегов {tagTable.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Чтение блоков данных
        /// </summary>
        private void ReadDataBlocks(PlcSoftware plcSoftware, PlcData plcData, ref int dbCount, ref int tagCount)
        {
            _logger.Info("Чтение блоков данных...");

            try
            {
                if (plcSoftware.BlockGroup == null)
                {
                    _logger.Error("Ошибка: BlockGroup не найдена в plcSoftware");
                    return;
                }

                _logger.Info($"BlockGroup найдена: {plcSoftware.BlockGroup.Name}");

                // Обрабатываем группы блоков данных
                ProcessBlockGroup(plcSoftware.BlockGroup, plcData, ref dbCount, ref tagCount);

                _logger.Info($"Чтение блоков данных завершено. Найдено {plcData.DbTags.Count} переменных DB в {dbCount} блоках");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при чтении блоков данных: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Рекурсивная обработка групп блоков
        /// </summary>
        private void ProcessBlockGroup(PlcBlockGroup group, PlcData plcData, ref int dbCount, ref int tagCount, string parentPath = "")
        {
            try
            {
                if (group == null)
                {
                    _logger.Error("Ошибка: group не может быть null");
                    return;
                }

                string groupPath = string.IsNullOrEmpty(parentPath) ?
                    group.Name : $"{parentPath}/{group.Name}";

                _logger.Debug($"Обработка группы блоков: {groupPath}");

                // Обрабатываем блоки в текущей группе
                if (group.Blocks != null)
                {
                    foreach (var block in group.Blocks)
                    {
                        if (block is DataBlock db)
                        {
                            ProcessDataBlock(db, plcData, groupPath, ref tagCount);
                            dbCount++;
                        }
                    }
                }
                else
                {
                    _logger.Warn($"Blocks равен null в группе {group.Name}");
                }

                // Рекурсивная обработка подгрупп
                if (group.Groups != null)
                {
                    foreach (var subgroup in group.Groups)
                    {
                        var plcBlockGroup = subgroup as PlcBlockGroup;
                        if (plcBlockGroup != null)
                        {
                            ProcessBlockGroup(plcBlockGroup, plcData, ref dbCount, ref tagCount, groupPath);
                        }
                        else
                        {
                            _logger.Warn($"Подгруппа не может быть приведена к типу PlcBlockGroup");
                        }
                    }
                }
                else
                {
                    _logger.Warn($"Groups равен null в группе {group.Name}");
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
        private void ProcessDataBlock(DataBlock db, PlcData plcData, string groupPath, ref int tagCount)
        {
            if (db == null)
            {
                _logger.Warn("Получен пустой блок данных");
                return;
            }

            _logger.Info($"Обработка блока данных: {db.Name}");

            try
            {
                // Проверяем наличие интерфейса
                if (db.Interface == null)
                {
                    _logger.Warn($"Блок данных {db.Name} не имеет интерфейса");
                    return;
                }

                // Определяем, является ли блок оптимизированным
                bool isOptimized = false;
                try
                {
                    isOptimized = db.MemoryLayout == MemoryLayout.Optimized;
                    _logger.Info($"Блок данных {db.Name} - {(isOptimized ? "оптимизированный" : "стандартный")}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Ошибка при определении типа блока данных: {ex.Message}");
                }

                // Определяем, является ли блок UDT или Safety
                bool isUDT = groupPath.Contains("PLC data types");
                bool isSafety = false;

                try
                {
                    var programmingLanguage = GetMultilingualText(db.GetAttribute("ProgrammingLanguage"));
                    isSafety = programmingLanguage == "F_DB";

                    if (isSafety)
                    {
                        _logger.Info($"Блок данных {db.Name} является Safety блоком (F_DB)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Ошибка при определении Safety блока: {ex.Message}");
                }

                // Рекурсивно обрабатываем переменные блока данных
                ProcessDbMembers(db.Interface, db.Name, "", plcData, isOptimized, isUDT, isSafety, ref tagCount);
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
            PlcData plcData, bool isOptimized, bool isUDT, bool isSafety, ref int tagCount)
        {
            try
            {
                if (memberContainer == null)
                {
                    _logger.Error("Ошибка: memberContainer не может быть null");
                    return;
                }

                // Пытаемся получить члены из контейнера
                var members = GetMembers(memberContainer);
                if (members == null || !members.Any())
                {
                    _logger.Debug($"Нет членов для контейнера {memberContainer.GetType().Name}");
                    return;
                }

                _logger.Debug($"Количество членов в контейнере: {members.Count()}");

                // Обрабатываем каждый член
                foreach (var member in members)
                {
                    try
                    {
                        if (member == null)
                        {
                            _logger.Debug("Пропускаем null-член");
                            continue;
                        }

                        // Получаем имя члена
                        string name = GetPropertyValue<string>(member, "Name") ?? "Unknown";
                        _logger.Debug($"Обработка члена: {name}");

                        // Получаем тип данных
                        string dataTypeString = "Unknown";
                        try
                        {
                            var dataTypeNameObj = GetPropertyValue<object>(member, "DataTypeName");
                            dataTypeString = GetMultilingualText(dataTypeNameObj);
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"Не удалось получить тип данных для {name}: {ex.Message}");
                        }

                        // Полный путь к переменной
                        string memberPath = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}.{name}";
                        string fullName = $"{dbName}.{memberPath}";

                        // Проверяем наличие вложенных элементов
                        var nestedMemberContainer = GetNestedMemberContainer(member);

                        if (nestedMemberContainer != null)
                        {
                            // Рекурсивно обрабатываем вложенную структуру
                            ProcessDbMembers(nestedMemberContainer, dbName, memberPath, plcData, isOptimized, isUDT, isSafety, ref tagCount);
                        }
                        else
                        {
                            // Получаем комментарий
                            string comment = "";
                            try
                            {
                                var commentObj = GetPropertyValue<object>(member, "Comment");
                                comment = GetMultilingualText(commentObj);
                            }
                            catch (Exception ex)
                            {
                                _logger.Debug($"Не удалось получить комментарий для {name}: {ex.Message}");
                            }

                            // Конвертируем тип данных
                            TagDataType dataType = ConvertToTagDataType(dataTypeString);

                            // Проверяем, поддерживается ли тип
                            if (!IsSupportedTagType(dataType))
                            {
                                _logger.Debug($"Пропущен тег DB {fullName} с неподдерживаемым типом данных {dataTypeString}");
                                continue;
                            }

                            // Создаем и добавляем тег блока данных
                            var dbTag = new TagDefinition
                            {
                                Name = fullName,
                                Address = isOptimized ? "Optimized" : GetMemberAddress(member, dbName),
                                DataType = dataType,
                                Comment = comment,
                                GroupName = dbName,
                                IsOptimized = isOptimized,
                                IsUDT = isUDT || dataTypeString.StartsWith("UDT_") || dataTypeString.Contains("type"),
                                IsSafety = isSafety
                            };

                            plcData.DbTags.Add(dbTag);
                            tagCount++;
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
        /// Получение вложенного контейнера членов
        /// </summary>
        private IEngineeringObject GetNestedMemberContainer(object member)
        {
            try
            {
                // Пытаемся получить интерфейс через разные пути
                var interfaceObj = GetPropertyValue<IEngineeringObject>(member, "Interface");
                if (interfaceObj != null)
                {
                    // Проверяем, есть ли члены в интерфейсе
                    var nestedMembers = GetMembers(interfaceObj);
                    if (nestedMembers != null && nestedMembers.Any())
                    {
                        return interfaceObj;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Ошибка при получении вложенного контейнера: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Получение членов контейнера
        /// </summary>
        private IEnumerable<object> GetMembers(object container)
        {
            try
            {
                // Попытка 1: Используем свойство Members
                var membersObj = GetPropertyValue<object>(container, "Members");
                if (membersObj != null && membersObj is IEnumerable<object> enumerableMembers)
                {
                    return enumerableMembers;
                }

                // Попытка 2: Используем метод GetComposition
                var getCompositionMethod = container.GetType().GetMethod("GetComposition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (getCompositionMethod != null)
                {
                    var compositionObj = getCompositionMethod.Invoke(container, new object[] { "Members" });
                    if (compositionObj != null && compositionObj is IEnumerable<object> enumerableComposition)
                    {
                        return enumerableComposition;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Ошибка при получении членов: {ex.Message}");
            }

            return Enumerable.Empty<object>();
        }

        /// <summary>
        /// Получение значения свойства через reflection
        /// </summary>
        private T GetPropertyValue<T>(object obj, string propertyName)
        {
            if (obj == null)
                return default;

            try
            {
                // Пытаемся получить свойство напрямую
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(obj);
                    if (value is T typedValue)
                    {
                        return typedValue;
                    }
                    else if (value != null && typeof(T) == typeof(object))
                    {
                        return (T)value;
                    }
                }

                // Альтернативная попытка через GetAttribute
                var getAttributeMethod = obj.GetType().GetMethod("GetAttribute");
                if (getAttributeMethod != null)
                {
                    var attributeValue = getAttributeMethod.Invoke(obj, new object[] { propertyName });
                    if (attributeValue is T typedAttribute)
                    {
                        return typedAttribute;
                    }
                    else if (attributeValue != null && typeof(T) == typeof(object))
                    {
                        return (T)attributeValue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Ошибка при получении свойства {propertyName}: {ex.Message}");
            }

            return default;
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

                // Для других объектов используем ToString
                return multilingualTextObj.ToString();
            }
            catch (Exception ex)
            {
                _logger.Debug($"Ошибка при получении многоязычного текста: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Получение адреса члена блока данных
        /// </summary>
        private string GetMemberAddress(object member, string dbName)
        {
            try
            {
                // Пытаемся получить смещение (offset) члена
                var offsetObj = GetPropertyValue<object>(member, "Offset");
                if (offsetObj != null)
                {
                    int offset = Convert.ToInt32(offsetObj);
                    return $"{dbName}.DBX{offset}.0"; // Для bool, остальные типы можно уточнить
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Ошибка при получении адреса: {ex.Message}");
            }

            return "Unknown";
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