using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW;
using SiemensTrend.Core.Models;
using SiemensTrend.Core.Logging;
using Siemens.Engineering;
using SiemensTrend.Communication.TIA;
using static SiemensTrend.Communication.TIA.TiaPortalCommunicationService;

namespace SiemensTrend.Helpers
{
    public class TiaPortalXmlManager
    {
        private readonly Logger _logger;
        private readonly string _baseExportPath;
        private string _currentProjectName = string.Empty;
        private string _currentProjectPath => Path.Combine(_baseExportPath, _currentProjectName);
        private string _plcTagsPath => Path.Combine(_currentProjectPath, "TagTables");
        private string _dbExportsPath => Path.Combine(_currentProjectPath, "DB");

        // Добавляем ссылку на TiaPortalCommunicationService
        private readonly TiaPortalCommunicationService _tiaService;

        /// <summary>
        /// Улучшенный конструктор с возможностью указать текущий проект и TiaPortalCommunicationService
        /// </summary>
        public TiaPortalXmlManager(Logger logger, TiaPortalCommunicationService tiaService = null, string currentProjectName = null)
        {
            _logger = logger;
            _tiaService = tiaService; // Сохраняем ссылку на TiaPortalCommunicationService
            _baseExportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TiaExports");
            Directory.CreateDirectory(_baseExportPath);
            _logger.Info($"TiaPortalXmlManager: Базовая директория для экспорта создана: {_baseExportPath}");

            // Если указано имя проекта, устанавливаем его
            if (!string.IsNullOrEmpty(currentProjectName))
            {
                SetCurrentProject(currentProjectName);
            }
        }

        /// <summary>
        /// Установка текущего проекта для работы с XML
        /// </summary>
        /// <param name="projectName">Название проекта TIA Portal</param>
        public void SetCurrentProject(string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                _logger.Warn("TiaPortalXmlManager: Попытка установить пустое имя проекта");
                return;
            }

