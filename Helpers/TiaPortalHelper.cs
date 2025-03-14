//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Xml.Linq;
//using Siemens.Engineering;
//using Siemens.Engineering.SW;
//using Siemens.Engineering.SW.Blocks;
//using Siemens.Engineering.SW.Tags;
//using Siemens.Engineering.HW;
//using Siemens.Engineering.HW.Features;

//namespace Siemens_trend.Helpers
//{
//    public class TiaPortalHelper
//    {
//        private PlcData plcData = new PlcData();
//        public PlcData PlcData => plcData;
//        private Project? project;
//        private TiaPortal? tiaPortal;
//        private string logFilePath = "log.txt";

//        private readonly string plcTagsPath = @"C:\TIA_Exports\TagTables";
//        private readonly string dbExportsPath = @"C:\TIA_Exports\DB";

//        public TiaPortalHelper()
//        {
//            File.WriteAllText(logFilePath, ""); // Очищаем лог при старте
//            Log("🟢 Приложение запущено. Лог очищен.");
//        }

//        public bool AreTagsAvailable()
//        {
//            string[] tagFiles = Directory.Exists(plcTagsPath) ? Directory.GetFiles(plcTagsPath, "*.xml", SearchOption.AllDirectories) : new string[0];
//            string[] dbFiles = Directory.Exists(dbExportsPath) ? Directory.GetFiles(dbExportsPath, "*.xml", SearchOption.TopDirectoryOnly) : new string[0];

//            Log($"📂 Проверка тегов: {plcTagsPath} -> {tagFiles.Length} файлов");
//            Log($"📂 Проверка DB: {dbExportsPath} -> {dbFiles.Length} файлов");

//            return tagFiles.Length > 0 && dbFiles.Length > 0;
//        }

//        public void OpenProject(string projectPath)
//        {
//            try
//            {
//                Log($"🔍 Проверяем путь к проекту: {projectPath}");

//                if (!File.Exists(projectPath))
//                {
//                    Log($"❌ Файл проекта не найден: {projectPath}");
//                    return;
//                }

//                Log("🚀 Запускаем TIA Portal...");
//                tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface); // Пробуем UI

//                Log("📂 Открываем проект...");
//                project = tiaPortal.Projects.Open(new FileInfo(projectPath));

//                if (project != null)
//                {
//                    Log($"✅ Проект открыт: {project.Name}");
//                }
//                else
//                {
//                    Log("❌ Ошибка: Проект не открылся.");
//                }
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Ошибка при открытии проекта: {ex.Message}");
//            }
//        }


//        public void ExportProjectToXml(string projectPath)
//        {
//            OpenProject(projectPath);

//            if (project == null)
//            {
//                Log("❌ Проект TIA не открыт!");
//                return;
//            }

//            Directory.CreateDirectory(plcTagsPath);
//            Directory.CreateDirectory(dbExportsPath);

//            Log($"📡 Экспортируем проект {project.Name} в XML...");

//            foreach (Device device in project.Devices)
//            {
//                foreach (DeviceItem deviceItem in device.DeviceItems)
//                {
//                    var softwareContainer = deviceItem.GetService<SoftwareContainer>();
//                    if (softwareContainer?.Software is PlcSoftware plcSoftware)
//                    {
//                        Log($"✅ PLC найден: {device.Name}");
//                        LoadTagTables(plcSoftware.TagTableGroup, plcTagsPath);
//                        FindDataBlocksInGroup(plcSoftware.BlockGroup);
//                    }
//                }
//            }

//            Log("✅ Экспорт завершён. Теперь можно закрыть TIA Portal.");
//        }

//        private void FindDataBlocksInGroup(PlcBlockGroup group)
//        {
//            foreach (PlcBlock block in group.Blocks)
//            {
//                if (block is DataBlock db)
//                {
//                    Log($"🔍 Обнаружен DB: {db.Name}, MemoryLayout: {db.MemoryLayout}");

//                    string dbGroupPath = GetDbGroupPath(group);
//                    bool isUDT = dbGroupPath.Contains("PLC data types");

//                    List<PlcDbVariable> variables = ReadDb(db);

//                    if (!isUDT && variables.Any(v => v.DataType.StartsWith("UDT_") || v.DataType.Contains("type")))
//                    {
//                        isUDT = true;
//                    }

//                    bool isSafety = db.GetAttribute("ProgrammingLanguage")?.ToString() == "F_DB";

//                    PlcDb plcDb = new PlcDb
//                    {
//                        Name = db.Name,
//                        IsOptimized = db.MemoryLayout == MemoryLayout.Optimized,
//                        IsSafety = isSafety,
//                        IsUDT = isUDT,
//                        Variables = variables
//                    };

//                    plcData.DataBlocks.Add(plcDb);
//                    Log($"✅ DB {db.Name} обработан: {plcDb.Variables.Count} переменных. Safety: {isSafety}, UDT: {isUDT}, Path: {dbGroupPath}");

//                    ExportDbToXml(plcDb);
//                }
//            }

//            foreach (PlcBlockGroup subgroup in group.Groups)
//            {
//                FindDataBlocksInGroup(subgroup);
//            }
//        }

//        private List<PlcDbVariable> ReadDb(DataBlock db)
//        {
//            List<PlcDbVariable> contents = new List<PlcDbVariable>();

//            try
//            {
//                Log($"🔍 Читаем DB: {db.Name}, MemoryLayout: {db.MemoryLayout}");

//                if (db.Interface == null || !db.Interface.Members.Any())
//                {
//                    Log($"⚠ DB {db.Name} не имеет переменных в Interface.");
//                    return contents;
//                }

//                foreach (var member in db.Interface.Members)
//                {
//                    string name = member.Name;
//                    string dataType = member.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";

//                    contents.Add(new PlcDbVariable
//                    {
//                        Name = name,
//                        DataType = dataType,
//                        LogicalAddress = db.MemoryLayout == MemoryLayout.Optimized ? "Optimized" : "Standard"
//                    });

//                    Log($"📂 Тег загружен: {name}, type: {dataType}");
//                }
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Ошибка загрузки DB {db.Name}: {ex.Message}");
//            }

//            GC.KeepAlive(db);
//            return contents;
//        }

