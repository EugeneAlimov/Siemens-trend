////using SiemensTrend.Models;
////using SiemensTrend.Services;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Xml.Linq;
//using Siemens.Engineering.SW.Blocks;
//using Siemens.Engineering.SW.Tags;
//using Siemens.Engineering.SW;
////using Siemens.Collaboration.Net.Logging;
//using SiemensTrend.Core.Models;
//using SiemensTrend.Core.Logging;

//namespace SiemensTrend.Helpers
//{
//    public class TiaPortalXmlManager
//    {
//        private readonly Logger _logger;
//        private readonly string _plcTagsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TiaExports", "TagTables");
//        private readonly string _dbExportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TiaExports", "DB");

//        public TiaPortalXmlManager(Logger logger)
//        {
//            _logger = logger;
//            Directory.CreateDirectory(_plcTagsPath);
//            Directory.CreateDirectory(_dbExportsPath);
//            _logger.Info($"TiaPortalXmlManager: Директории для экспорта созданы: {_plcTagsPath}, {_dbExportsPath}");
//        }

//        public async Task ExportTagsToXml(PlcSoftware plcSoftware)
//        {
//            _logger.Info("ExportTagsToXml: Начало экспорта тегов в XML");

//            try
//            {
//                // Экспорт таблиц тегов
//                await ExportTagTables(plcSoftware.TagTableGroup, _plcTagsPath);

//                // Экспорт блоков данных
//                await ExportDataBlocks(plcSoftware.BlockGroup);

//                _logger.Info("ExportTagsToXml: Экспорт завершен успешно");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"ExportTagsToXml: Ошибка при экспорте: {ex.Message}");
//            }
//        }

//        private async Task ExportTagTables(PlcTagTableGroup tagGroup, string exportFolder)
//        {
//            if (tagGroup == null) return;

//            foreach (PlcTagTable tagTable in tagGroup.TagTables)
//            {
//                await ExportTagTable(tagTable, exportFolder);
//            }

//            foreach (PlcTagTableUserGroup subgroup in tagGroup.Groups)
//            {
//                string subgroupPath = Path.Combine(exportFolder, subgroup.Name);
//                Directory.CreateDirectory(subgroupPath);
//                await ExportTagTablesUserGroup(subgroup, subgroupPath);
//            }
//        }

//        private async Task ExportTagTablesUserGroup(PlcTagTableUserGroup userGroup, string exportFolder)
//        {
//            foreach (PlcTagTable tagTable in userGroup.TagTables)
//            {
//                await ExportTagTable(tagTable, exportFolder);
//            }

//            foreach (PlcTagTableUserGroup subgroup in userGroup.Groups)
//            {
//                string subgroupPath = Path.Combine(exportFolder, subgroup.Name);
//                Directory.CreateDirectory(subgroupPath);
//                await ExportTagTablesUserGroup(subgroup, subgroupPath);
//            }
//        }

//        private async Task ExportTagTable(PlcTagTable tagTable, string exportFolder)
//        {
//            try
//            {
//                string exportPath = Path.Combine(exportFolder, $"{tagTable.Name}.xml");

//                XDocument doc = new XDocument(
//                    new XElement("TagTable",
//                        new XAttribute("Name", tagTable.Name),
//                        new XElement("Tags",
//                            tagTable.Tags.Select(tag =>
//                                new XElement("Tag",
//                                    new XAttribute("Name", tag.Name),
//                                    new XAttribute("DataType", tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown"),
//                                    new XAttribute("Address", tag.GetAttribute("LogicalAddress")?.ToString() ?? "")
//                                )
//                            )
//                        )
//                    )
//                );

//                await Task.Run(() => doc.Save(exportPath));
//                _logger.Info($"ExportTagTable: Таблица {tagTable.Name} экспортирована: {exportPath}");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"ExportTagTable: Ошибка при экспорте {tagTable.Name}: {ex.Message}");
//            }
//        }

//        private async Task ExportDataBlocks(PlcBlockGroup blockGroup)
//        {
//            try
//            {
//                foreach (PlcBlock block in blockGroup.Blocks)
//                {
//                    if (block is DataBlock db)
//                    {
//                        await ExportDataBlock(db);
//                    }
//                }

//                // Не делаем глубокий рекурсивный обход, только первый уровень
//                foreach (PlcBlockGroup subgroup in blockGroup.Groups)
//                {
//                    foreach (PlcBlock block in subgroup.Blocks)
//                    {
//                        if (block is DataBlock db)
//                        {
//                            await ExportDataBlock(db);
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"ExportDataBlocks: Ошибка: {ex.Message}");
//            }
//        }

