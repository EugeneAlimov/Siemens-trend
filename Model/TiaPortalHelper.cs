using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Tags;
using System.Windows;

namespace SiemensTagExporter
{
    /// <summary>
    /// Делегат для событий прогресса
    /// </summary>
    /// <param name="percent">Процент завершения</param>
    /// <param name="message">Сообщение о статусе</param>
    public delegate void ProgressChangedHandler(int percent, string message);

    /// <summary>
    /// Информация о ПЛК-устройстве
    /// </summary>
    public class PlcInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string OrderNumber { get; set; }
        public PlcSoftware Software { get; set; }
        public DeviceItem DeviceItem { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }
    }

    /// <summary>
    /// Информация о теге ПЛК
    /// </summary>
    public class PlcTagInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string LogicalAddress { get; set; }
        public string Comment { get; set; }
        public string TableName { get; set; }

        public string FullName => $"{TableName}.{Name}";

        public override string ToString()
        {
            return $"{Name} : {DataType} @ {LogicalAddress}";
        }
    }

    /// <summary>
    /// Информация о блоке данных
    /// </summary>
    public class DbInfo
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public bool IsOptimized { get; set; }
        public string Path { get; set; }
        public GlobalDB Instance { get; set; }

        public override string ToString()
        {
            return $"{Name} (DB{Number}) {(IsOptimized ? "[Optimized]" : "[Standard]")}";
        }
    }

    /// <summary>
    /// Информация о теге DB
    /// </summary>
    public class DbTagInfo
    {
        public string DbName { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Offset { get; set; }  // Для неоптимизированных DB
        public bool IsOptimized { get; set; }
        public string Path { get; set; }  // Путь в иерархии (для структур)

        public string FullTagName => Path != null ? $"{DbName}.{Path}.{Name}" : $"{DbName}.{Name}";

        public override string ToString()
        {
            return $"{Name} : {DataType}";
        }
    }

    /// <summary>
    /// Класс-помощник для работы с TIA Portal Openness API
    /// </summary>
    public class TiaPortalHelper : IDisposable
    {
        #region Поля и свойства

        private TiaPortal _tiaPortal;
        private Project _project;
        private bool _isConnected;
        private readonly ILogger _logger;

        /// <summary>
        /// Статус подключения к TIA Portal
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Имя текущего проекта
        /// </summary>
        public string ProjectName => _project?.Name ?? "Не подключено";

        #endregion

        #region События

        /// <summary>
        /// Событие изменения прогресса операции
        /// </summary>
        public event ProgressChangedHandler ProgressChanged;

        /// <summary>
        /// Событие успешного подключения к TIA Portal
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Событие отключения от TIA Portal
        /// </summary>
        public event EventHandler Disconnected;

        #endregion

        #region Конструкторы и инициализация

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logger">Логгер для записи информации</param>
        public TiaPortalHelper(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isConnected = false;
        }

        #endregion

        private string GetTiaOpennessVersion()
        {
            try
            {
                // Получаем тип TiaPortal из загруженной сборки
                Type tiaPortalType = typeof(TiaPortal);

                // Получаем версию сборки
                return tiaPortalType.Assembly.GetName().Version.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }

        #region Методы для работы с TIA Portal

        /// <summary>
        /// Подключение к TIA Portal
        /// </summary>
        /// <param name="autoStart">Запускать ли TIA Portal если он не запущен</param>
        /// <returns>Успешность подключения</returns>
        public bool Connect(bool autoStart = false)
        {
            try
            {
                // Получаем список запущенных процессов TIA Portal
                IList<TiaPortalProcess> processes = TiaPortal.GetProcesses();

                if (processes.Count == 0)
                {
                    if (!autoStart)
                    {
                        _logger.Error("TIA Portal не запущен. Запустите TIA Portal и откройте проект.");
                        return false;
                    }

                    // Запускаем новый экземпляр, если разрешено автозапуском
                    _logger.Info("Запуск нового экземпляра TIA Portal...");
                    _tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface);
                    _logger.Info("TIA Portal запущен.");
                }
                else
                {
                    // Подключаемся к первому найденному экземпляру
                    _tiaPortal = processes[0].Attach();
                    _logger.Info($"Подключено к существующему экземпляру TIA Portal.");
                }

                // Проверяем наличие открытых проектов
                if (_tiaPortal.Projects.Count == 0)
                {
                    _logger.Error("Нет открытых проектов в TIA Portal.");
                    return false;
                }

                // Получаем первый открытый проект
                _project = _tiaPortal.Projects[0];
                _logger.Info($"Подключено к проекту: {_project.Name}");

                _isConnected = true;
                Connected?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении к TIA Portal: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Отключение от TIA Portal
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _isConnected = false;
                _project = null;

                // TIA Portal не нужно закрывать, мы просто отключаемся от него
                _tiaPortal = null;

                _logger.Info("Отключено от TIA Portal.");
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отключении от TIA Portal: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение списка доступных ПЛК в проекте
        /// </summary>
        /// <returns>Список информации о ПЛК</returns>
        public List<PlcInfo> GetAvailablePlcs()
        {
            List<PlcInfo> plcList = new List<PlcInfo>();

            if (!_isConnected || _project == null)
            {
                _logger.Error("Нет подключения к TIA Portal.");
                return plcList;
            }

            try
            {
                // Получаем все устройства проекта
                foreach (Device device in _project.Devices)
                {
                    foreach (DeviceItem deviceItem in device.DeviceItems)
                    {
                        // Проверяем, является ли устройство ПЛК
                        if (deviceItem.TypeIdentifier.Contains("SIMATIC"))
                        {
                            // Получаем контейнер ПО
                            SoftwareContainer softwareContainer = deviceItem.GetService<SoftwareContainer>();
                            if (softwareContainer != null && softwareContainer.Software is PlcSoftware)
                            {
                                PlcSoftware plcSoftware = softwareContainer.Software as PlcSoftware;

                                plcList.Add(new PlcInfo
                                {
                                    Name = deviceItem.Name,
                                    Type = deviceItem.TypeIdentifier,
                                    OrderNumber = deviceItem.GetAttribute("OrderNumber")?.ToString() ?? "Неизвестно",
                                    Software = plcSoftware,
                                    DeviceItem = deviceItem
                                });

                                _logger.Info($"Найден ПЛК: {deviceItem.Name}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении списка ПЛК: {ex.Message}");
            }

            return plcList;
        }

        #endregion

        #region Методы для работы с тегами ПЛК

        /// <summary>
        /// Получение всех тегов ПЛК
        /// </summary>
        /// <param name="plcSoftware">Программное обеспечение ПЛК</param>
        /// <returns>Список информации о тегах</returns>
        public List<PlcTagInfo> GetAllPlcTags(PlcSoftware plcSoftware)
        {
            List<PlcTagInfo> allTags = new List<PlcTagInfo>();

            if (!_isConnected || plcSoftware == null)
            {
                _logger.Error("Нет подключения к TIA Portal или не выбран ПЛК.");
                return allTags;
            }

            try
            {
                // Получаем группу таблиц тегов
                PlcTagTableSystemGroup tagTableGroup = plcSoftware.TagTableGroup;

                // Находим все таблицы тегов для отображения прогресса
                List<PlcTagTable> allTagTables = GetAllTagTables(tagTableGroup);
                int totalTables = allTagTables.Count;
                int processedTables = 0;

                // Обрабатываем пользовательские таблицы тегов
                foreach (PlcTagTable tagTable in tagTableGroup.TagTables)
                {
                    ProcessTagTable(tagTable, allTags);
                    processedTables++;
                    ReportProgress(processedTables * 100 / totalTables, $"Обработка таблицы {tagTable.Name}");
                }

                // Обрабатываем системные таблицы тегов
                foreach (PlcTagTableGroup userGroup in tagTableGroup.Groups)
                {
                    ProcessTagTableGroup(userGroup, allTags, ref processedTables, totalTables);
                }

                ReportProgress(100, "Загрузка тегов завершена");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
            }

            return allTags;
        }

        /// <summary>
        /// Асинхронно получает все теги ПЛК
        /// </summary>
        public async Task<List<PlcTagInfo>> GetAllPlcTagsAsync(PlcSoftware plcSoftware)
        {
            return await Task.Run(() => GetAllPlcTags(plcSoftware));
        }

        /// <summary>
        /// Получает все таблицы тегов из группы рекурсивно
        /// </summary>
        private List<PlcTagTable> GetAllTagTables(PlcTagTableSystemGroup tagTableGroup)
        {
            List<PlcTagTable> tables = new List<PlcTagTable>();

            // Добавляем таблицы из текущей группы
            tables.AddRange(tagTableGroup.TagTables);

            // Рекурсивно добавляем таблицы из подгрупп
            foreach (PlcTagTableGroup subGroup in tagTableGroup.Groups)
            {
                tables.AddRange(GetAllTagTablesFromGroup(subGroup));
            }

            return tables;
        }

        /// <summary>
        /// Получает все таблицы тегов из группы рекурсивно
        /// </summary>
        private List<PlcTagTable> GetAllTagTablesFromGroup(PlcTagTableGroup group)
        {
            List<PlcTagTable> tables = new List<PlcTagTable>();

            // Добавляем таблицы из текущей группы
            tables.AddRange(group.TagTables);

            // Рекурсивно добавляем таблицы из подгрупп
            foreach (PlcTagTableGroup subGroup in group.Groups)
            {
                tables.AddRange(GetAllTagTablesFromGroup(subGroup));
            }

            return tables;
        }

        /// <summary>
        /// Рекурсивно обрабатывает группу таблиц тегов
        /// </summary>
        private void ProcessTagTableGroup(PlcTagTableGroup group, List<PlcTagInfo> allTags,
            ref int processedTables, int totalTables)
        {
            // Обрабатываем таблицы текущей группы
            foreach (PlcTagTable tagTable in group.TagTables)
            {
                ProcessTagTable(tagTable, allTags);
                processedTables++;
                ReportProgress(processedTables * 100 / totalTables, $"Обработка таблицы {tagTable.Name}");
            }

            // Рекурсивно обрабатываем подгруппы
            foreach (PlcTagTableGroup subGroup in group.Groups)
            {
                ProcessTagTableGroup(subGroup, allTags, ref processedTables, totalTables);
            }
        }

        /// <summary>
        /// Обрабатывает отдельную таблицу тегов
        /// </summary>
        private void ProcessTagTable(PlcTagTable tagTable, List<PlcTagInfo> allTags)
        {
            try
            {
                _logger.Debug($"Обработка таблицы тегов: {tagTable.Name}");

                // Получаем все теги из таблицы
                foreach (PlcTag tag in tagTable.Tags)
                {
                    allTags.Add(new PlcTagInfo
                    {
                        Name = tag.Name,
                        DataType = tag.DataTypeName,
                        LogicalAddress = tag.LogicalAddress,
                        Comment = tag.Comment?.ToString() ?? "",
                        TableName = tagTable.Name
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке таблицы тегов {tagTable.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспортирует все теги ПЛК в CSV-файл
        /// </summary>
        public bool ExportPlcTagsToCsv(List<PlcTagInfo> tags, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Заголовок CSV
                    writer.WriteLine("Table,Name,DataType,Address,Comment");

                    // Данные
                    foreach (var tag in tags)
                    {
                        // Экранируем кавычками поля, которые могут содержать запятые
                        string comment = tag.Comment?.Replace("\"", "\"\"") ?? "";
                        writer.WriteLine($"\"{tag.TableName}\",\"{tag.Name}\",\"{tag.DataType}\",\"{tag.LogicalAddress}\",\"{comment}\"");
                    }
                }

                _logger.Info($"Теги успешно экспортированы в файл: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при экспорте тегов в CSV: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Методы для работы с DB

        /// <summary>
        /// Проверяет, является ли блок данных оптимизированным
        /// </summary>
        /// <param name="db">Блок данных</param>
        /// <returns>true, если блок оптимизирован, иначе false</returns>
        /// <summary>
        /// Проверяет, является ли блок данных оптимизированным
        /// </summary>
        /// <param name="db">Блок данных</param>
        /// <returns>true, если блок оптимизирован, иначе false</returns>
        private bool IsDbOptimized(GlobalDB db)
        {
            if (db == null)
                return false;

            try
            {
                // Метод 1: Прямая проверка свойства MemoryLayout
                var propInfo = db.GetType().GetProperty("MemoryLayout");
                if (propInfo != null)
                {
                    var memoryLayout = propInfo.GetValue(db);
                    // Проверяем, является ли значение MemoryLayout.Optimized
                    // Так как мы не можем напрямую использовать перечисление, сравниваем строки
                    return memoryLayout.ToString() == "Optimized";
                }

                // Альтернативные методы, если первый не сработал
                propInfo = db.GetType().GetProperty("IsOptimizedBlockAccess");
                if (propInfo != null)
                {
                    return (bool)propInfo.GetValue(db);
                }

                propInfo = db.GetType().GetProperty("OptimizedBlockAccess");
                if (propInfo != null)
                {
                    return (bool)propInfo.GetValue(db);
                }

                // По умолчанию предполагаем, что блок не оптимизирован
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при определении оптимизации DB: {ex.Message}");
                return false;
            }
        }
        // Определяем дополнительные методы доступные для Global DB

        private void LogDbCapabilities(GlobalDB db)
        {
            try
            {
                _logger.Debug("=== Доступные методы и свойства для GlobalDB ===");

                _logger.Debug("Методы:");
                foreach (var method in db.GetType().GetMethods()
                    .Where(m => !m.IsSpecialName)
                    .OrderBy(m => m.Name))
                {
                    var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    _logger.Debug($"  {method.ReturnType.Name} {method.Name}({parameters})");
                }

                _logger.Debug("Свойства:");
                foreach (var prop in db.GetType().GetProperties().OrderBy(p => p.Name))
                {
                    try
                    {
                        var value = prop.GetValue(db);
                        _logger.Debug($"  {prop.PropertyType.Name} {prop.Name} = {value}");
                    }
                    catch
                    {
                        _logger.Debug($"  {prop.PropertyType.Name} {prop.Name} = <ошибка доступа>");
                    }
                }

                _logger.Debug("=========================================");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при анализе возможностей DB: {ex.Message}");
            }
        }
        /// <summary>
        /// Получение всех блоков данных для указанного ПЛК
        /// </summary>
        /// <param name="plcSoftware">Программное обеспечение ПЛК</param>
        /// <returns>Список информации о DB</returns>
        /// 

        public List<DbInfo> GetAllDataBlocks(PlcSoftware plcSoftware)
        {
            List<DbInfo> dbList = new List<DbInfo>();

            if (!_isConnected || plcSoftware == null)
            {
                _logger.Error("Нет подключения к TIA Portal или не выбран ПЛК.");
                return dbList;
            }

            try
            {
                // Получаем корневую группу блоков
                PlcBlockGroup blockGroup = plcSoftware.BlockGroup;

                // Список для хранения всех блоков (для отображения прогресса)
                List<PlcBlock> allBlocks = GetAllBlocks(blockGroup);
                int totalBlocks = allBlocks.Count;
                int processedBlocks = 0;

                // Обрабатываем блоки
                foreach (PlcBlock block in blockGroup.Blocks)
                {
                    if (block is GlobalDB db)
                    {

                        LogDbCapabilities(db);

                        dbList.Add(new DbInfo
                        {
                            Name = db.Name,
                            Number = db.Number,
                            IsOptimized = IsDbOptimized(db),
                            Path = "/", // Корневая группа
                            Instance = db
                        });
                    }

                    processedBlocks++;
                    ReportProgress(processedBlocks * 100 / totalBlocks, $"Обработка блока {block.Name}");
                }

                // Рекурсивно обрабатываем блоки в подгруппах
                ProcessBlockGroups(blockGroup.Groups, dbList, "/", ref processedBlocks, totalBlocks);

                ReportProgress(100, "Загрузка блоков данных завершена");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении блоков данных: {ex.Message}");
            }

            return dbList;
        }

        /// <summary>
        /// Асинхронно получает все блоки данных для указанного ПЛК
        /// </summary>
        public async Task<List<DbInfo>> GetAllDataBlocksAsync(PlcSoftware plcSoftware)
        {
            return await Task.Run(() => GetAllDataBlocks(plcSoftware));
        }

        /// <summary>
        /// Получает все блоки рекурсивно
        /// </summary>
        private List<PlcBlock> GetAllBlocks(PlcBlockGroup blockGroup)
        {
            List<PlcBlock> blocks = new List<PlcBlock>();

            // Добавляем блоки из текущей группы
            blocks.AddRange(blockGroup.Blocks);

            // Рекурсивно добавляем блоки из подгрупп
            foreach (PlcBlockGroup subGroup in blockGroup.Groups)
            {
                blocks.AddRange(GetAllBlocksFromGroup(subGroup));
            }

            return blocks;
        }

        /// <summary>
        /// Получает все блоки из группы рекурсивно
        /// </summary>
        private List<PlcBlock> GetAllBlocksFromGroup(PlcBlockGroup group)
        {
            List<PlcBlock> blocks = new List<PlcBlock>();

            // Добавляем блоки из текущей группы
            blocks.AddRange(group.Blocks);

            // Рекурсивно добавляем блоки из подгрупп
            foreach (PlcBlockGroup subGroup in group.Groups)
            {
                blocks.AddRange(GetAllBlocksFromGroup(subGroup));
            }

            return blocks;
        }

        /// <summary>
        /// Рекурсивно обрабатывает группы блоков
        /// </summary>
        private void ProcessBlockGroups(PlcBlockUserGroupComposition groups, List<DbInfo> dbList, string parentPath, ref int processedBlocks, int totalBlocks)
        {
            foreach (PlcBlockUserGroup group in groups)
            {
                string currentPath = parentPath + group.Name + "/";

                // Обрабатываем блоки в текущей группе
                foreach (PlcBlock block in group.Blocks)
                {
                    if (block is GlobalDB db)
                    {
                        // Проверяем оптимизацию DB через reflection
                        bool isOptimized = false;
                        try
                        {
                            var propInfo = db.GetType().GetProperty("IsOptimizedBlockAccess");
                            if (propInfo != null)
                            {
                                isOptimized = (bool)propInfo.GetValue(db);
                            }
                        }
                        catch
                        {
                            // Игнорируем ошибки, предполагаем неоптимизированный DB
                        }

                        dbList.Add(new DbInfo
                        {
                            Name = db.Name,
                            Number = db.Number,
                            IsOptimized = isOptimized,
                            Path = currentPath,
                            Instance = db
                        });
                    }

                    processedBlocks++;
                    ReportProgress(processedBlocks * 100 / totalBlocks, $"Обработка блока {block.Name}");
                }

                // Рекурсивно обрабатываем подгруппы
                ProcessBlockGroups(group.Groups, dbList, currentPath, ref processedBlocks, totalBlocks);
            }
        }

        /// <summary>
        /// Получает теги из блока данных с учетом оптимизации
        /// </summary>
        /// <param name="db">Блок данных</param>
        /// <returns>Список тегов DB</returns>
        /// <summary>
        /// Получает теги из блока данных
        /// </summary>
        /// <param name="db">Блок данных</param>
        /// <returns>Список тегов DB</returns>
        public List<DbTagInfo> GetDataBlockTags(GlobalDB db)
        {
            List<DbTagInfo> tags = new List<DbTagInfo>();

            if (!_isConnected || db == null)
            {
                _logger.Error("Нет подключения к TIA Portal или не выбран блок данных.");
                return tags;
            }

            try
            {
                bool isOptimized = IsDbOptimized(db);
                _logger.Info($"Получение тегов из DB {db.Name} (оптимизирован: {isOptimized})");

                // Получаем интерфейс блока данных через reflection
                var interfaceProp = db.GetType().GetProperty("Interface");
                if (interfaceProp != null)
                {
                    var dbInterface = interfaceProp.GetValue(db);
                    if (dbInterface != null)
                    {
                        // Получаем доступ к Members
                        var membersProp = dbInterface.GetType().GetProperty("Members");
                        if (membersProp != null)
                        {
                            var members = membersProp.GetValue(dbInterface) as System.Collections.IEnumerable;
                            if (members != null)
                            {
                                foreach (var member in members)
                                {
                                    try
                                    {
                                        // Получаем имя и тип данных переменной
                                        var nameProp = member.GetType().GetProperty("Name");
                                        if (nameProp != null)
                                        {
                                            string name = nameProp.GetValue(member)?.ToString();
                                            string dataType = "Unknown";

                                            // Получаем тип данных через метод GetAttribute
                                            var getAttributeMethod = member.GetType().GetMethod("GetAttribute", new Type[] { typeof(string) });
                                            if (getAttributeMethod != null)
                                            {
                                                var dataTypeAttr = getAttributeMethod.Invoke(member, new object[] { "DataTypeName" });
                                                if (dataTypeAttr != null)
                                                {
                                                    dataType = dataTypeAttr.ToString();
                                                }
                                            }

                                            if (!string.IsNullOrEmpty(name))
                                            {
                                                tags.Add(new DbTagInfo
                                                {
                                                    DbName = db.Name,
                                                    Name = name,
                                                    DataType = dataType,
                                                    IsOptimized = isOptimized,
                                                    Offset = isOptimized ? "" : "Standard" // В оптимизированных блоках нет смещения
                                                });

                                                _logger.Debug($"Добавлен тег DB: {name}, тип: {dataType}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.Error($"Ошибка при обработке тега DB: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }

                if (tags.Count == 0)
                {
                    _logger.Warn($"Не найдено тегов в DB {db.Name}. Попробуем использовать экспорт в XML...");
                    // Здесь можно оставить запасной вариант с экспортом в XML, если метод с Interface не сработал
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при извлечении тегов из DB {db.Name}: {ex.Message}");
            }

            return tags;
        }        /// <summary>
                 /// Асинхронно получает теги из блока данных
                 /// </summary>
        public async Task<List<DbTagInfo>> GetDataBlockTagsAsync(GlobalDB db)
        {
            return await Task.Run(() => GetDataBlockTags(db));
        }

        /// <summary>
        /// Получает теги из оптимизированного DB через экспорт в XML
        /// </summary>
        private List<DbTagInfo> GetTagsFromOptimizedDb(GlobalDB db)
        {
            List<DbTagInfo> tags = new List<DbTagInfo>();
            string tempPath = Path.Combine(Path.GetTempPath(), $"{db.Name}_{Guid.NewGuid()}.xml");

            try
            {
                _logger.Info($"Экспорт DB {db.Name} в XML...");

                // Проверяем, какие методы Export доступны
                var exportMethods = db.GetType().GetMethods()
                    .Where(m => m.Name == "Export")
                    .ToList();

                _logger.Debug($"Найдено {exportMethods.Count} методов Export");

                bool exportSuccess = false;

                // Пробуем все варианты экспорта
                // Вариант 1: Export с FileInfo
                try
                {
                    db.Export(new FileInfo(tempPath), ExportOptions.WithDefaults);
                    exportSuccess = true;
                    _logger.Debug("Успешный экспорт через Export(FileInfo)");
                }
                catch (Exception ex1)
                {
                    _logger.Debug($"Ошибка Export(FileInfo): {ex1.Message}");

                    // Вариант 2: Export со строкой пути
                    try
                    {
                        // Используем reflection для вызова метода Export со строковым параметром
                        var exportMethod = db.GetType().GetMethod("Export", new[] { typeof(string) });
                        if (exportMethod != null)
                        {
                            exportMethod.Invoke(db, new object[] { tempPath });
                            exportSuccess = true;
                            _logger.Debug("Успешный экспорт через Export(string)");
                        }
                    }
                    catch (Exception ex2)
                    {
                        _logger.Debug($"Ошибка Export(string): {ex2.Message}");

                        // Если все не получилось, пробуем другие варианты...
                        _logger.Error("Не удалось экспортировать DB. Попробуйте экспортировать его вручную из TIA Portal.");
                    }
                }

                // Если экспорт успешен, анализируем файл
                if (exportSuccess && File.Exists(tempPath))
                {
                    // Анализ XML и остальной код...
                    // (ваш существующий код для парсинга XML остается без изменений)
                }
                else
                {
                    _logger.Error($"Файл экспорта не создан: {tempPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Общая ошибка при работе с DB: {ex.Message}");
            }
            finally
            {
                // Удаляем временный файл
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Игнорируем ошибки при удалении
                    }
                }
            }

            return tags;
        }

        /// <summary>
        /// Получает теги из стандартного (неоптимизированного) DB через экспорт в XML
        /// </summary>
        private List<DbTagInfo> GetTagsFromStandardDb(GlobalDB db)
        {
            List<DbTagInfo> tags = new List<DbTagInfo>();
            string tempPath = Path.Combine(Path.GetTempPath(), $"{db.Name}_{Guid.NewGuid()}.xml");

            try
            {
                // Настраиваем опции экспорта
                ExportOptions options = ExportOptions.WithDefaults; // new ExportOptions();
                //options.ExportDefaultValues = true;


                // Экспортируем блок в XML
                db.Export(new FileInfo(tempPath), options);
                _logger.Debug($"DB {db.Name} экспортирован во временный файл {tempPath}");

                // Загружаем XML для парсинга
                XDocument doc = XDocument.Load(tempPath);

                // Для неоптимизированных блоков ищем элементы с атрибутом Offset
                var elements = doc.Descendants()
                    .Where(e => e.Attribute("Offset") != null)
                    .ToList();

                _logger.Debug($"Найдено {elements.Count} элементов с атрибутом Offset в DB {db.Name}");

                // Обрабатываем каждый элемент
                foreach (var element in elements)
                {
                    string name = element.Attribute("Name")?.Value;
                    string dataType = element.Attribute("Datatype")?.Value;
                    string offset = element.Attribute("Offset")?.Value;

                    // Пропускаем элементы без имени
                    if (string.IsNullOrEmpty(name))
                        continue;

                    // Получаем путь в иерархии для структурированных типов
                    string path = GetElementPath(element);

                    tags.Add(new DbTagInfo
                    {
                        DbName = db.Name,
                        Name = name,
                        DataType = dataType ?? "Unknown",
                        Offset = offset ?? "0",
                        IsOptimized = false,
                        Path = path
                    });

                    _logger.Debug($"Добавлен тег: {name}, тип: {dataType}, смещение: {offset}, путь: {path}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при извлечении тегов из стандартного DB {db.Name}: {ex.Message}");
            }
            finally
            {
                // Удаляем временный файл
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                        _logger.Debug($"Временный файл {tempPath} удален");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при удалении временного файла: {ex.Message}");
                    }
                }
            }

            return tags;
        }

        /// <summary>
        /// Получает путь к элементу в XML иерархии
        /// </summary>
        private string GetElementPath(XElement element)
        {
            List<string> pathParts = new List<string>();
            XElement current = element;

            // Поднимаемся по иерархии XML и собираем имена родительских элементов
            while (current.Parent != null &&
                   (current.Parent.Name.LocalName == "Section" || current.Parent.Name.LocalName == "Member") &&
                   current.Parent.Attribute("Name") != null)
            {
                current = current.Parent;
                pathParts.Add(current.Attribute("Name").Value);
            }

            // Разворачиваем список для получения правильного порядка
            pathParts.Reverse();

            // Если путь пустой, возвращаем null
            if (pathParts.Count == 0)
                return null;

            // Соединяем части пути через точку
            return string.Join(".", pathParts);
        }

        /// <summary>
        /// Экспортирует теги DB в CSV-файл
        /// </summary>
        public bool ExportDbTagsToCsv(List<DbTagInfo> tags, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Заголовок CSV
                    writer.WriteLine("DB,Path,Name,DataType,Offset,IsOptimized");

                    // Данные
                    foreach (var tag in tags)
                    {
                        // Экранируем кавычками поля, которые могут содержать запятые
                        string path = tag.Path?.Replace("\"", "\"\"") ?? "";
                        writer.WriteLine($"\"{tag.DbName}\",\"{path}\",\"{tag.Name}\",\"{tag.DataType}\",\"{tag.Offset}\",\"{tag.IsOptimized}\"");
                    }
                }

                _logger.Info($"Теги DB успешно экспортированы в файл: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при экспорте тегов DB в CSV: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Отправляет уведомление о прогрессе
        /// </summary>
        private void ReportProgress(int percent, string message)
        {
            try
            {
                ProgressChanged?.Invoke(percent, message);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отправке уведомления о прогрессе: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет, подключен ли TIA Portal, и выбрасывает исключение, если нет
        /// </summary>
        private void EnsureConnected()
        {
            if (!_isConnected || _project == null)
            {
                throw new InvalidOperationException("Нет подключения к TIA Portal. Сначала вызовите метод Connect().");
            }
        }

        #endregion

        #region IDisposable реализация

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_isConnected)
                {
                    Disconnect();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при освобождении ресурсов: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Интерфейс для логирования
    /// </summary>
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}