//        private string GetDbGroupPath(PlcBlockGroup group)
//        {
//            string path = group.Name;

//            while (group.Parent is PlcBlockGroup parentGroup)
//            {
//                path = parentGroup.Name + "/" + path;
//                group = parentGroup;
//            }

//            return path;
//        }

//        public PlcData GetTagsAndDB()
//        {
//            plcData = new PlcData();
//            LoadPlcTagsFromXml();
//            LoadDbFromXml();
//            Log($"✅ Итог: Загружено {plcData.Tags.Count} тегов, {plcData.DataBlocks.Count} DB.");
//            return plcData;
//        }

//        private void LoadTagTables(object tagGroup, string exportFolder)
//        {
//            if (tagGroup is PlcTagTableSystemGroup systemGroup)
//            {
//                foreach (PlcTagTable tagTable in systemGroup.TagTables)
//                {
//                    ExportTagTable(tagTable, exportFolder);
//                }

//                foreach (PlcTagTableUserGroup subgroup in systemGroup.Groups)
//                {
//                    LoadTagTables(subgroup, Path.Combine(exportFolder, subgroup.Name));
//                }
//            }
//            else if (tagGroup is PlcTagTableUserGroup userGroup)
//            {
//                foreach (PlcTagTable tagTable in userGroup.TagTables)
//                {
//                    ExportTagTable(tagTable, exportFolder);
//                }

//                foreach (PlcTagTableUserGroup subgroup in userGroup.Groups)
//                {
//                    LoadTagTables(subgroup, Path.Combine(exportFolder, subgroup.Name));
//                }
//            }
//        }

//        private void ExportTagTable(PlcTagTable tagTable, string exportFolder)
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
//                                    new XAttribute("Type", tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown")
//                                )
//                            )
//                        )
//                    )
//                );

//                doc.Save(exportPath);
//                Log($"📂 Таблица тегов {tagTable.Name} экспортирована в XML: {exportPath}");
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Ошибка экспорта таблицы тегов {tagTable.Name}: {ex.Message}");
//            }
//        }

//        public void LoadPlcTagsFromXml()
//        {
//            if (!Directory.Exists(plcTagsPath))
//            {
//                Log($"❌ Папка с тегами '{plcTagsPath}' не найдена.");
//                return;
//            }

//            string[] xmlFiles = Directory.GetFiles(plcTagsPath, "*.xml", SearchOption.AllDirectories);
//            Log($"🔍 Найдено {xmlFiles.Length} XML-файлов с тегами.");

//            if (xmlFiles.Length == 0)
//            {
//                Log($"❌ Нет XML-файлов с тегами! Проверка пути: {plcTagsPath}");
//                return;
//            }

//            plcData.Tags.Clear();

//            foreach (string file in xmlFiles)
//            {
//                Log($"📂 Читаем файл тегов: {file}");
//                ParseTagXml(file);
//            }

//            Log($"✅ Загружено {plcData.Tags.Count} тегов.");
//        }

//        private void ParseTagXml(string filePath)
//        {
//            try
//            {
//                XDocument doc = XDocument.Load(filePath);
//                string tableName = Path.GetFileNameWithoutExtension(filePath);

//                var tags = doc.Descendants("Tag");

//                foreach (var tag in tags)
//                {
//                    plcData.Tags.Add(new PlcTag
//                    {
//                        Name = tag.Attribute("Name")?.Value ?? "Unknown",
//                        DataType = tag.Attribute("Type")?.Value ?? "Unknown",
//                        TableName = tableName
//                    });
//                }
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Ошибка обработки {filePath}: {ex.Message}");
//            }
//        }

//        private void LoadDbFromXml()
//        {
//            if (!Directory.Exists(dbExportsPath))
//            {
//                Log($"❌ Папка с DB XML '{dbExportsPath}' не найдена.");
//                return;
//            }

//            string[] xmlFiles = Directory.GetFiles(dbExportsPath, "*.xml", SearchOption.TopDirectoryOnly);
//            Log($"🔍 Найдено {xmlFiles.Length} XML-файлов с DB.");

//            plcData.DataBlocks.Clear();

//            foreach (string file in xmlFiles)
//            {
//                ParseDbXml(file);
//            }
//        }

//        public async Task ExportProjectToXmlAsync(string projectPath)
//        {
//            await Task.Run(() =>
//            {
//                OpenProject(projectPath);

//                if (project == null)
//                {
//                    Log("❌ Проект TIA не открыт!");
//                    return;
//                }

//                Directory.CreateDirectory(plcTagsPath);
//                Directory.CreateDirectory(dbExportsPath);

//                Log($"📡 Экспортируем проект {project.Name} в XML...");

//                foreach (Device device in project.Devices)
//                {
//                    foreach (DeviceItem deviceItem in device.DeviceItems)
//                    {
//                        var softwareContainer = deviceItem.GetService<SoftwareContainer>();
//                        if (softwareContainer?.Software is PlcSoftware plcSoftware)
//                        {
//                            Log($"✅ PLC найден: {device.Name}");
//                            LoadTagTables(plcSoftware.TagTableGroup, plcTagsPath);
//                            FindDataBlocksInGroup(plcSoftware.BlockGroup);
//                        }
//                    }
//                }

//                Log("✅ Экспорт завершён.");

//                // 🔹 Закрываем проект, чтобы освободить его для TIA Portal
//                //CloseProject();
//            });
//        }

//        private void CloseProject()
//        {
//            try
//            {
//                if (project != null)
//                {
//                    Log("🔒 Закрываем проект TIA...");
//                    project.Close();
//                    project = null;
//                }

//                if (tiaPortal != null)
//                {
//                    Log("🔒 Закрываем TIA Portal...");
//                    tiaPortal.Dispose();
//                    tiaPortal = null;
//                }

//                Log("✅ Проект и TIA Portal закрыты.");
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Ошибка при закрытии проекта: {ex.Message}");
//            }
//        }

//        private void ExportDbToXml(PlcDb db)
//        {
//            try
//            {
//                string exportPath = Path.Combine(dbExportsPath, $"{db.Name}.xml");
//                Log($"📂 Экспорт DB в файл: {exportPath}");