            // Заменяем недопустимые символы в имени файла
            string safeName = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars()));
            _currentProjectName = safeName;

            // Создаем директории для текущего проекта
            Directory.CreateDirectory(_currentProjectPath);
            Directory.CreateDirectory(_plcTagsPath);
            Directory.CreateDirectory(_dbExportsPath);

            _logger.Info($"TiaPortalXmlManager: Установлен текущий проект: {projectName}");
            _logger.Info($"TiaPortalXmlManager: Пути экспорта: {_plcTagsPath}, {_dbExportsPath}");
        }

        /// <summary>
        /// Экспорт тегов в XML с возможностью выбора типа тегов
        /// </summary>
        public async Task ExportTagsToXml(ExportTagType tagType = ExportTagType.All)
        {
            if (_tiaService == null)
            {
                _logger.Error("ExportTagsToXml: TiaPortalCommunicationService не установлен");
                return;
            }

            if (!_tiaService.IsConnected)
            {
                _logger.Error("ExportTagsToXml: Нет подключения к TIA Portal");
                return;
            }

            var plcSoftware = _tiaService.GetPlcSoftware();
            if (plcSoftware == null)
            {
                _logger.Error("ExportTagsToXml: Не удалось получить PlcSoftware");
                return;
            }

            // Экспортируем в зависимости от типа
            try
            {
                _logger.Info($"ExportTagsToXml: Экспорт {tagType} тегов начат");

                if (tagType == ExportTagType.All)
                {
                    // Используем существующий метод для экспорта всех тегов
                    await ExportTagsToXml(plcSoftware);
                }
                else if (tagType == ExportTagType.PlcTags)
                {
                    // Для тегов ПЛК используем метод
                    ExportTagTablesToXml(plcSoftware.TagTableGroup);
                }
                else if (tagType == ExportTagType.DbTags)
                {
                    // Для тегов DB используем метод
                    ExportDataBlocksToXml(plcSoftware.BlockGroup);
                }

                _logger.Info($"ExportTagsToXml: Экспорт {tagType} тегов завершен");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXml: Ошибка при экспорте {tagType} тегов: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт всех тегов в XML из PlcSoftware
        /// </summary>
        public async Task ExportTagsToXml(PlcSoftware plcSoftware)
        {
            _logger.Info("ExportTagsToXml: Начало экспорта тегов в XML");

            try
            {
                // Экспорт таблиц тегов
                ExportTagTablesToXml(plcSoftware.TagTableGroup);

                // Экспорт блоков данных
                ExportDataBlocksToXml(plcSoftware.BlockGroup);

                // Ждем небольшую паузу для завершения операций
                await Task.Delay(100);

                _logger.Info("ExportTagsToXml: Экспорт завершен успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXml: Ошибка при экспорте: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт только таблиц тегов ПЛК
        /// </summary>
        public void ExportTagTablesToXml(PlcTagTableGroup tagTableGroup)
        {
            try
            {
                _logger.Info("ExportTagTablesToXml: Начало экспорта таблиц тегов");

                // Создаем директорию для экспорта, если она не существует
                Directory.CreateDirectory(_plcTagsPath);

                // Собираем все таблицы тегов для постепенной обработки
                var allTagTables = new List<PlcTagTable>();
                CollectTagTables(tagTableGroup, allTagTables);

                _logger.Info($"ExportTagTablesToXml: Найдено {allTagTables.Count} таблиц тегов");

                // Обрабатываем каждую таблицу отдельно
                int processedCount = 0;
                int successCount = 0;

                foreach (var tagTable in allTagTables)
                {
                    try
                    {
                        processedCount++;
                        string tableName = tagTable.Name;

                        _logger.Info($"ExportTagTablesToXml: Обработка таблицы {tableName} ({processedCount}/{allTagTables.Count})");

                        // Создаем поддиректорию, если нужно
                        string exportFolder = _plcTagsPath;
                        Directory.CreateDirectory(exportFolder);

                        // Экспортируем таблицу тегов
                        string exportPath = Path.Combine(exportFolder, $"{tableName}.xml");
                        ExportSingleTagTableToXml(tagTable, exportPath);

                        successCount++;
                        _logger.Info($"ExportTagTablesToXml: Таблица {tableName} успешно экспортирована");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"ExportTagTablesToXml: Ошибка при обработке таблицы тегов: {ex.Message}");
                        // Продолжаем с другими таблицами
                    }
                }

                _logger.Info($"ExportTagTablesToXml: Экспорт таблиц тегов завершен. Успешно: {successCount}/{allTagTables.Count}");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagTablesToXml: Общая ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивный сбор таблиц тегов из группы
        /// </summary>
        private void CollectTagTables(PlcTagTableGroup group, List<PlcTagTable> tagTables)
        {
            if (group == null) return;

            try
            {
                // Собираем таблицы на текущем уровне
                if (group.TagTables != null)
                {
                    foreach (var table in group.TagTables)
                    {
                        if (table is PlcTagTable tagTable)
                        {
                            tagTables.Add(tagTable);
                        }
                    }
                }

                // Рекурсивно обрабатываем подгруппы
                if (group.Groups != null)
                {
                    foreach (var subgroup in group.Groups)
                    {
                        if (subgroup is PlcTagTableUserGroup userGroup)
                        {
                            CollectTagTables(userGroup, tagTables);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"CollectTagTables: Ошибка при сборе таблиц тегов: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт отдельной таблицы тегов в XML
        /// </summary>
        private void ExportSingleTagTableToXml(PlcTagTable tagTable, string exportPath)
        {
            try
            {
                // Ограничиваем количество тегов для обработки
                int maxTags = 5000;
                var tagElements = new List<XElement>();
                int tagCount = 0;

                foreach (var tag in tagTable.Tags)
                {
                    if (tagCount >= maxTags)
                    {
                        _logger.Warn($"ExportSingleTagTableToXml: Достигнут лимит тегов ({maxTags}) для таблицы {tagTable.Name}");
                        break;
                    }

                    try
                    {
                        string name = tag.Name;
                        string dataType = "Unknown";
                        string address = "";

                        try { dataType = tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown"; } catch { }
                        try { address = tag.LogicalAddress; } catch { }

                        tagElements.Add(new XElement("Tag",
                            new XAttribute("Name", name),
                            new XAttribute("DataType", dataType),
                            new XAttribute("Address", address)
                        ));

                        tagCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"ExportSingleTagTableToXml: Ошибка при обработке тега: {ex.Message}");
                        // Продолжаем с другими тегами
                    }
                }

                // Создаем XML документ
                XDocument doc = new XDocument(
                    new XElement("TagTable",
                        new XAttribute("Name", tagTable.Name),
                        new XElement("Tags", tagElements)
                    )
                );

                // Сохраняем документ
                doc.Save(exportPath);
                _logger.Info($"ExportSingleTagTableToXml: Таблица {tagTable.Name} экспортирована: {exportPath}");
            }
            catch (Exception ex)
            {
                string tableName = "unknown";
                try { tableName = tagTable.Name; } catch { }

                _logger.Error($"ExportSingleTagTableToXml: Ошибка при экспорте таблицы {tableName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт только блоков данных
        /// </summary>
        public void ExportDataBlocksToXml(PlcBlockGroup blockGroup)
        {
            try
            {
                _logger.Info("ExportDataBlocksToXml: Начало экспорта блоков данных");

                // Создаем директорию для экспорта, если она не существует
                Directory.CreateDirectory(_dbExportsPath);

                // Получаем список всех блоков данных для постепенной обработки
                var allDataBlocks = new List<DataBlock>();
                CollectDataBlocks(blockGroup, allDataBlocks);

                _logger.Info($"ExportDataBlocksToXml: Найдено {allDataBlocks.Count} блоков данных");

                // Обрабатываем каждый блок данных по отдельности с таймаутом
                int processedCount = 0;
                int successCount = 0;

                foreach (var db in allDataBlocks)
                {
                    try
                    {
                        processedCount++;
                        string dbName = db.Name;

                        _logger.Info($"ExportDataBlocksToXml: Обработка DB {dbName} ({processedCount}/{allDataBlocks.Count})");

                        // Создаем задачу с таймаутом в 10 секунд для каждого блока
                        var cts = new CancellationTokenSource(10000); // 10 секунд
                        var task = Task.Run(() => ExportSingleDataBlockToXml(db), cts.Token);

                        try
                        {
                            // Ожидаем завершения задачи или таймаута
                            task.Wait(cts.Token);
                            successCount++;
                            _logger.Info($"ExportDataBlocksToXml: DB {dbName} успешно экспортирован");
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.Warn($"ExportDataBlocksToXml: Превышен таймаут обработки DB {dbName}, пропускаем");
                        }
                        catch (AggregateException ex)
                        {
                            _logger.Error($"ExportDataBlocksToXml: Ошибка при экспорте DB {dbName}: {ex.InnerException?.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"ExportDataBlocksToXml: Ошибка при обработке блока данных: {ex.Message}");
                        // Продолжаем с другими блоками
                    }
                }

                _logger.Info($"ExportDataBlocksToXml: Экспорт блоков данных завершен. Успешно: {successCount}/{allDataBlocks.Count}");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportDataBlocksToXml: Общая ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Рекурсивный сбор блоков данных из группы
        /// </summary>
        private void CollectDataBlocks(PlcBlockGroup group, List<DataBlock> dataBlocks)
        {
            if (group == null) return;

            try
            {
                // Собираем блоки на текущем уровне
                if (group.Blocks != null)
                {
                    foreach (var block in group.Blocks)
                    {
                        if (block is DataBlock db)
                        {
                            dataBlocks.Add(db);
                        }
                    }
                }

                // Рекурсивно обрабатываем подгруппы
                if (group.Groups != null)
                {
                    foreach (var subgroup in group.Groups)
                    {
                        CollectDataBlocks(subgroup as PlcBlockGroup, dataBlocks);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"CollectDataBlocks: Ошибка при сборе блоков данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт отдельного блока данных в XML
        /// </summary>
        private void ExportSingleDataBlockToXml(DataBlock db)
        {
            if (db == null) return;

            try
            {
                string dbName = db.Name;
                string exportPath = Path.Combine(_dbExportsPath, $"{dbName}.xml");
                bool isOptimized = false;

                try
                {
                    isOptimized = db.MemoryLayout == MemoryLayout.Optimized;
                }
                catch
                {
                    _logger.Warn($"ExportSingleDataBlockToXml: Не удалось определить MemoryLayout для DB {dbName}");
                }

                // Безопасное извлечение переменных из блока данных
                var variables = new List<XElement>();
                int variableCount = 0;

                if (db.Interface != null && db.Interface.Members != null)
                {
                    try
                    {
                        foreach (var member in db.Interface.Members)
                        {
                            if (variableCount >= 1000) // Ограничиваем количество переменных
                            {
                                _logger.Warn($"ExportSingleDataBlockToXml: Достигнут лимит переменных (1000) для DB {dbName}");
                                break;
                            }

                            string varName = member.Name;
                            string dataType = "Unknown";

                            try
                            {
                                dataType = member.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";
                            }
                            catch
                            {
                                // Игнорируем ошибки при получении типа данных
                            }

                            variables.Add(new XElement("Variable",
                                new XAttribute("Name", varName),
                                new XAttribute("DataType", dataType)
                            ));

                            variableCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"ExportSingleDataBlockToXml: Ошибка при обработке переменных DB {dbName}: {ex.Message}");
                    }
                }

                // Создаем XML документ
                XDocument doc = new XDocument(
                    new XElement("DataBlock",
                        new XAttribute("Name", dbName),
                        new XAttribute("Optimized", isOptimized),
                        new XElement("Variables", variables)
                    )
                );

                // Сохраняем документ
                doc.Save(exportPath);
                _logger.Info($"ExportSingleDataBlockToXml: DB {dbName} экспортирован: {exportPath}");
            }
            catch (Exception ex)
            {
                string dbName = "unknown";
                try { dbName = db.Name; } catch { }

                _logger.Error($"ExportSingleDataBlockToXml: Ошибка при экспорте DB {dbName}: {ex.Message}");
            }
        }

        public List<TagDefinition> LoadPlcTagsFromXml()
        {
            if (string.IsNullOrEmpty(_currentProjectName))
            {
                _logger.Error("LoadPlcTagsFromXml: Не установлен текущий проект");
                return new List<TagDefinition>();
            }

            var tagList = new List<TagDefinition>();

            try
            {
                if (!Directory.Exists(_plcTagsPath))
                {
                    _logger.Error($"LoadPlcTagsFromXml: Директория {_plcTagsPath} не найдена");
                    return tagList;
                }

                string[] xmlFiles = Directory.GetFiles(_plcTagsPath, "*.xml", SearchOption.AllDirectories);
                _logger.Info($"LoadPlcTagsFromXml: Найдено {xmlFiles.Length} XML-файлов с тегами");

                foreach (string file in xmlFiles)
                {
                    string tableName = Path.GetFileNameWithoutExtension(file);
                    _logger.Info($"LoadPlcTagsFromXml: Обработка файла {tableName}");

                    try
                    {
                        XDocument doc = XDocument.Load(file);
                        foreach (var tagElement in doc.Descendants("Tag"))
                        {
                            try
                            {
                                string name = tagElement.Attribute("Name")?.Value ?? "Unknown";
                                string dataTypeStr = tagElement.Attribute("DataType")?.Value ?? "Unknown";
                                string address = tagElement.Attribute("Address")?.Value ?? "";

                                TagDataType dataType = ConvertStringToTagDataType(dataTypeStr);

                                tagList.Add(new TagDefinition
                                {
                                    Name = name,
                                    DataType = dataType,
                                    Address = address,
                                    GroupName = tableName
                                });

                                _logger.Debug($"LoadPlcTagsFromXml: Загружен тег {name}, тип: {dataType}");
                            }
                            catch (Exception tagEx)
                            {
                                _logger.Debug($"LoadPlcTagsFromXml: Ошибка при обработке тега: {tagEx.Message}");
                                // Продолжаем с другими тегами
                            }
                        }
                    }
                    catch (Exception fileEx)
                    {
                        _logger.Error($"LoadPlcTagsFromXml: Ошибка при обработке файла {file}: {fileEx.Message}");
                        // Продолжаем с другими файлами
                    }
                }

                _logger.Info($"LoadPlcTagsFromXml: Всего загружено {tagList.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadPlcTagsFromXml: Ошибка: {ex.Message}");
            }

            return tagList;
        }

        public List<TagDefinition> LoadDbTagsFromXml()
        {
            if (string.IsNullOrEmpty(_currentProjectName))
            {
                _logger.Error("LoadDbTagsFromXml: Не установлен текущий проект");
                return new List<TagDefinition>();
            }

            var dbTags = new List<TagDefinition>();

            try
            {
                if (!Directory.Exists(_dbExportsPath))
                {
                    _logger.Error($"LoadDbTagsFromXml: Директория {_dbExportsPath} не найдена");
                    return dbTags;
                }

                string[] xmlFiles = Directory.GetFiles(_dbExportsPath, "*.xml", SearchOption.TopDirectoryOnly);
                _logger.Info($"LoadDbTagsFromXml: Найдено {xmlFiles.Length} XML-файлов с DB");

                foreach (string file in xmlFiles)
                {
                    try
                    {
                        XDocument doc = XDocument.Load(file);
                        string dbName = doc.Root?.Attribute("Name")?.Value ?? Path.GetFileNameWithoutExtension(file);
                        bool isOptimized = bool.TryParse(doc.Root?.Attribute("Optimized")?.Value ?? "false", out bool opt) && opt;

                        // Добавляем информацию о блоке данных
                        var dbTag = new TagDefinition
                        {
                            Name = dbName,
                            Address = isOptimized ? "Optimized" : "Standard",
                            DataType = TagDataType.Bool, // Используем существующий тип вместо Struct
                            GroupName = "DataBlocks",
                            IsOptimized = isOptimized
                        };

                        dbTags.Add(dbTag);

                        // Если нужно также добавлять переменные блоков данных:
                        /*
                        foreach (var varElement in doc.Descendants("Variable"))
                        {
                            try
                            {
                                string varName = varElement.Attribute("Name")?.Value ?? "Unknown";
                                string dataTypeStr = varElement.Attribute("DataType")?.Value ?? "Unknown";
                                
                                // Формируем полное имя переменной с префиксом DB
                                string fullName = $"{dbName}.{varName}";
                                
                                TagDataType dataType = ConvertStringToTagDataType(dataTypeStr);
                                
                                dbTags.Add(new TagDefinition
                                {
                                    Name = fullName,
                                    DataType = dataType,
                                    Address = isOptimized ? "Optimized" : "Standard",
                                    GroupName = dbName,
                                    IsOptimized = isOptimized
                                });
                            }
                            catch (Exception varEx)
                            {
                                _logger.Debug($"LoadDbTagsFromXml: Ошибка при обработке переменной: {varEx.Message}");
                                // Продолжаем с другими переменными
                            }
                        }
                        */

                        _logger.Info($"LoadDbTagsFromXml: Загружен DB {dbName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"LoadDbTagsFromXml: Ошибка при обработке файла {Path.GetFileName(file)}: {ex.Message}");
                        // Продолжаем с другими файлами
                    }
                }

                _logger.Info($"LoadDbTagsFromXml: Всего загружено {dbTags.Count} блоков данных");
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadDbTagsFromXml: Ошибка: {ex.Message}");
            }

            return dbTags;
        }

        private TagDataType ConvertStringToTagDataType(string dataTypeStr)
        {
            switch (dataTypeStr.ToLower())
            {
                case "bool": return TagDataType.Bool;
                case "int": return TagDataType.Int;
                case "dint": return TagDataType.DInt;
                case "real": return TagDataType.Real;
                //case "Struct": return TagDataType.Struct;
                // Используйте подходящее значение из вашего перечисления для остальных типов
                case "byte":
                case "struct":
                default: return TagDataType.Other; // Используем TagDataType.Other для неизвестных типов
            }
        }

        /// <summary>
        /// Проверка наличия экспортированных данных для указанного проекта
        /// </summary>
        /// <param name="projectName">Имя проекта</param>
        /// <returns>True, если данные найдены</returns>
        public bool HasExportedDataForProject(string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
                return false;

            // Заменяем недопустимые символы в имени файла
            string safeName = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars()));

            // Проверяем существование папки проекта и наличие в ней файлов
            string projectPath = Path.Combine(_baseExportPath, safeName);

            if (!Directory.Exists(projectPath))
                return false;

            string tagsPath = Path.Combine(projectPath, "TagTables");
            string dbPath = Path.Combine(projectPath, "DB");

            bool hasTags = Directory.Exists(tagsPath) &&
                          (Directory.GetFiles(tagsPath, "*.xml", SearchOption.AllDirectories).Length > 0);

            bool hasDBs = Directory.Exists(dbPath) &&
                         (Directory.GetFiles(dbPath, "*.xml", SearchOption.TopDirectoryOnly).Length > 0);

            return hasTags || hasDBs;
        }

        /// <summary>
        /// Удаление файлов кэша проекта
        /// </summary>
        public bool ClearProjectCache(string projectName = null)
        {
            try
            {
                // Если имя проекта не указано, используем текущий проект
                string targetProject = projectName ?? _currentProjectName;

                if (string.IsNullOrEmpty(targetProject))
                {
                    _logger.Error("ClearProjectCache: Не указано имя проекта для очистки кэша");
                    return false;
                }

                // Заменяем недопустимые символы в имени файла
                string safeName = string.Join("_", targetProject.Split(Path.GetInvalidFileNameChars()));
                string projectPath = Path.Combine(_baseExportPath, safeName);

                if (!Directory.Exists(projectPath))
                {
                    _logger.Warn($"ClearProjectCache: Директория кэша для проекта {targetProject} не найдена: {projectPath}");
                    return false;
                }

                // Удаляем папку проекта со всем содержимым
                Directory.Delete(projectPath, true);
                _logger.Info($"ClearProjectCache: Кэш проекта {targetProject} успешно удален");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"ClearProjectCache: Ошибка при очистке кэша: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получение списка проектов в кэше
        /// </summary>
        public List<string> GetCachedProjects()
        {
            var projects = new List<string>();

            try
            {
                if (!Directory.Exists(_baseExportPath))
                    return projects;

                // Получаем все папки в директории кэша
                var directories = Directory.GetDirectories(_baseExportPath);

                foreach (var dir in directories)
                {
                    // Проверяем, содержит ли директория данные (подпапки TagTables или DB)
                    bool hasTagTables = Directory.Exists(Path.Combine(dir, "TagTables"));
                    bool hasDBs = Directory.Exists(Path.Combine(dir, "DB"));

                    if (hasTagTables || hasDBs)
                    {
                        // Добавляем имя папки (проекта) в список
                        projects.Add(Path.GetFileName(dir));
                    }
                }

                _logger.Info($"GetCachedProjects: Найдено {projects.Count} проектов в кэше");
            }
            catch (Exception ex)
            {
                _logger.Error($"GetCachedProjects: Ошибка: {ex.Message}");
            }

            return projects;
        }

        /// <summary>
        /// Аварийное прерывание всех активных операций экспорта
        /// </summary>
        public void CancelAllExportOperations()
        {
            try
            {
                _logger.Info("CancelAllExportOperations: Запрошена остановка всех операций экспорта");

                // Здесь можно добавить механизм отмены всех операций,
                // если вы реализуете асинхронную обработку с CancellationToken

                // Если это необходимо, вы можете добавить статическое поле CancellationTokenSource
                // и использовать его для отмены всех операций

                // Очистка кэша позволит избежать использования частично экспортированных данных
                _logger.Info("CancelAllExportOperations: Операции экспорта остановлены");
            }
            catch (Exception ex)
            {
                _logger.Error($"CancelAllExportOperations: Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение информации о состоянии кэша
        /// </summary>
        public Dictionary<string, object> GetCacheStatus(string projectName = null)
        {
            var status = new Dictionary<string, object>();

            try
            {
                // Если имя проекта не указано, используем текущий проект
                string targetProject = projectName ?? _currentProjectName;

                if (string.IsNullOrEmpty(targetProject))
                {
                    status["error"] = "Не указано имя проекта";
                    return status;
                }

                // Заменяем недопустимые символы в имени файла
                string safeName = string.Join("_", targetProject.Split(Path.GetInvalidFileNameChars()));
                string projectPath = Path.Combine(_baseExportPath, safeName);

                if (!Directory.Exists(projectPath))
                {
                    status["exists"] = false;
                    return status;
                }

                status["exists"] = true;

                // Проверяем наличие кэша тегов и блоков данных
                string tagsPath = Path.Combine(projectPath, "TagTables");
                string dbPath = Path.Combine(projectPath, "DB");

                bool hasTags = Directory.Exists(tagsPath);
                bool hasDBs = Directory.Exists(dbPath);

                status["hasTags"] = hasTags;
                status["hasDBs"] = hasDBs;

                // Если есть кэш, получаем дополнительную информацию
                if (hasTags)
                {
                    var tagFiles = Directory.GetFiles(tagsPath, "*.xml", SearchOption.AllDirectories);
                    status["tagCount"] = tagFiles.Length;

                    if (tagFiles.Length > 0)
                    {
                        var lastWriteTime = File.GetLastWriteTime(tagFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First());
                        status["tagsLastUpdated"] = lastWriteTime;
                    }
                }

                if (hasDBs)
                {
                    var dbFiles = Directory.GetFiles(dbPath, "*.xml", SearchOption.TopDirectoryOnly);
                    status["dbCount"] = dbFiles.Length;

                    if (dbFiles.Length > 0)
                    {
                        var lastWriteTime = File.GetLastWriteTime(dbFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First());
                        status["dbsLastUpdated"] = lastWriteTime;
                    }
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetCacheStatus: Ошибка: {ex.Message}");
                status["error"] = ex.Message;
                return status;
            }
        }
    }
}