//        private async Task ExportDataBlock(DataBlock db)
//        {
//            try
//            {
//                string exportPath = Path.Combine(_dbExportsPath, $"{db.Name}.xml");
//                bool isOptimized = db.MemoryLayout == MemoryLayout.Optimized;

//                // Безопасный доступ к членам блока данных
//                var dbMembers = new List<XElement>();

//                if (db.Interface != null && db.Interface.Members != null)
//                {
//                    foreach (var member in db.Interface.Members)
//                    {
//                        try
//                        {
//                            string name = member.Name;
//                            string dataType = "Unknown";

//                            try
//                            {
//                                dataType = member.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";
//                            }
//                            catch
//                            {
//                                // Игнорируем ошибки при получении типа данных
//                            }

//                            dbMembers.Add(new XElement("Variable",
//                                new XAttribute("Name", name),
//                                new XAttribute("DataType", dataType)
//                            ));
//                        }
//                        catch
//                        {
//                            // Игнорируем ошибки при обработке отдельных членов
//                        }
//                    }
//                }

//                XDocument doc = new XDocument(
//                    new XElement("DataBlock",
//                        new XAttribute("Name", db.Name),
//                        new XAttribute("Optimized", isOptimized),
//                        new XElement("Variables", dbMembers)
//                    )
//                );

//                await Task.Run(() => doc.Save(exportPath));
//                _logger.Info($"ExportDataBlock: DB {db.Name} экспортирован: {exportPath}");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"ExportDataBlock: Ошибка при экспорте DB {db.Name}: {ex.Message}");
//            }
//        }

//        public List<TagDefinition> LoadPlcTagsFromXml()
//        {
//            var tagList = new List<TagDefinition>();

//            try
//            {
//                if (!Directory.Exists(_plcTagsPath))
//                {
//                    _logger.Error($"LoadPlcTagsFromXml: Директория {_plcTagsPath} не найдена");
//                    return tagList;
//                }

//                string[] xmlFiles = Directory.GetFiles(_plcTagsPath, "*.xml", SearchOption.AllDirectories);
//                _logger.Info($"LoadPlcTagsFromXml: Найдено {xmlFiles.Length} XML-файлов с тегами");

//                foreach (string file in xmlFiles)
//                {
//                    string tableName = Path.GetFileNameWithoutExtension(file);
//                    _logger.Info($"LoadPlcTagsFromXml: Обработка файла {tableName}");

//                    XDocument doc = XDocument.Load(file);
//                    foreach (var tagElement in doc.Descendants("Tag"))
//                    {
//                        string name = tagElement.Attribute("Name")?.Value ?? "Unknown";
//                        string dataTypeStr = tagElement.Attribute("DataType")?.Value ?? "Unknown";
//                        string address = tagElement.Attribute("Address")?.Value ?? "";

//                        TagDataType dataType = ConvertStringToTagDataType(dataTypeStr);

//                        tagList.Add(new TagDefinition
//                        {
//                            Name = name,
//                            DataType = dataType,
//                            Address = address,
//                            GroupName = tableName
//                        });

//                        _logger.Debug($"LoadPlcTagsFromXml: Загружен тег {name}, тип: {dataType}");
//                    }
//                }

//                _logger.Info($"LoadPlcTagsFromXml: Всего загружено {tagList.Count} тегов");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"LoadPlcTagsFromXml: Ошибка: {ex.Message}");
//            }

//            return tagList;
//        }

//        public List<TagDefinition> LoadDbTagsFromXml()
//        {
//            var dbTags = new List<TagDefinition>();

//            try
//            {
//                if (!Directory.Exists(_dbExportsPath))
//                {
//                    _logger.Error($"LoadDbTagsFromXml: Директория {_dbExportsPath} не найдена");
//                    return dbTags;
//                }

//                string[] xmlFiles = Directory.GetFiles(_dbExportsPath, "*.xml", SearchOption.TopDirectoryOnly);
//                _logger.Info($"LoadDbTagsFromXml: Найдено {xmlFiles.Length} XML-файлов с DB");

//                foreach (string file in xmlFiles)
//                {
//                    try
//                    {
//                        XDocument doc = XDocument.Load(file);
//                        string dbName = doc.Root?.Attribute("Name")?.Value ?? Path.GetFileNameWithoutExtension(file);
//                        bool isOptimized = bool.TryParse(doc.Root?.Attribute("Optimized")?.Value ?? "false", out bool opt) && opt;

//                        dbTags.Add(new TagDefinition
//                        {
//                            Name = dbName,
//                            Address = isOptimized ? "Optimized" : "Standard",
//                            DataType = TagDataType.Bool, // Используем существующий тип вместо Struct
//                            GroupName = "DataBlocks",
//                            IsOptimized = isOptimized
//                        });

//                        // Если нужны переменные блоков данных, их можно добавить тут
//                        // foreach (var varElement in doc.Descendants("Variable")) { ... }