//                XDocument doc = new XDocument(
//                    new XElement("DataBlock",
//                        new XAttribute("Name", db.Name),
//                        new XAttribute("Optimized", db.IsOptimized),
//                        new XElement("Variables",
//                            db.Variables.Select(v =>
//                                new XElement("Variable",
//                                    new XAttribute("Name", v.Name),
//                                    new XAttribute("Type", v.DataType),
//                                    new XAttribute("Address", v.LogicalAddress)
//                                )
//                            )
//                        )
//                    )
//                );

//                doc.Save(exportPath);
//                Log($"✅ DB {db.Name} экспортирован.");
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Ошибка экспорта DB {db.Name}: {ex.Message}");
//            }
//        }

//        private void ParseDbXml(string filePath)
//        {
//            try
//            {
//                XDocument doc = XDocument.Load(filePath);
//                string dbName = doc.Root?.Attribute("Name")?.Value ?? "Unknown";

//                plcData.DataBlocks.Add(new PlcDb
//                {
//                    Name = dbName,
//                    Variables = doc.Descendants("Variable")
//                        .Select(v => new PlcDbVariable
//                        {
//                            Name = v.Attribute("Name")?.Value ?? "Unknown",
//                            DataType = v.Attribute("Type")?.Value ?? "Unknown"
//                        }).ToList()
//                });

//                Log($"📂 DB {dbName} загружен.");
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Ошибка при разборе {filePath}: {ex.Message}");
//            }
//        }

//        public PlcSoftware? GetPlcSoftware()
//        {
//            if (project == null)
//            {
//                Log("❌ Ошибка: Проект TIA не открыт.");
//                return null;
//            }

//            Log($"🔍 Поиск PLC Software в проекте {project.Name}...");
//            Log($"📡 Список устройств в проекте:");

//            foreach (Device device in project.Devices)
//            {
//                Log($"- {device.Name} (Type: {device.TypeIdentifier})");

//                foreach (DeviceItem deviceItem in device.DeviceItems)
//                {
//                    var softwareContainer = deviceItem.GetService<SoftwareContainer>();

//                    if (softwareContainer?.Software is PlcSoftware plcSoftware)
//                    {
//                        Log($"✅ Найден PLC Software в устройстве: {device.Name}");
//                        return plcSoftware;
//                    }
//                }
//            }

//            Log("❌ Ошибка: PLC Software не найдено в проекте.");
//            return null;
//        }

//        public object ReadTagValue(string tagName)
//        {
//            try
//            {
//                var plcSoftware = GetPlcSoftware();
//                if (plcSoftware == null)
//                {
//                    Log("❌ Ошибка: PLC Software не найден.");
//                    return "N/A";
//                }

//                // 🔹 Ищем таблицу тегов, где хранится наш тег
//                var tagTable = plcSoftware.TagTableGroup.TagTables.FirstOrDefault(t => t.Tags.Any(tag => tag.Name == tagName));
//                if (tagTable == null)
//                {
//                    Log($"❌ Ошибка: Тег {tagName} не найден в таблицах TIA Portal.");
//                    return "N/A";
//                }

//                // 🔹 Ищем сам тег в таблице
//                var tag = tagTable.Tags.FirstOrDefault(t => t.Name == tagName);
//                if (tag == null)
//                {
//                    Log($"❌ Ошибка: Тег {tagName} отсутствует в TIA Portal.");
//                    return "N/A";
//                }

//                // 🔹 Читаем значение тега
//                return tag.GetAttribute("Value")?.ToString() ?? "N/A";
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Ошибка чтения тега {tagName}: {ex.Message}");
//                return "N/A";
//            }
//        }

