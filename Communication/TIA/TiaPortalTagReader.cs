using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
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
                    ProcessTagTable(tagTable, plcData, parentPath);
                }

                // Рекурсивная обработка подгрупп
                foreach (var subgroup in group.Groups)
                {
                    string newPath = string.IsNullOrEmpty(parentPath) ?
                        subgroup.Name : $"{parentPath}/{subgroup.Name}";

                    ProcessTagTableGroup(subgroup, plcData, newPath);
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
                        // Получаем атрибуты тега
                        string name = tag.Name;
                        string dataTypeString = tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";
                        string address = tag.GetAttribute("LogicalAddress")?.ToString() ?? "";
                        string comment = tag.GetAttribute("Comment")?.ToString() ?? "";

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
                        _logger.Error($"Ошибка при обработке тега {tag.Name}: {ex.Message}");
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
                    ProcessBlockGroup(subgroup, plcData, groupPath);
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
                // Проверяем наличие интерфейса и членов
                if (db.Interface == null || !db.Interface.Members.Any())
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
                    var programmingLanguage = db.GetAttribute("ProgrammingLanguage")?.ToString();
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
                // Получаем коллекцию членов блока данных
                var members = memberContainer.GetComposition("Members");

                if (members == null || members.Count == 0)
                {
                    return;
                }

                foreach (var member in members)
                {
                    try
                    {
                        string name = member.GetAttribute("Name")?.ToString() ?? "Unknown";
                        string dataTypeString = member.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";

                        // Полный путь к переменной
                        string memberPath = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}.{name}";

                        // Полное имя переменной с именем блока данных
                        string fullName = $"{dbName}.{memberPath}";

                        // Проверяем наличие вложенных членов (структуры)
                        var nestedMembers = member.GetComposition("Members");

                        if (nestedMembers != null && nestedMembers.Count > 0)
                        {
                            // Рекурсивно обрабатываем вложенную структуру
                            ProcessDbMembers(member, dbName, memberPath, plcData, isOptimized, isUDT, isSafety);
                        }
                        else
                        {
                            // Создаем и добавляем тег блока данных
                            var dbTag = new TagDefinition
                            {
                                Name = fullName,
                                Address = isOptimized ? "Optimized" : "Standard",
                                DataType = ConvertToTagDataType(dataTypeString),
                                Comment = member.GetAttribute("Comment")?.ToString() ?? "",
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
                        string name = tag.Name;
                        string dataType = tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";
                        string address = tag.GetAttribute("LogicalAddress")?.ToString() ?? "";
                        string comment = tag.GetAttribute("Comment")?.ToString() ?? "";

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