//                        _logger.Info($"LoadDbTagsFromXml: Загружен DB {dbName}");
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"LoadDbTagsFromXml: Ошибка при обработке файла {Path.GetFileName(file)}: {ex.Message}");
//                    }
//                }

//                _logger.Info($"LoadDbTagsFromXml: Всего загружено {dbTags.Count} блоков данных");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"LoadDbTagsFromXml: Ошибка: {ex.Message}");
//            }

//            return dbTags;
//        }

//        private TagDataType ConvertStringToTagDataType(string dataTypeStr)
//        {
//            switch (dataTypeStr.ToLower())
//            {
//                case "bool": return TagDataType.Bool;
//                case "int": return TagDataType.Int;
//                case "dint": return TagDataType.DInt;
//                case "real": return TagDataType.Real;
//                //case "Struct": return TagDataType.Struct;
//                // Используйте подходящее значение из вашего перечисления для остальных using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW;
using SiemensTrend.Core.Models;
using SiemensTrend.Core.Logging;
using System;
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
        private bool IsConnected { get; set; }
        private object _project { get; set; }

        private readonly TiaPortalXmlManager _xmlManager;
        /// <summary>
        /// Улучшенный конструктор с возможностью указать текущий проект
        /// </summary>
        public TiaPortalXmlManager(Logger logger, string currentProjectName = null)
        {
            _logger = logger;
            _baseExportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TiaExports");
            Directory.CreateDirectory(_baseExportPath);
            _logger.Info($"TiaPortalXmlManager: Базовая директория для экспорта создана: {_baseExportPath}");

                // Initialize the _xmlManager field
            _xmlManager = this;

            IsConnected = false; // or true, depending on your logic
            _project = null; // or assign the actual project object

            // Если указано имя проекта, устанавливаем его
            if (!string.IsNullOrEmpty(currentProjectName))
            {
                SetCurrentProject(currentProjectName);
            }
        }
        private void SetCurrentProjectInXmlManager()
        {
            if (!string.IsNullOrEmpty(_currentProjectName))
            {
                SetCurrentProject(_currentProjectName);
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
            if (!IsConnected || _project == null)
            {
                _logger.Error("ExportTagsToXml: Нет подключения к TIA Portal");
                return;
            }

            var plcSoftware = GetPlcSoftware();
            if (plcSoftware == null) return;

            // Сначала проверяем, настроен ли XML-менеджер для текущего проекта
            if (_project is Project project && !string.IsNullOrEmpty(project.Name))
            {
                SetCurrentProjectInXmlManager();
            }

            // Экспортируем в зависимости от типа
            try
            {
                _logger.Info($"ExportTagsToXml: Экспорт {tagType} тегов начат");

                if (tagType == ExportTagType.All)
                {
                    // Используем существующий метод для экспорта всех тегов
                    await _xmlManager.ExportTagsToXml(plcSoftware);
                }
                else if (tagType == ExportTagType.PlcTags)
                {
                    // Для тегов ПЛК используем новый метод
                    _xmlManager.ExportTagTablesToXml(plcSoftware.TagTableGroup);
                }
                else if (tagType == ExportTagType.DbTags)
                {
                    // Для тегов DB используем новый метод
                    _xmlManager.ExportDataBlocksToXml(plcSoftware.BlockGroup);
                }

                _logger.Info($"ExportTagsToXml: Экспорт {tagType} тегов завершен");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXml: Ошибка при экспорте {tagType} тегов: {ex.Message}");
            }
        }
        public async Task ExportTagTables(PlcTagTableGroup tagGroup, string exportFolder)
        {
            if (tagGroup == null) return;

            foreach (PlcTagTable tagTable in tagGroup.TagTables)
            {
                await ExportTagTables(tagGroup, _plcTagsPath);
            }

            foreach (PlcTagTableUserGroup subgroup in tagGroup.Groups)
            {
                string subgroupPath = Path.Combine(exportFolder, subgroup.Name);
                Directory.CreateDirectory(subgroupPath);
                await ExportTagTablesUserGroup(subgroup, subgroupPath);
            }
        }

        private async Task ExportTagTablesUserGroup(PlcTagTableUserGroup userGroup, string exportFolder)
        {
            foreach (PlcTagTable tagTable in userGroup.TagTables)
            {
                await ExportTagTable(tagTable, exportFolder);
            }

            foreach (PlcTagTableUserGroup subgroup in userGroup.Groups)
            {
                string subgroupPath = Path.Combine(exportFolder, subgroup.Name);
                Directory.CreateDirectory(subgroupPath);
                await ExportTagTablesUserGroup(subgroup, subgroupPath);
            }
        }

        private async Task ExportTagTable(PlcTagTable tagTable, string exportFolder)
        {
            try
            {
                string exportPath = Path.Combine(exportFolder, $"{tagTable.Name}.xml");

                XDocument doc = new XDocument(
                    new XElement("TagTable",
                        new XAttribute("Name", tagTable.Name),
                        new XElement("Tags",
                            tagTable.Tags.Select(tag =>
                                new XElement("Tag",
                                    new XAttribute("Name", tag.Name),
                                    new XAttribute("DataType", tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown"),
                                    new XAttribute("Address", tag.GetAttribute("LogicalAddress")?.ToString() ?? "")
                                )
                            )
                        )
                    )
                );

                await Task.Run(() => doc.Save(exportPath));
                _logger.Info($"ExportTagTable: Таблица {tagTable.Name} экспортирована: {exportPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagTable: Ошибка при экспорте {tagTable.Name}: {ex.Message}");
            }
        }

        public async Task ExportDataBlocks(PlcBlockGroup blockGroup)
        {
            try
            {
                foreach (PlcBlock block in blockGroup.Blocks)
                {
                    if (block is DataBlock db)
                    {
                        await ExportDataBlock(db);
                    }
                }

                // Не делаем глубокий рекурсивный обход, только первый уровень
                foreach (PlcBlockGroup subgroup in blockGroup.Groups)
                {
                    foreach (PlcBlock block in subgroup.Blocks)
                    {
                        if (block is DataBlock db)
                        {
                            await ExportDataBlock(db);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportDataBlocks: Ошибка: {ex.Message}");
            }
        }

        private async Task ExportDataBlock(DataBlock db)
        {
            try
            {
                string exportPath = Path.Combine(_dbExportsPath, $"{db.Name}.xml");
                bool isOptimized = db.MemoryLayout == MemoryLayout.Optimized;

                // Безопасный доступ к членам блока данных
                var dbMembers = new List<XElement>();

                if (db.Interface != null && db.Interface.Members != null)
                {
                    foreach (var member in db.Interface.Members)
                    {
                        try
                        {
                            string name = member.Name;
                            string dataType = "Unknown";

                            try
                            {
                                dataType = member.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";
                            }
                            catch
                            {
                                // Игнорируем ошибки при получении типа данных
                            }

                            dbMembers.Add(new XElement("Variable",
                                new XAttribute("Name", name),
                                new XAttribute("DataType", dataType)
                            ));
                        }
                        catch
                        {
                            // Игнорируем ошибки при обработке отдельных членов
                        }
                    }
                }

                XDocument doc = new XDocument(
                    new XElement("DataBlock",
                        new XAttribute("Name", db.Name),
                        new XAttribute("Optimized", isOptimized),
                        new XElement("Variables", dbMembers)
                    )
                );

                await Task.Run(() => doc.Save(exportPath));
                _logger.Info($"ExportDataBlock: DB {db.Name} экспортирован: {exportPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportDataBlock: Ошибка при экспорте DB {db.Name}: {ex.Message}");
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

                    XDocument doc = XDocument.Load(file);
                    foreach (var tagElement in doc.Descendants("Tag"))
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

                        dbTags.Add(new TagDefinition
                        {
                            Name = dbName,
                            Address = isOptimized ? "Optimized" : "Standard",
                            DataType = TagDataType.Bool, // Используем существующий тип вместо Struct
                            GroupName = "DataBlocks",
                            IsOptimized = isOptimized
                        });

                        // Если нужны переменные блоков данных, их можно добавить тут
                        // foreach (var varElement in doc.Descendants("Variable")) { ... }

                        _logger.Info($"LoadDbTagsFromXml: Загружен DB {dbName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"LoadDbTagsFromXml: Ошибка при обработке файла {Path.GetFileName(file)}: {ex.Message}");
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
                default: return TagDataType.Bool; // Временно используем Bool для неизвестных типов
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
        /// Добавьте этот метод в класс TiaPortalXmlManager для удаления файлов кэша проекта
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
        /// Экспорт только таблиц тегов ПЛК
        /// </summary>
        public void ExportTagTablesToXml(PlcTagTableGroup tagTableGroup)
        {
            try
            {
                _logger.Info("ExportTagTablesToXml: Начало экспорта таблиц тегов");
                ExportTagTables(tagTableGroup, _plcTagsPath).Wait();
                _logger.Info("ExportTagTablesToXml: Экспорт таблиц тегов завершен");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagTablesToXml: Ошибка: {ex.Message}");
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
                ExportDataBlocks(blockGroup).Wait();
                _logger.Info("ExportDataBlocksToXml: Экспорт блоков данных завершен");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportDataBlocksToXml: Ошибка: {ex.Message}");
            }
        }
    }
}