//        private void Log(string message)
//        {
//            string logMessage = $"{DateTime.Now:HH:mm:ss} {message}";
//            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
//            Console.WriteLine(logMessage);
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Siemens_trend.Helpers
{
    public class TiaPortalHelper
    {
        private PlcData plcData = new PlcData();
        public PlcData PlcData => plcData;
        private Project? project;
        private TiaPortal? tiaPortal;
        private string logFilePath = "log.txt";

        private readonly string plcTagsPath = @"C:\TIA_Exports\TagTables";
        private readonly string dbExportsPath = @"C:\TIA_Exports\DB";

        public TiaPortalHelper()
        {
            File.WriteAllText(logFilePath, ""); // Очищаем лог при старте
            Log("🟢 Приложение запущено. Лог очищен.");
        }

        public bool AreTagsAvailable()
        {
            string[] tagFiles = Directory.Exists(plcTagsPath) ? Directory.GetFiles(plcTagsPath, "*.xml", SearchOption.AllDirectories) : new string[0];
            string[] dbFiles = Directory.Exists(dbExportsPath) ? Directory.GetFiles(dbExportsPath, "*.xml", SearchOption.TopDirectoryOnly) : new string[0];

            Log($"📂 Проверка тегов: {plcTagsPath} -> {tagFiles.Length} файлов");
            Log($"📂 Проверка DB: {dbExportsPath} -> {dbFiles.Length} файлов");

            return tagFiles.Length > 0 && dbFiles.Length > 0;
        }

        public void OpenProject(string projectPath)
        {
            try
            {
                Log($"🔍 Проверяем путь к проекту: {projectPath}");

                if (!File.Exists(projectPath))
                {
                    Log($"❌ Файл проекта не найден: {projectPath}");
                    return;
                }

                Log("🚀 Запускаем TIA Portal...");
                tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface); // Пробуем UI

                Log("📂 Открываем проект...");
                project = tiaPortal.Projects.Open(new FileInfo(projectPath));

                if (project != null)
                {
                    Log($"✅ Проект открыт: {project.Name}");
                }
                else
                {
                    Log("❌ Ошибка: Проект не открылся.");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка при открытии проекта: {ex.Message}");
            }
        }

        public void ExportProjectToXml(string projectPath)
        {
            OpenProject(projectPath);

            if (project == null)
            {
                Log("❌ Проект TIA не открыт!");
                return;
            }

            Directory.CreateDirectory(plcTagsPath);
            Directory.CreateDirectory(dbExportsPath);

            Log($"📡 Экспортируем проект {project.Name} в XML...");

            foreach (Device device in project.Devices)
            {
                foreach (DeviceItem deviceItem in device.DeviceItems)
                {
                    var softwareContainer = deviceItem.GetService<SoftwareContainer>();
                    if (softwareContainer?.Software is PlcSoftware plcSoftware)
                    {
                        Log($"✅ PLC найден: {device.Name}");
                        LoadTagTables(plcSoftware.TagTableGroup, plcTagsPath);
                        FindDataBlocksInGroup(plcSoftware.BlockGroup);
                    }
                }
            }

            Log("✅ Экспорт завершён. Теперь можно закрыть TIA Portal.");
        }

        //private void FindDataBlocksInGroup(PlcBlockGroup group)
        //{
        //    foreach (PlcBlock block in group.Blocks)
        //    {
        //        if (block is DataBlock db)
        //        {
        //            Log($"🔍 Обнаружен DB: {db.Name}, MemoryLayout: {db.MemoryLayout}");

        //            string dbGroupPath = GetDbGroupPath(group);
        //            bool isUDT = dbGroupPath.Contains("PLC data types");

        //            List<PlcDbVariable> variables = ReadDb(db);

        //            if (!isUDT && variables.Any(v => v.DataType.StartsWith("UDT_") || v.DataType.Contains("type")))
        //            {
        //                isUDT = true;
        //            }

        //            bool isSafety = db.GetAttribute("ProgrammingLanguage")?.ToString() == "F_DB";

        //            PlcDb plcDb = new PlcDb
        //            {
        //                Name = db.Name,
        //                IsOptimized = db.MemoryLayout == MemoryLayout.Optimized,
        //                IsSafety = isSafety,
        //                IsUDT = isUDT,
        //                Variables = variables
        //            };

        //            plcData.DataBlocks.Add(plcDb);
        //            Log($"✅ DB {db.Name} обработан: {plcDb.Variables.Count} переменных. Safety: {isSafety}, UDT: {isUDT}, Path: {dbGroupPath}");

        //            ExportDbToXml(plcDb);
        //        }
        //    }

        //    foreach (PlcBlockGroup subgroup in group.Groups)
        //    {
        //        FindDataBlocksInGroup(subgroup);
        //    }
        //}

        private void FindDataBlocksInGroup(PlcBlockGroup group)
        {
            Log($"📡 Ищем DB в группе: {group.Name}");

            foreach (PlcBlock block in group.Blocks)
            {
                Log($"🔍 Найден блок: {block.Name}, Тип: {block.GetType()}");

                if (block is DataBlock db)
                {
                    Log($"🔍 Найден DB: {db.Name}");

                    List<PlcDbVariable> variables = ReadDb(db);
                    if (variables.Count == 0)
                    {
                        Log($"⚠ DB {db.Name} не содержит переменных!");
                        continue;
                    }

                    PlcDb plcDb = new PlcDb
                    {
                        Name = db.Name,
                        IsOptimized = db.MemoryLayout == MemoryLayout.Optimized,
                        Variables = variables
                    };

                    plcData.DataBlocks.Add(plcDb);
                    ExportDbToXml(plcDb);
                    Log($"✅ DB {db.Name} экспортирован с {variables.Count} переменными.");
                }
            }

            foreach (PlcBlockGroup subgroup in group.Groups)
            {
                FindDataBlocksInGroup(subgroup);
            }
        }

        //private List<PlcDbVariable> ReadDb(DataBlock db)
        //{
        //    List<PlcDbVariable> contents = new List<PlcDbVariable>();

        //    try
        //    {
        //        Log($"🔍 Читаем DB: {db.Name}, MemoryLayout: {db.MemoryLayout}");

        //        if (db.Interface == null || !db.Interface.Members.Any())
        //        {
        //            Log($"⚠ DB {db.Name} не имеет переменных в Interface.");
        //            return contents;
        //        }

        //        foreach (var member in db.Interface.Members)
        //        {
        //            //string name = member.Name;
        //            string name = $"\"{db.Name}\".{member.Name}";
        //            string dataType = member.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";

        //            contents.Add(new PlcDbVariable
        //            {
        //                Name = name,
        //                DataType = dataType,
        //                LogicalAddress = db.MemoryLayout == MemoryLayout.Optimized ? "Optimized" : "Standard"
        //            });

        //            Log($"📂 Тег загружен: {name}, type: {dataType}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"❌ Ошибка загрузки DB {db.Name}: {ex.Message}");
        //    }

        //    GC.KeepAlive(db);
        //    return contents;
        //}

        private List<PlcDbVariable> ReadDb(DataBlock db)
        {
            List<PlcDbVariable> contents = new List<PlcDbVariable>();

            try
            {
                if (db.Interface == null || !db.Interface.Members.Any())
                {
                    return contents;
                }

                foreach (var member in db.Interface.Members)
                {
                    string varName = member.Name;
                    string dataType = member.GetAttribute("DataTypeName")?.ToString() ?? "Unknown";

                    //// ✅ Убираем тип данных из имени
                    //if (varName.Contains(":"))
                    //{
                    //    varName = varName.Substring(0, varName.LastIndexOf(":"));
                    //}

                    // ✅ Добавляем имя DB в начало
                    varName = $"{db.Name}.{varName}";

                    contents.Add(new PlcDbVariable
                    {
                        Name = varName,
                        DataType = dataType
                    });

                    Log($"📂 Переменная загружена: {varName}, Тип: {dataType}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка загрузки DB {db.Name}: {ex.Message}");
            }

            return contents;
        }

        private string GetDbGroupPath(PlcBlockGroup group)
        {
            string path = group.Name;

            while (group.Parent is PlcBlockGroup parentGroup)
            {
                path = parentGroup.Name + "/" + path;
                group = parentGroup;
            }

            return path;
        }

        public PlcData GetTagsAndDB()
        {
            plcData = new PlcData();
            LoadPlcTagsFromXml();
            LoadDbFromXml();
            Log($"✅ Итог: Загружено {plcData.Tags.Count} тегов, {plcData.DataBlocks.Count} DB.");
            return plcData;
        }

        private void LoadTagTables(object tagGroup, string exportFolder)
        {
            if (tagGroup is PlcTagTableSystemGroup systemGroup)
            {
                foreach (PlcTagTable tagTable in systemGroup.TagTables)
                {
                    ExportTagTable(tagTable, exportFolder);
                }

                foreach (PlcTagTableUserGroup subgroup in systemGroup.Groups)
                {
                    LoadTagTables(subgroup, Path.Combine(exportFolder, subgroup.Name));
                }
            }
            else if (tagGroup is PlcTagTableUserGroup userGroup)
            {
                foreach (PlcTagTable tagTable in userGroup.TagTables)
                {
                    ExportTagTable(tagTable, exportFolder);
                }

                foreach (PlcTagTableUserGroup subgroup in userGroup.Groups)
                {
                    LoadTagTables(subgroup, Path.Combine(exportFolder, subgroup.Name));
                }
            }
        }

        private void ExportTagTable(PlcTagTable tagTable, string exportFolder)
        {
            try
            {
                string exportPath = Path.Combine(exportFolder, $"{tagTable.Name}.xml");

                //XDocument doc = new XDocument(
                //    new XElement("TagTable",
                //        new XAttribute("Name", tagTable.Name),
                //        new XElement("Tags",
                //            tagTable.Tags.Select(tag =>
                //                new XElement("Tag",
                //                    new XAttribute("Name", tag.Name),
                //                    new XAttribute("Type", tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown")
                //                )
                //            )
                //        )
                //    )
                //);

                XDocument doc = new XDocument(
                    new XElement("TagTable",
                        new XAttribute("Name", tagTable.Name),
                        new XElement("Tags",
                            tagTable.Tags.Select(tag =>
                                new XElement("Tag",
                                    new XAttribute("Name", tag.Name),
                                    new XAttribute("DataType", tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown"),
                                    new XAttribute("Address", tag.GetAttribute("LogicalAddress")?.ToString() ?? "No Address")
                                )
                            )
                        )
                    )
                );

                doc.Save(exportPath);
                Log($"📂 Таблица тегов {tagTable.Name} экспортирована в XML: {exportPath}");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка экспорта таблицы тегов {tagTable.Name}: {ex.Message}");
            }
        }

        public void LoadPlcTagsFromXml()
        {
            if (!Directory.Exists(plcTagsPath))
            {
                Log($"❌ Папка с тегами '{plcTagsPath}' не найдена.");
                return;
            }

            string[] xmlFiles = Directory.GetFiles(plcTagsPath, "*.xml", SearchOption.AllDirectories);
            Log($"🔍 Найдено {xmlFiles.Length} XML-файлов с тегами.");

            if (xmlFiles.Length == 0)
            {
                Log($"❌ Нет XML-файлов с тегами! Проверка пути: {plcTagsPath}");
                return;
            }

            plcData.Tags.Clear();

            foreach (string file in xmlFiles)
            {
                Log($"📂 Читаем файл тегов: {file}");
                ParseTagXml(file);
            }

            Log($"✅ Загружено {plcData.Tags.Count} тегов.");
        }

        //private void ParseTagXml(string filePath)
        //{
        //    try
        //    {
        //        XDocument doc = XDocument.Load(filePath);
        //        string tableName = Path.GetFileNameWithoutExtension(filePath); // Используем имя файла как имя таблицы

        //        var tags = doc.Descendants("Tag");

        //        foreach (var tag in tags)
        //        {
        //            string name = tag.Attribute("Name")?.Value ?? "Unknown";
        //            string type = tag.Attribute("Type")?.Value ?? "Unknown";

        //            // ✅ Добавляем имя DB в начало, если тег относится к DB
        //            if (tableName.StartsWith("DB") && !name.Contains("."))
        //            {
        //                name = $"{tableName}.{name}";
        //            }

        //            // ✅ Убираем тип данных в конце имени
        //            if (name.Contains(":"))
        //            {
        //                name = name.Substring(0, name.LastIndexOf(":"));
        //            }

        //            plcData.Tags.Add(new PlcTag
        //            {
        //                Name = name,
        //                DataType = type,
        //                TableName = tableName
        //            });

        //            Log($"📂 Тег загружен: {name}, type: {type}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"❌ Ошибка обработки {filePath}: {ex.Message}");
        //    }
        //}

        private void ParseTagXml(string filePath)
        {
            try
            {
                XDocument doc = XDocument.Load(filePath);
                string tableName = Path.GetFileNameWithoutExtension(filePath);

                var tags = doc.Descendants("Tag");

                foreach (var tag in tags)
                {
                    //string name = tag.Attribute("Name")?.Value ?? "Unknown";
                    string name = (tag.Attribute("Name")?.Value ?? "Unknown").Trim();
                    string type = tag.Attribute("Type")?.Value ?? "Unknown";

                    // ✅ Убираем тип данных из имени (например, "TagName: Bool" -> "TagName")
                    //if (name.Contains(":"))
                    //{
                    //    name = name.Substring(0, name.LastIndexOf(":")).Trim();
                    //}

                    // ✅ Если тег относится к DB, добавляем имя DB в начало
                    if (tableName.StartsWith("DB") && !name.StartsWith(tableName + "."))
                    {
                        name = $"{tableName}.{name}";
                    }

                    plcData.Tags.Add(new PlcTag
                    {
                        Name = name,
                        DataType = type,
                        TableName = tableName
                    });

                    Log($"📂 Тег загружен: {name}, Тип: {type}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка обработки {filePath}: {ex.Message}");
            }
        }

        //private void ParseDbXml(string filePath)
        //{
        //    try
        //    {
        //        XDocument doc = XDocument.Load(filePath);
        //        string dbName = doc.Root?.Attribute("Name")?.Value ?? "Unknown";

        //        plcData.DataBlocks.Add(new PlcDb
        //        {
        //            Name = dbName,


        //            Variables = doc.Descendants("Variable")
        //                .Select(v => new PlcDbVariable
        //                {
        //                    Name = v.Attribute("Name")?.Value ?? "Unknown",
        //                    DataType = v.Attribute("Type")?.Value ?? "Unknown"
        //                }).ToList()
        //        });

        //        Log($"📂 DB {dbName} загружен.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"❌ Ошибка при разборе {filePath}: {ex.Message}");
        //    }
        //}

        private void ParseDbXml(string filePath)
        {
            try
            {
                XDocument doc = XDocument.Load(filePath);
                //string dbName = doc.Root?.Attribute("Name")?.Value ?? "Unknown";
                string dbName = (doc.Root?.Attribute("Name")?.Value ?? "Unknown").Trim();

                var variables = doc.Descendants("Variable").Select(var =>
                {
                    string varName = var.Attribute("Name")?.Value ?? "Unknown";
                    string dataType = var.Attribute("Type")?.Value ?? "Unknown";

                    // ✅ Убираем тип данных из имени (например, "VarName: Int" -> "VarName")
                    if (varName.Contains(":"))
                    {
                        varName = varName.Substring(0, varName.LastIndexOf(":")).Trim();
                    }

                    // ✅ Добавляем имя DB в начало (например, "GlobalFlags.VarName")
                    if (!varName.StartsWith(dbName + "."))
                    {
                        varName = $"{dbName}.{varName}";
                    }

                    return new PlcDbVariable
                    {
                        Name = varName,
                        DataType = dataType
                    };
                }).ToList();

                plcData.DataBlocks.Add(new PlcDb
                {
                    Name = dbName,
                    Variables = variables
                });

                Log($"📂 DB {dbName} загружен. Переменных: {variables.Count}");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка при разборе {filePath}: {ex.Message}");
            }
        }

        private void LoadDbFromXml()
        {
            if (!Directory.Exists(dbExportsPath))
            {
                Log($"❌ Папка с DB XML '{dbExportsPath}' не найдена.");
                return;
            }

            string[] xmlFiles = Directory.GetFiles(dbExportsPath, "*.xml", SearchOption.TopDirectoryOnly);
            Log($"🔍 Найдено {xmlFiles.Length} XML-файлов с DB.");

            plcData.DataBlocks.Clear();

            foreach (string file in xmlFiles)
            {
                ParseDbXml(file);
            }
        }

        public async Task ExportProjectToXmlAsync(string projectPath)
        {
            await Task.Run(() =>
            {
                OpenProject(projectPath);

                if (project == null)
                {
                    Log("❌ Проект TIA не открыт!");
                    return;
                }

                Directory.CreateDirectory(plcTagsPath);
                Directory.CreateDirectory(dbExportsPath);

                Log($"📡 Экспортируем проект {project.Name} в XML...");

                foreach (Device device in project.Devices)
                {
                    foreach (DeviceItem deviceItem in device.DeviceItems)
                    {
                        var softwareContainer = deviceItem.GetService<SoftwareContainer>();
                        if (softwareContainer?.Software is PlcSoftware plcSoftware)
                        {
                            Log($"✅ PLC найден: {device.Name}");

                            ExportTagTableGroups(plcSoftware);
                            ExportDbStructure(plcSoftware);

                            LoadTagTables(plcSoftware.TagTableGroup, plcTagsPath);
                            FindDataBlocksInGroup(plcSoftware.BlockGroup);
                        }
                    }
                }

                Log("✅ Экспорт завершён.");

                // 🔹 Закрываем проект, чтобы освободить его для TIA Portal
                //CloseProject();
            });
        }

        private void CloseProject()
        {
            try
            {
                if (project != null)
                {
                    Log("🔒 Закрываем проект TIA...");
                    project.Close();
                    project = null;
                }

                if (tiaPortal != null)
                {
                    Log("🔒 Закрываем TIA Portal...");
                    tiaPortal.Dispose();
                    tiaPortal = null;
                }

                Log("✅ Проект и TIA Portal закрыты.");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка при закрытии проекта: {ex.Message}");
            }
        }

        //private void ExportDbToXml(PlcDb db)
        //{
        //    try
        //    {
        //        string exportPath = Path.Combine(dbExportsPath, $"{db.Name}.xml");
        //        Log($"📂 Экспорт DB в файл: {exportPath}");

        //        XDocument doc = new XDocument(
        //            new XElement("DataBlock",
        //                new XAttribute("Name", db.Name),
        //                new XAttribute("Optimized", db.IsOptimized),
        //                new XElement("Variables",
        //                    db.Variables.Select(v =>
        //                        new XElement("Variable",
        //                            new XAttribute("Name", v.Name),
        //                            new XAttribute("Type", v.DataType),
        //                            new XAttribute("Address", v.LogicalAddress)
        //                        )
        //                    )
        //                )
        //            )
        //        );

        //        doc.Save(exportPath);
        //        Log($"✅ DB {db.Name} экспортирован.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"❌ Ошибка экспорта DB {db.Name}: {ex.Message}");
        //    }
        //}

        private void ExportDbToXml(PlcDb db)
        {
            try
            {
                string exportPath = Path.Combine(dbExportsPath, $"{db.Name}.xml");
                Directory.CreateDirectory(dbExportsPath);

                XDocument doc = new XDocument(
                    new XElement("DataBlock",
                        new XAttribute("Name", db.Name),
                        new XElement("Variables",
                            db.Variables.Select(v =>
                                new XElement("Variable",
                                    new XAttribute("Name", v.Name),
                                    new XAttribute("Type", v.DataType)
                                )
                            )
                        )
                    )
                );

                doc.Save(exportPath);
                Log($"📂 DB {db.Name} сохранён в XML.");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка экспорта DB {db.Name}: {ex.Message}");
            }
        }

        public PlcSoftware? GetPlcSoftware()
        {
            if (project == null)
            {
                Log("❌ Ошибка: Проект TIA не открыт.");
                return null;
            }

            Log($"🔍 Поиск PLC Software в проекте {project.Name}...");
            Log($"📡 Список устройств в проекте:");

            foreach (Device device in project.Devices)
            {
                Log($"- {device.Name} (Type: {device.TypeIdentifier})");

                foreach (DeviceItem deviceItem in device.DeviceItems)
                {
                    var softwareContainer = deviceItem.GetService<SoftwareContainer>();

                    if (softwareContainer?.Software is PlcSoftware plcSoftware)
                    {
                        Log($"✅ Найден PLC Software в устройстве: {device.Name}");
                        return plcSoftware;
                    }
                }
            }

            Log("❌ Ошибка: PLC Software не найдено в проекте.");
            return null;
        }

        //public object ReadTagValue(string tagName)
        //{
        //    try
        //    {
        //        var plcSoftware = GetPlcSoftware();
        //        if (plcSoftware == null)
        //        {
        //            Log("❌ Ошибка: PLC Software не найден.");
        //            return "N/A";
        //        }

        //        // 🔹 Формируем корректное имя тега
        //        string correctedTagName = plcData.Tags
        //            .Where(tag => tag.Name.EndsWith(tagName)) // Ищем по концовке имени
        //            .Select(tag => tag.Name)
        //            .FirstOrDefault() ?? tagName;

        //        // ✅ Если имя содержит `:`, убираем тип данных в конце
        //        if (correctedTagName.Contains(":"))
        //        {
        //            correctedTagName = correctedTagName.Split(':')[0];
        //        }

        //        // ✅ Если тег принадлежит DB, но не содержит имя DB — добавляем его
        //        var dbTag = plcData.DataBlocks
        //            .SelectMany(db => db.Variables)
        //            .FirstOrDefault(var => var.Name.EndsWith(tagName));

        //        if (dbTag != null && !correctedTagName.Contains("."))
        //        {
        //            correctedTagName = $"{dbTag.Name}.{correctedTagName}";
        //        }

        //        Log($"📡 Читаем тег: {correctedTagName}");

        //        // 🔹 Ищем таблицу тегов в TIA Portal
        //        var tagTable = plcSoftware.TagTableGroup.TagTables
        //            .FirstOrDefault(t => t.Tags.Any(tag => tag.Name == correctedTagName));

        //        if (tagTable == null)
        //        {
        //            Log($"❌ Ошибка: Тег {correctedTagName} не найден в таблицах TIA Portal.");
        //            return "N/A";
        //        }

        //        // 🔹 Ищем сам тег
        //        var tag = tagTable.Tags.FirstOrDefault(t => t.Name == correctedTagName);
        //        if (tag == null)
        //        {
        //            Log($"❌ Ошибка: Тег {correctedTagName} отсутствует в TIA Portal.");
        //            return "N/A";
        //        }

        //        // 🔹 Читаем значение
        //        return tag.GetAttribute("Value")?.ToString() ?? "N/A";
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"❌ Ошибка чтения тега {tagName}: {ex.Message}");
        //        return "N/A";
        //    }
        //}
        public void LogAllTags()
        {
            var plcSoftware = GetPlcSoftware();
            if (plcSoftware == null)
            {
                Log("❌ Ошибка: PLC Software не найден.");
                return;
            }

            Log("📡 Список тегов в TIA Portal:");

            foreach (var tagTable in plcSoftware.TagTableGroup.TagTables)
            {
                foreach (var tag in tagTable.Tags)
                {
                    Log($"🔹 {tag.Name}");
                }
            }
        }

        //public object ReadTagValue(string tagName)
        //{
        //    try
        //    {
        //        var plcSoftware = GetPlcSoftware();
        //        if (plcSoftware == null)
        //        {
        //            Log("❌ Ошибка: PLC Software не найден.");
        //            return "N/A";
        //        }

        //        // 🔍 **ШАГ 1: Проверяем в таблицах тегов**
        //        Log($"📡 Читаем тег: {tagName}");
        //        var tagTable = plcSoftware.TagTableGroup.TagTables
        //            .FirstOrDefault(t => t.Tags.Any(tag => tag.Name == tagName));

        //        if (tagTable != null)
        //        {
        //            var tag = tagTable.Tags.FirstOrDefault(t => t.Name == tagName);
        //            if (tag != null)
        //            {
        //                return tag.GetAttribute("Value")?.ToString() ?? "N/A";
        //            }
        //        }

        //        // 🔍 **ШАГ 2: Проверяем в DB**
        //        var dbTag = plcData.DataBlocks
        //            .SelectMany(db => db.Variables)
        //            .FirstOrDefault(var => var.Name == tagName);

        //        if (dbTag != null)
        //        {
        //            Log($"✅ Тег {tagName} найден в DB.");
        //            return "DB Value (нужно доработать чтение значений)"; // Тут пока заглушка
        //        }

        //        Log($"❌ Ошибка: Тег {tagName} не найден ни в таблицах, ни в DB.");
        //        return "N/A";
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"❌ Ошибка чтения тега {tagName}: {ex.Message}");
        //        return "N/A";
        //    }
        //}

        //public object ReadTagValue(string tagName)
        //{
        //    try
        //    {
        //        var plcSoftware = GetPlcSoftware();
        //        if (plcSoftware == null)
        //        {
        //            Log("❌ Ошибка: PLC Software не найден.");
        //            return "N/A";
        //        }

        //        Log($"📡 Читаем тег: {tagName}");

        //        // 🔹 Ищем в XML (файлы в TagTables)
        //        string tagTablesPath = Path.Combine(@"C:\TIA_Exports", "TagTables");
        //        if (Directory.Exists(tagTablesPath))
        //        {
        //            foreach (string file in Directory.GetFiles(tagTablesPath, "*.xml"))
        //            {
        //                XDocument doc = XDocument.Load(file);
        //                var tagElements = doc.Descendants("Tag");

        //                foreach (var tagElement in tagElements)
        //                {
        //                    string xmlTagName = tagElement.Attribute("Name")?.Value ?? "";

        //                    Log($"✅ Тег найден в XML {xmlTagName}");

        //                    if (xmlTagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        Log($"✅ Тег найден в XML ({Path.GetFileName(file)}): {xmlTagName}");
        //                        return "Value from XML"; // Здесь можно добавить реальное чтение значений
        //                    }
        //                }
        //            }
        //        }

        //        // 🔹 Ищем в DB
        //        var dbTag = plcData.DataBlocks
        //            .SelectMany(db => db.Variables)
        //            .FirstOrDefault(var => var.Name.Trim() == tagName.Trim());

        //        if (dbTag != null)
        //        {
        //            Log($"✅ Тег найден в DB: {tagName}");
        //            return "DB Value"; // Нужно добавить код чтения DB
        //        }

        //        Log($"❌ Ошибка: Тег {tagName} не найден ни в XML, ни в DB.");
        //        return "N/A";
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"❌ Ошибка чтения тега {tagName}: {ex.Message}");
        //        return "N/A";
        //    }
        //}

        public object ReadTagValue(string tagName)
        {
            try
            {
                var plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    Log("❌ Ошибка: PLC Software не найден.");
                    return "N/A";
                }

                // 🔍 Очистка имени тега от типа данных
                //string cleanedTagName = tagName.Trim().Split(':')[0].Trim(); // Убираем все после `:` (если есть)
                //cleanedTagName = cleanedTagName.Split('(')[0].Trim(); // Убираем все после `(` (если есть)
                //cleanedTagName = cleanedTagName.Trim('"'); // Убираем кавычки
                string cleanedTagName = tagName.Trim(); // Убираем только кавычки, но не индекс



                // 🔹 **ШАГ 1: Проверяем в XML (перебираем файлы в TagTables)**
                string tagTablesPath = Path.Combine(@"C:\TIA_Exports", "TagTables");
                if (Directory.Exists(tagTablesPath))
                {
                    foreach (string file in Directory.GetFiles(tagTablesPath, "*.xml", SearchOption.AllDirectories))
                    {
                        XDocument doc = XDocument.Load(file);

                        foreach (var tag in doc.Descendants("Tag"))
                        {
                            if (tag.Attribute("Name")?.Value == cleanedTagName)
                            {
                                Log($"✅ Тег {tag.Attribute("Name")?.Value} найден в XML ({Path.GetFileName(file)}): {cleanedTagName}");
                                return "Value from XML"; // 🔥 Здесь заглушка, нужно добавить реальное чтение
                            }
                            Log($"🔍 Тег в XML ({Path.GetFileName(file)}): {tag.Attribute("Name")?.Value}");
                        }

                        //var tagElement = doc.Descendants("Tag")
                        //    .FirstOrDefault(t => (t.Attribute("Name").Value) == cleanedTagName);

                        //Log($"✅ Тег найден в XML ({tagElement}): {cleanedTagName}");
                        //if (tagElement != null)
                        //{
                        //    Log($"✅ Тег найден в XML ({Path.GetFileName(file)}): {cleanedTagName}");
                        //    return "Value from XML"; // 🔥 Здесь заглушка, нужно добавить реальное чтение
                        //}
                    }
                }

                // 🔹 **ШАГ 2: Проверяем в DB**
                var dbTag = plcData.DataBlocks
                    .SelectMany(db => db.Variables)
                    .FirstOrDefault(var => var.Name.Trim().Trim('"') == cleanedTagName);

                if (dbTag != null)
                {
                    Log($"✅ Тег найден в DB: {cleanedTagName}");
                    return "DB Value"; // 🔥 Заглушка, сюда нужно реальное чтение
                }

                Log($"❌ Ошибка: Тег {cleanedTagName} не найден ни в XML, ни в DB.");
                return "N/A";
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка чтения тега {tagName}: {ex.Message}");
                return "N/A";
            }
        }


        private void Log(string message)
        {
            string logMessage = $"{DateTime.Now:HH:mm:ss} {message}";
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            Console.WriteLine(logMessage);
        }

        private void ExportTagTableGroups(PlcSoftware plcSoftware)
        {
            string exportPath = Path.Combine(plcTagsPath, "PlcTagTables_Structure.xml");
            Directory.CreateDirectory(plcTagsPath);

            XDocument doc = new XDocument(
                new XElement("TagTableGroups",
                    new XElement("SystemGroups",
                        plcSoftware.TagTableGroup.Groups
                            .OfType<PlcTagTableSystemGroup>()
                            .Select(group =>
                                new XElement("Group",
                                    new XAttribute("Name", group.Name),
                                    new XElement("TagTables",
                                        group.TagTables.Select(tagTable =>
                                            new XElement("TagTable",
                                                new XAttribute("Name", tagTable.Name),
                                                new XElement("Tags",
                                                    tagTable.Tags.Select(tag =>
                                                        new XElement("Tag",
                                                            new XAttribute("Name", tag.Name),
                                                            new XAttribute("Type", tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown"),
                                                            new XAttribute("Address", tag.GetAttribute("LogicalAddress")?.ToString() ?? "No Address")
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                    ),
                    new XElement("UserGroups",
                        plcSoftware.TagTableGroup.Groups
                            .OfType<PlcTagTableUserGroup>()
                            .Select(group =>
                                new XElement("Group",
                                    new XAttribute("Name", group.Name),
                                    new XElement("TagTables",
                                        group.TagTables.Select(tagTable =>
                                            new XElement("TagTable",
                                                new XAttribute("Name", tagTable.Name),
                                                new XElement("Tags",
                                                    tagTable.Tags.Select(tag =>
                                                        new XElement("Tag",
                                                            new XAttribute("Name", tag.Name),
                                                            new XAttribute("Type", tag.GetAttribute("DataTypeName")?.ToString() ?? "Unknown"),
                                                            new XAttribute("Address", tag.GetAttribute("LogicalAddress")?.ToString() ?? "No Address")
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                    )
                )
            );

            doc.Save(exportPath);
            Log($"📂 Структура таблиц тегов экспортирована: {exportPath}");
        }

        private void ExportDbStructure(PlcSoftware plcSoftware)
        {
            string exportPath = Path.Combine(dbExportsPath, "DB_Structure.xml");
            Directory.CreateDirectory(dbExportsPath);

            XDocument doc = new XDocument(
                new XElement("DataBlocks",
                    plcSoftware.BlockGroup.Blocks
                        .OfType<DataBlock>()
                        .Select(db =>
                            new XElement("DataBlock",
                                new XAttribute("Name", db.Name),
                                new XAttribute("Optimized", db.MemoryLayout == MemoryLayout.Optimized),
                                new XElement("Variables",
                                    db.Interface?.Members.Select(member =>
                                        new XElement("Variable",
                                            new XAttribute("Name", member.Name),
                                            new XAttribute("Type", member.GetAttribute("DataTypeName")?.ToString() ?? "Unknown")
                                        )
                                    ) ?? new List<XElement>()
                                )
                            )
                        )
                )
            );

            doc.Save(exportPath);
            Log($"📂 Структура DB экспортирована: {exportPath}");
        }

    }
}