//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Siemens.Engineering;
//using Siemens.Engineering.SW;
//using SiemensTrend.Core.Logging;
//using SiemensTrend.Core.Models;
//using System.Linq;
//using SiemensTrend.Helpers;
//using System.IO;
////using Siemens.Collaboration.Net.Logging;

//namespace SiemensTrend.Communication.TIA
//{
//    /// <summary>
//    /// Сервис для коммуникации с TIA Portal (исправленная версия)
//    /// </summary>
//    public partial class TiaPortalCommunicationService
//    {
//        private readonly Logger _logger;
//        private TiaPortal _tiaPortal;
//        private Project _project;
//        private bool _isConnected;
//        private TiaPortalTagReader _tagReader;

//        /// <summary>
//        /// Флаг подключения к TIA Portal
//        /// </summary>
//        public bool IsConnected => _isConnected;

//        /// <summary>
//        /// Текущий проект TIA Portal
//        /// </summary>
//        public Project CurrentProject => _project;

//        /// <summary>
//        /// Конструктор
//        /// </summary>
//        public TiaPortalCommunicationService(Logger logger)
//        {
//            _logger = logger;
//            _logger.Info("Создание экземпляра TiaPortalCommunicationService");
//            _xmlManager = new TiaPortalXmlManager(_logger);
//            _logger.Info("Экземпляр TiaPortalCommunicationService создан успешно");
//        }

//        /// <summary>
//        /// Получение списка открытых проектов TIA Portal
//        /// </summary>
//        public List<TiaProjectInfo> GetOpenProjects()
//        {
//            List<TiaProjectInfo> projects = new List<TiaProjectInfo>();

//            try
//            {
//                _logger.Info("Получение списка открытых проектов TIA Portal");
//                var tiaProcesses = TiaPortal.GetProcesses();
//                _logger.Info($"Найдено {tiaProcesses.Count} процессов TIA Portal");

//                foreach (var process in tiaProcesses)
//                {
//                    try
//                    {
//                        // Подключаемся к процессу
//                        _logger.Info($"Подключение к процессу TIA Portal {process.Id}");
//                        var tiaPortal = process.Attach();
//                        _logger.Info($"Успешное подключение к процессу TIA Portal {process.Id}");

//                        // Проверяем, есть ли открытый проект
//                        int projectCount = tiaPortal.Projects.Count;
//                        _logger.Info($"В процессе TIA Portal {process.Id} найдено {projectCount} проектов");

//                        if (projectCount > 0)
//                        {
//                            foreach (var project in tiaPortal.Projects)
//                            {
//                                try
//                                {
//                                    var projectInfo = new TiaProjectInfo
//                                    {
//                                        Name = project.Name,
//                                        Path = project.Path.ToString(),
//                                        TiaProcess = process,
//                                        TiaPortalInstance = tiaPortal,
//                                        Project = project
//                                    };

//                                    projects.Add(projectInfo);
//                                    _logger.Info($"Найден открытый проект: {project.Name} в процессе {process.Id}");
//                                }
//                                catch (Exception ex)
//                                {
//                                    _logger.Error($"Ошибка при получении информации о проекте: {ex.Message}");
//                                }
//                            }
//                        }
//                        else
//                        {
//                            _logger.Info($"В процессе TIA Portal {process.Id} нет открытых проектов");
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при подключении к процессу TIA Portal {process.Id}: {ex.Message}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении списка открытых проектов TIA Portal: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//            }

//            return projects;
//        }

//        /// <summary>
//        /// Подключение к выбранному проекту
//        /// </summary>
//        public bool ConnectToProject(TiaProjectInfo projectInfo)
//        {
//            try
//            {
//                // Проверяем входные параметры
//                if (projectInfo == null)
//                {
//                    _logger.Error("ConnectToProject: projectInfo не может быть null");
//                    return false;
//                }

//                if (projectInfo.Project == null)
//                {
//                    _logger.Error("ConnectToProject: projectInfo.Project не может быть null");
//                    return false;
//                }

//                if (projectInfo.TiaPortalInstance == null)
//                {
//                    _logger.Error("ConnectToProject: projectInfo.TiaPortalInstance не может быть null");
//                    return false;
//                }

//                _logger.Info($"ConnectToProject: Подключение к проекту: {projectInfo.Name}");

//                // Если уже подключены к какому-то проекту, сначала отключаемся
//                if (_isConnected || _project != null || _tiaPortal != null)
//                {
//                    _logger.Info("ConnectToProject: Обнаружено активное подключение, выполняем отключение");
//                    Disconnect();
//                }

//                // Сохраняем ссылки на объекты TIA Portal
//                _tiaPortal = projectInfo.TiaPortalInstance;
//                _project = projectInfo.Project;

//                // Немедленно проверяем, что _project не null
//                if (_project == null)
//                {
//                    _logger.Error("ConnectToProject: После присвоения _project оказался null");
//                    return false;
//                }

//                // Устанавливаем текущий проект для XML-менеджера
//                SetCurrentProjectInXmlManager();

//                // Логируем информацию о проекте
//                try
//                {
//                    _logger.Info($"ConnectToProject: Проект: {_project.Name}, Путь: {_project.Path}");
//                }
//                catch (Exception ex)
//                {
//                    _logger.Error($"ConnectToProject: Не удалось получить информацию о проекте: {ex.Message}");
//                    // Не прерываем выполнение, так как это не критично
//                }

//                // Проверяем доступность проекта, пытаясь обратиться к его свойствам
//                try
//                {
//                    // Пытаемся обратиться к свойствам проекта для проверки
//                    var devices = _project.Devices.Count;
//                    _logger.Info($"ConnectToProject: Количество устройств в проекте: {devices}");
//                }
//                catch (Exception ex)
//                {
//                    _logger.Error($"ConnectToProject: Ошибка при проверке свойств проекта: {ex.Message}");
//                    Disconnect(); // Отключаемся при ошибке
//                    return false;
//                }

//                // Создаем читатель тегов только после успешной проверки проекта
//                try
//                {
//                    _tagReader = new TiaPortalTagReader(_logger, this);
//                    _logger.Info("ConnectToProject: TiaPortalTagReader создан успешно");
//                }
//                catch (Exception ex)
//                {
//                    _logger.Error($"ConnectToProject: Ошибка при создании TiaPortalTagReader: {ex.Message}");
//                    Disconnect(); // Отключаемся при ошибке
//                    return false;
//                }

//                // Устанавливаем флаг подключения
//                _isConnected = true;
//                _logger.Info($"ConnectToProject: Успешное подключение к проекту: {_project.Name}");

//                // Даем немного времени для завершения операций в UI
//                System.Windows.Forms.Application.DoEvents();

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"ConnectToProject: Ошибка при подключении к проекту: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"ConnectToProject: Внутренняя ошибка: {ex.InnerException.Message}");
//                }

//                // Сбрасываем состояние в случае ошибки
//                _tiaPortal = null;
//                _project = null;
//                _isConnected = false;
//                return false;
//            }
//        }

//        /// <summary>
//        /// Отключение от TIA Portal
//        /// </summary>
//        public void Disconnect()
//        {
//            try
//            {
//                _logger.Info("Disconnect: Отключение от TIA Portal");

//                // Освобождаем ресурсы
//                _tagReader = null;
//                _project = null;
//                _tiaPortal = null;
//                _isConnected = false;

//                // Принудительно запускаем сборщик мусора для освобождения COM-объектов
//                GC.Collect();
//                GC.WaitForPendingFinalizers();

//                _logger.Info("Disconnect: Отключение от TIA Portal выполнено успешно");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Disconnect: Ошибка при отключении от TIA Portal: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"Disconnect: Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//            }
//        }

//        /// <summary>
//        /// Получение PlcSoftware из проекта
//        /// </summary>
//        public PlcSoftware GetPlcSoftware()
//        {
//            // Проверяем, есть ли активное подключение к проекту
//            if (_project == null)
//            {
//                _logger.Error("GetPlcSoftware: Проект TIA Portal не открыт");
//                return null;
//            }

//            _logger.Info($"GetPlcSoftware: Поиск PLC Software в проекте {_project.Name}");

//            try
//            {
//                // Проверяем наличие устройств в проекте
//                if (_project.Devices == null || _project.Devices.Count == 0)
//                {
//                    _logger.Error("GetPlcSoftware: В проекте нет устройств");
//                    return null;
//                }

//                _logger.Info($"GetPlcSoftware: Количество устройств в проекте: {_project.Devices.Count}");

//                // Перебираем все устройства в проекте
//                foreach (var device in _project.Devices)
//                {
//                    try
//                    {
//                        _logger.Info($"GetPlcSoftware: Проверка устройства {device.Name} (Тип: {device.TypeIdentifier})");

//                        // Проверяем элементы устройства
//                        if (device.DeviceItems == null || device.DeviceItems.Count == 0)
//                        {
//                            _logger.Info($"GetPlcSoftware: Устройство {device.Name} не содержит элементов");
//                            continue;
//                        }

//                        _logger.Info($"GetPlcSoftware: Количество элементов устройства {device.Name}: {device.DeviceItems.Count}");

//                        // Перебираем элементы устройства
//                        foreach (var deviceItem in device.DeviceItems)
//                        {
//                            try
//                            {
//                                _logger.Info($"GetPlcSoftware: Проверка элемента устройства {deviceItem.Name}");

//                                // Получаем SoftwareContainer
//                                var softwareContainer = deviceItem.GetService<Siemens.Engineering.HW.Features.SoftwareContainer>();
//                                if (softwareContainer == null)
//                                {
//                                    _logger.Info($"GetPlcSoftware: Элемент {deviceItem.Name} не содержит SoftwareContainer");
//                                    continue;
//                                }

//                                _logger.Info($"GetPlcSoftware: SoftwareContainer найден для элемента {deviceItem.Name}");

//                                // Проверяем, содержит ли SoftwareContainer PlcSoftware
//                                if (softwareContainer.Software is PlcSoftware plcSoftware)
//                                {
//                                    _logger.Info($"GetPlcSoftware: PlcSoftware найден в устройстве {device.Name}, элемент {deviceItem.Name}");
//                                    return plcSoftware;
//                                }
//                                else
//                                {
//                                    _logger.Info($"GetPlcSoftware: Элемент {deviceItem.Name} не содержит PlcSoftware");
//                                }
//                            }
//                            catch (Exception ex)
//                            {
//                                _logger.Error($"GetPlcSoftware: Ошибка при проверке элемента устройства {deviceItem.Name}: {ex.Message}");
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"GetPlcSoftware: Ошибка при проверке устройства {device.Name}: {ex.Message}");
//                    }
//                }

//                _logger.Error("GetPlcSoftware: PlcSoftware не найден ни в одном устройстве проекта");
//                return null;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"GetPlcSoftware: Ошибка при поиске PlcSoftware: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"GetPlcSoftware: Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//                return null;
//            }
//        }

//        /// <summary>
//        /// Открытие проекта TIA Portal (синхронный метод)
//        /// </summary>
//        public bool OpenProjectSync(string projectPath)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(projectPath))
//                {
//                    _logger.Error("OpenProjectSync: путь к проекту не может быть пустым");
//                    return false;
//                }

//                _logger.Info($"OpenProjectSync: Открытие проекта TIA Portal: {projectPath}");

//                // Если уже подключены к какому-то проекту, сначала отключаемся
//                if (_isConnected || _project != null || _tiaPortal != null)
//                {
//                    _logger.Info("OpenProjectSync: Обнаружено активное подключение, выполняем отключение");
//                    Disconnect();
//                }

//                // Создаем новый экземпляр TIA Portal с пользовательским интерфейсом
//                _logger.Info("OpenProjectSync: Создание нового экземпляра TIA Portal");
//                _tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface);
//                _logger.Info("OpenProjectSync: Экземпляр TIA Portal создан успешно");

//                // Открываем проект
//                bool openResult = false;

//                try
//                {
//                    _logger.Info($"OpenProjectSync: Попытка открыть проект {projectPath}");

//                    // Проверяем существование файла проекта
//                    if (!System.IO.File.Exists(projectPath))
//                    {
//                        _logger.Error($"OpenProjectSync: Файл проекта не существует: {projectPath}");
//                        return false;
//                    }

//                    // Создаем FileInfo для проекта
//                    var projectFile = new System.IO.FileInfo(projectPath);

//                    // Открываем проект напрямую (синхронно)
//                    _project = _tiaPortal.Projects.Open(projectFile);

//                    // Обработка UI-событий во время открытия проекта
//                    System.Windows.Forms.Application.DoEvents();

//                    // Если проект успешно открыт
//                    if (_project != null)
//                    {
//                        _logger.Info($"OpenProjectSync: Проект успешно открыт: {_project.Name}");

//                        // Устанавливаем текущий проект для XML-менеджера
//                        SetCurrentProjectInXmlManager();

//                        openResult = true;
//                    }
//                    else
//                    {
//                        _logger.Error("OpenProjectSync: Проект не удалось открыть, Projects.Open вернул null");
//                        openResult = false;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.Error($"OpenProjectSync: Ошибка при открытии проекта: {ex.Message}");
//                    if (ex.InnerException != null)
//                    {
//                        _logger.Error($"OpenProjectSync: Внутренняя ошибка: {ex.InnerException.Message}");
//                    }
//                    openResult = false;

//                    // Освобождаем ресурсы при ошибке
//                    try
//                    {
//                        if (_tiaPortal != null)
//                        {
//                            _tiaPortal.Dispose();
//                            _tiaPortal = null;
//                        }
//                    }
//                    catch { }

//                    return false;
//                }

//                if (openResult)
//                {
//                    // Создаем читатель тегов после успешного открытия проекта
//                    try
//                    {
//                        _tagReader = new TiaPortalTagReader(_logger, this);
//                        _logger.Info("OpenProjectSync: TiaPortalTagReader создан успешно");
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"OpenProjectSync: Ошибка при создании TiaPortalTagReader: {ex.Message}");
//                        return false;
//                    }

//                    _isConnected = true;
//                    _logger.Info($"OpenProjectSync: Проект успешно открыт: {_project.Name}");
//                    return true;
//                }
//                else
//                {
//                    _isConnected = false;
//                    _tiaPortal = null;
//                    _project = null;
//                    return false;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"OpenProjectSync: Ошибка при открытии проекта TIA Portal: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"OpenProjectSync: Внутренняя ошибка: {ex.InnerException.Message}");
//                }

//                // Сбрасываем состояние в случае ошибки
//                _isConnected = false;
//                _tiaPortal = null;
//                _project = null;
//                return false;
//            }
//        }


//        /// <summary>
//        /// Открытие Созlадем xml manager
//        /// </summary>
//        private TiaPortalXmlManager _xmlManager;

//        /// <summary>
//        /// Установка текущего проекта для работы с XML
//        /// </summary>
//        private void SetCurrentProjectInXmlManager()
//        {
//            if (_project != null)
//            {
//                try
//                {
//                    string projectName = _project.Name;
//                    _xmlManager.SetCurrentProject(projectName);
//                    _logger.Info($"SetCurrentProjectInXmlManager: Установлен проект {projectName} для работы с XML");
//                }
//                catch (Exception ex)
//                {
//                    _logger.Error($"SetCurrentProjectInXmlManager: Ошибка при установке текущего проекта: {ex.Message}");
//                }
//            }
//            else
//            {
//                _logger.Warn("SetCurrentProjectInXmlManager: Нет активного проекта");
//            }
//        }

//        /// <summary>
//        /// "Экспортируем теги в xml"
//        /// </summary>
//        public async Task ExportTagsToXml()
//        {
//            if (!IsConnected || _project == null)
//            {
//                _logger.Error("ExportTagsToXml: Нет подключения к TIA Portal");
//                return;
//            }

//            var plcSoftware = GetPlcSoftware();
//            if (plcSoftware == null) return;

//            await _xmlManager.ExportTagsToXml(plcSoftware);
//        }

//        /// <summary>
//        /// Получение только тегов ПЛК из проекта
//        /// </summary>

//        public async Task<List<TagDefinition>> GetPlcTagsAsync()
//        {
//            try
//            {
//                // Убедимся, что текущий проект установлен в XML менеджере
//                if (CurrentProject != null && !string.IsNullOrEmpty(CurrentProject.Name))
//                {
//                    _xmlManager.SetCurrentProject(CurrentProject.Name);
//                    _logger.Info($"GetPlcTagsAsync: Установлен текущий проект для XML: {CurrentProject.Name}");
//                }

//                // Проверка наличия XML-файлов
//                var tagsFromXml = _xmlManager.LoadPlcTagsFromXml();
//                if (tagsFromXml.Count > 0)
//                {
//                    _logger.Info($"GetPlcTagsAsync: Загружено {tagsFromXml.Count} тегов из XML");
//                    return tagsFromXml;
//                }
//                else
//                {
//                    _logger.Warn($"GetPlcTagsAsync: XML-кэш для проекта {CurrentProject?.Name} пуст или поврежден");
//                }

//                // Если XML нет или они пустые, получаем данные напрямую из TIA Portal
//                if (IsConnected && CurrentProject != null)
//                {
//                    // Проверяем наличие читателя тегов
//                    if (_tagReader == null)
//                    {
//                        _logger.Warn("GetPlcTagsAsync: TiaPortalTagReader не инициализирован, создаем новый экземпляр");
//                        _tagReader = new TiaPortalTagReader(_logger, this);
//                    }

//                    // ВАЖНО: Мы не используем Task.Run() для TIA Portal Openness API!
//                    // Вместо этого мы выполняем операции синхронно в том же STA-потоке
//                    _logger.Info("GetPlcTagsAsync: Синхронное чтение тегов ПЛК из TIA Portal");

//                    try
//                    {
//                        // Вызываем метод чтения тегов напрямую (без Task.Run)
//                        var plcData = _tagReader.ReadAllTags();

//                        // Возвращаем только теги ПЛК
//                        _logger.Info($"GetPlcTagsAsync: Загружено {plcData.PlcTags.Count} тегов ПЛК");
//                        return plcData.PlcTags;
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"GetPlcTagsAsync: Ошибка при чтении тегов: {ex.Message}");
//                        if (ex.InnerException != null)
//                        {
//                            _logger.Error($"GetPlcTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
//                        }
//                        return new List<TagDefinition>();
//                    }
//                }

//                _logger.Error("GetPlcTagsAsync: Нет подключения к TIA Portal и отсутствуют XML");
//                return new List<TagDefinition>();
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"GetPlcTagsAsync: Ошибка: {ex.Message}");
//                return new List<TagDefinition>();
//            }
//        }                 /// Загрузка и возврат всех тегов проекта
//                          /// </summary>
//                          /// <summary>
//                          /// Загрузка и возврат всех тегов проекта
//                          /// </summary>
//        public async Task<PlcData> GetAllProjectTagsAsync()
//        {
//            if (!IsConnected || CurrentProject == null)
//            {
//                _logger.Error("GetAllProjectTagsAsync: Попытка получения тегов без подключения к TIA Portal");
//                return new PlcData();
//            }

//            try
//            {
//                _logger.Info("GetAllProjectTagsAsync: Запуск чтения всех тегов проекта");

//                // Проверяем наличие читателя тегов
//                if (_tagReader == null)
//                {
//                    _logger.Warn("GetAllProjectTagsAsync: TiaPortalTagReader не инициализирован, создаем новый экземпляр");
//                    _tagReader = new TiaPortalTagReader(_logger, this);
//                }

//                // ВАЖНО: Используем синхронный метод ReadAllTags для TIA Portal Openness API
//                // НЕЛЬЗЯ использовать Task.Run() с TIA Portal Openness!
//                _logger.Info("GetAllProjectTagsAsync: Синхронное чтение тегов");
//                PlcData plcData = new PlcData();

//                try
//                {
//                    // Вызываем метод синхронно в текущем STA-потоке
//                    plcData = _tagReader.ReadAllTags();
//                    _logger.Info("GetAllProjectTagsAsync: Метод ReadAllTags() выполнен успешно");
//                }
//                catch (Exception ex)
//                {
//                    _logger.Error($"GetAllProjectTagsAsync: Ошибка при вызове ReadAllTags(): {ex.Message}");
//                    if (ex.InnerException != null)
//                    {
//                        _logger.Error($"GetAllProjectTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
//                    }
//                    return new PlcData();
//                }

//                // Проверяем результаты
//                _logger.Info($"GetAllProjectTagsAsync: Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");
//                return plcData;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"GetAllProjectTagsAsync: Общая ошибка при получении всех тегов проекта: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"GetAllProjectTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//                return new PlcData();
//            }
//        }                 /// Получение только тегов блоков данных из проекта
//                          /// </summary>
//        public async Task<List<TagDefinition>> GetDbTagsAsync()
//        {
//            try
//            {
//                // Убедимся, что текущий проект установлен в XML менеджере
//                if (CurrentProject != null && !string.IsNullOrEmpty(CurrentProject.Name))
//                {
//                    _xmlManager.SetCurrentProject(CurrentProject.Name);
//                    _logger.Info($"GetDbTagsAsync: Установлен текущий проект для XML: {CurrentProject.Name}");
//                }

//                // Проверка наличия XML-файлов
//                var dbsFromXml = _xmlManager.LoadDbTagsFromXml();
//                if (dbsFromXml.Count > 0)
//                {
//                    _logger.Info($"GetDbTagsAsync: Загружено {dbsFromXml.Count} блоков данных из XML");
//                    return dbsFromXml;
//                }
//                else
//                {
//                    _logger.Warn($"GetDbTagsAsync: XML-кэш для блоков данных проекта {CurrentProject?.Name} пуст или поврежден");
//                }

//                // Если XML нет или они пустые, получаем данные напрямую из TIA Portal
//                if (IsConnected && CurrentProject != null)
//                {
//                    // Проверяем наличие читателя тегов
//                    if (_tagReader == null)
//                    {
//                        _logger.Warn("GetDbTagsAsync: TiaPortalTagReader не инициализирован, создаем новый экземпляр");
//                        _tagReader = new TiaPortalTagReader(_logger, this);
//                    }

//                    // ВАЖНО: Мы не используем Task.Run() для TIA Portal Openness API!
//                    // Вместо этого мы выполняем операции синхронно в том же STA-потоке
//                    _logger.Info("GetDbTagsAsync: Синхронное чтение тегов DB из TIA Portal");

//                    try
//                    {
//                        // Вызываем метод чтения тегов напрямую (без Task.Run)
//                        var plcData = _tagReader.ReadAllTags();

//                        // Возвращаем только теги DB
//                        _logger.Info($"GetDbTagsAsync: Загружено {plcData.DbTags.Count} тегов DB");
//                        return plcData.DbTags;
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"GetDbTagsAsync: Ошибка при чтении тегов DB: {ex.Message}");
//                        if (ex.InnerException != null)
//                        {
//                            _logger.Error($"GetDbTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
//                        }
//                        return new List<TagDefinition>();
//                    }
//                }

//                _logger.Error("GetDbTagsAsync: Нет подключения к TIA Portal и отсутствуют XML");
//                return new List<TagDefinition>();
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"GetDbTagsAsync: Ошибка: {ex.Message}");
//                return new List<TagDefinition>();
//            }
//        }                 /// Добавьте это свойство в класс TiaPortalCommunicationService для публичного доступа к XmlManager
//                          /// </summary>
//        public Helpers.TiaPortalXmlManager XmlManager
//        {
//            get { return _xmlManager; }
//        }

//        /// <summary>
//        /// Задание текущего проекта для XML-менеджера
//        /// </summary>
//        /// <param name="projectName">Имя проекта</param>
//        public void SetXmlManagerProject(string projectName)
//        {
//            if (_xmlManager != null && !string.IsNullOrEmpty(projectName))
//            {
//                _xmlManager.SetCurrentProject(projectName);
//                _logger.Info($"SetXmlManagerProject: Установлен проект {projectName}");
//            }
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using System.Linq;
using SiemensTrend.Helpers;
using System.IO;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System.Diagnostics;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Сервис для коммуникации с TIA Portal
    /// </summary>
    public class TiaPortalCommunicationService
    {
        private readonly Logger _logger;
        private TiaPortal _tiaPortal;
        private Project _project;
        private bool _isConnected;
        private TiaPortalTagReader _tagReader;
        private readonly TiaPortalXmlManager _xmlManager;
        private readonly object _tiaPortalLock = new object();

        /// <summary>
        /// Флаг подключения к TIA Portal
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Текущий проект TIA Portal
        /// </summary>
        public Project CurrentProject => _project;

        /// <summary>
        /// XML-менеджер для работы с кэшем
        /// </summary>
        public TiaPortalXmlManager XmlManager => _xmlManager;

        /// <summary>
        /// Конструктор
        /// </summary>
        public TiaPortalCommunicationService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Info("Создание экземпляра TiaPortalCommunicationService");
            _xmlManager = new TiaPortalXmlManager(_logger);
            _logger.Info("Экземпляр TiaPortalCommunicationService создан успешно");
        }

        /// <summary>
        /// Получение списка открытых проектов TIA Portal
        /// </summary>
        public List<TiaProjectInfo> GetOpenProjects()
        {
            List<TiaProjectInfo> projects = new List<TiaProjectInfo>();

            try
            {
                _logger.Info("Получение списка открытых проектов TIA Portal");
                var tiaProcesses = TiaPortal.GetProcesses();
                _logger.Info($"Найдено {tiaProcesses.Count} процессов TIA Portal");

                foreach (var process in tiaProcesses)
                {
                    try
                    {
                        // Подключаемся к процессу
                        _logger.Info($"Подключение к процессу TIA Portal {process.Id}");
                        var tiaPortal = process.Attach();
                        _logger.Info($"Успешное подключение к процессу TIA Portal {process.Id}");

                        // Проверяем, есть ли открытый проект
                        int projectCount = tiaPortal.Projects.Count;
                        _logger.Info($"В процессе TIA Portal {process.Id} найдено {projectCount} проектов");

                        if (projectCount > 0)
                        {
                            foreach (var project in tiaPortal.Projects)
                            {
                                try
                                {
                                    var projectInfo = new TiaProjectInfo
                                    {
                                        Name = project.Name,
                                        Path = project.Path.ToString(),
                                        TiaProcess = process,
                                        TiaPortalInstance = tiaPortal,
                                        Project = project
                                    };

                                    projects.Add(projectInfo);
                                    _logger.Info($"Найден открытый проект: {project.Name} в процессе {process.Id}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.Error($"Ошибка при получении информации о проекте: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            _logger.Info($"В процессе TIA Portal {process.Id} нет открытых проектов");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при подключении к процессу TIA Portal {process.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении списка открытых проектов TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }

            return projects;
        }

        /// <summary>
        /// Подключение к выбранному проекту
        /// </summary>
        public bool ConnectToProject(TiaProjectInfo projectInfo)
        {
            try
            {
                // Проверяем входные параметры
                if (projectInfo == null)
                {
                    _logger.Error("ConnectToProject: projectInfo не может быть null");
                    return false;
                }

                if (projectInfo.Project == null)
                {
                    _logger.Error("ConnectToProject: projectInfo.Project не может быть null");
                    return false;
                }

                if (projectInfo.TiaPortalInstance == null)
                {
                    _logger.Error("ConnectToProject: projectInfo.TiaPortalInstance не может быть null");
                    return false;
                }

                _logger.Info($"ConnectToProject: Подключение к проекту: {projectInfo.Name}");

                // Если уже подключены к какому-то проекту, сначала отключаемся
                if (_isConnected || _project != null || _tiaPortal != null)
                {
                    _logger.Info("ConnectToProject: Обнаружено активное подключение, выполняем отключение");
                    Disconnect();
                }

                // Сохраняем ссылки на объекты TIA Portal
                _tiaPortal = projectInfo.TiaPortalInstance;
                _project = projectInfo.Project;

                // Немедленно проверяем, что _project не null
                if (_project == null)
                {
                    _logger.Error("ConnectToProject: После присвоения _project оказался null");
                    return false;
                }

                // Устанавливаем текущий проект для XML-менеджера
                SetCurrentProjectInXmlManager();

                // Логируем информацию о проекте
                try
                {
                    _logger.Info($"ConnectToProject: Проект: {_project.Name}, Путь: {_project.Path}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ConnectToProject: Не удалось получить информацию о проекте: {ex.Message}");
                    // Не прерываем выполнение, так как это не критично
                }

                // Проверяем доступность проекта, пытаясь обратиться к его свойствам
                try
                {
                    // Пытаемся обратиться к свойствам проекта для проверки
                    var devices = _project.Devices.Count;
                    _logger.Info($"ConnectToProject: Количество устройств в проекте: {devices}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ConnectToProject: Ошибка при проверке свойств проекта: {ex.Message}");
                    Disconnect(); // Отключаемся при ошибке
                    return false;
                }

                // Создаем читатель тегов только после успешной проверки проекта
                try
                {
                    _tagReader = new TiaPortalTagReader(_logger, this);
                    _logger.Info("ConnectToProject: TiaPortalTagReader создан успешно");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ConnectToProject: Ошибка при создании TiaPortalTagReader: {ex.Message}");
                    Disconnect(); // Отключаемся при ошибке
                    return false;
                }

                // Устанавливаем флаг подключения
                _isConnected = true;
                _logger.Info($"ConnectToProject: Успешное подключение к проекту: {_project.Name}");

                // Даем немного времени для завершения операций в UI
                System.Windows.Forms.Application.DoEvents();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"ConnectToProject: Ошибка при подключении к проекту: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"ConnectToProject: Внутренняя ошибка: {ex.InnerException.Message}");
                }

                // Сбрасываем состояние в случае ошибки
                _tiaPortal = null;
                _project = null;
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
                _logger.Info("Disconnect: Отключение от TIA Portal");

                // Освобождаем ресурсы
                _tagReader = null;
                _project = null;
                _tiaPortal = null;
                _isConnected = false;

                // Принудительно запускаем сборщик мусора для освобождения COM-объектов
                GC.Collect();
                GC.WaitForPendingFinalizers();

                _logger.Info("Disconnect: Отключение от TIA Portal выполнено успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"Disconnect: Ошибка при отключении от TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Disconnect: Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Получение PlcSoftware из проекта
        /// </summary>
        public PlcSoftware GetPlcSoftware()
        {
            // Проверяем, есть ли активное подключение к проекту
            if (_project == null)
            {
                _logger.Error("GetPlcSoftware: Проект TIA Portal не открыт");
                return null;
            }

            _logger.Info($"GetPlcSoftware: Поиск PLC Software в проекте {_project.Name}");

            try
            {
                // Проверяем наличие устройств в проекте
                if (_project.Devices == null || _project.Devices.Count == 0)
                {
                    _logger.Error("GetPlcSoftware: В проекте нет устройств");
                    return null;
                }

                _logger.Info($"GetPlcSoftware: Количество устройств в проекте: {_project.Devices.Count}");

                // Перебираем все устройства в проекте
                foreach (var device in _project.Devices)
                {
                    try
                    {
                        _logger.Info($"GetPlcSoftware: Проверка устройства {device.Name} (Тип: {device.TypeIdentifier})");

                        // Проверяем элементы устройства
                        if (device.DeviceItems == null || device.DeviceItems.Count == 0)
                        {
                            _logger.Info($"GetPlcSoftware: Устройство {device.Name} не содержит элементов");
                            continue;
                        }

                        _logger.Info($"GetPlcSoftware: Количество элементов устройства {device.Name}: {device.DeviceItems.Count}");

                        // Перебираем элементы устройства
                        foreach (var deviceItem in device.DeviceItems)
                        {
                            try
                            {
                                _logger.Info($"GetPlcSoftware: Проверка элемента устройства {deviceItem.Name}");

                                // Получаем SoftwareContainer
                                var softwareContainer = deviceItem.GetService<SoftwareContainer>();
                                if (softwareContainer == null)
                                {
                                    _logger.Info($"GetPlcSoftware: Элемент {deviceItem.Name} не содержит SoftwareContainer");
                                    continue;
                                }

                                _logger.Info($"GetPlcSoftware: SoftwareContainer найден для элемента {deviceItem.Name}");

                                // Проверяем, содержит ли SoftwareContainer PlcSoftware
                                if (softwareContainer.Software is PlcSoftware plcSoftware)
                                {
                                    _logger.Info($"GetPlcSoftware: PlcSoftware найден в устройстве {device.Name}, элемент {deviceItem.Name}");
                                    return plcSoftware;
                                }
                                else
                                {
                                    _logger.Info($"GetPlcSoftware: Элемент {deviceItem.Name} не содержит PlcSoftware");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"GetPlcSoftware: Ошибка при проверке элемента устройства {deviceItem.Name}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"GetPlcSoftware: Ошибка при проверке устройства {device.Name}: {ex.Message}");
                    }
                }

                _logger.Error("GetPlcSoftware: PlcSoftware не найден ни в одном устройстве проекта");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetPlcSoftware: Ошибка при поиске PlcSoftware: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"GetPlcSoftware: Внутренняя ошибка: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// Открытие проекта TIA Portal (синхронный метод)
        /// </summary>
        /// <param name="projectPath">Путь к файлу проекта TIA Portal</param>
        /// <returns>True если проект успешно открыт</returns>
        public bool OpenProjectSync(string projectPath)
        {
            try
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    _logger.Error("OpenProjectSync: путь к проекту не может быть пустым");
                    return false;
                }

                _logger.Info($"OpenProjectSync: Открытие проекта TIA Portal: {projectPath}");

                // Если уже подключены к какому-то проекту, сначала отключаемся
                if (_isConnected || _project != null || _tiaPortal != null)
                {
                    _logger.Info("OpenProjectSync: Обнаружено активное подключение, выполняем отключение");
                    Disconnect();
                }

                // Создаем новый экземпляр TIA Portal с пользовательским интерфейсом
                _logger.Info("OpenProjectSync: Создание нового экземпляра TIA Portal");
                _tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface);
                _logger.Info("OpenProjectSync: Экземпляр TIA Portal создан успешно");

                // Открываем проект
                bool openResult = false;

                try
                {
                    _logger.Info($"OpenProjectSync: Попытка открыть проект {projectPath}");

                    // Проверяем существование файла проекта
                    if (!File.Exists(projectPath))
                    {
                        _logger.Error($"OpenProjectSync: Файл проекта не существует: {projectPath}");
                        return false;
                    }

                    // Создаем FileInfo для проекта
                    var projectFile = new FileInfo(projectPath);

                    // Открываем проект напрямую (синхронно)
                    _project = _tiaPortal.Projects.Open(projectFile);

                    // Обработка UI-событий во время открытия проекта
                    System.Windows.Forms.Application.DoEvents();

                    // Если проект успешно открыт
                    if (_project != null)
                    {
                        _logger.Info($"OpenProjectSync: Проект успешно открыт: {_project.Name}");

                        // Устанавливаем текущий проект для XML-менеджера
                        SetCurrentProjectInXmlManager();

                        openResult = true;
                    }
                    else
                    {
                        _logger.Error("OpenProjectSync: Проект не удалось открыть, Projects.Open вернул null");
                        openResult = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"OpenProjectSync: Ошибка при открытии проекта: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error($"OpenProjectSync: Внутренняя ошибка: {ex.InnerException.Message}");
                    }
                    openResult = false;

                    // Освобождаем ресурсы при ошибке
                    try
                    {
                        if (_tiaPortal != null)
                        {
                            _tiaPortal.Dispose();
                            _tiaPortal = null;
                        }
                    }
                    catch (Exception disposeEx)
                    {
                        _logger.Error($"OpenProjectSync: Ошибка при освобождении ресурсов TIA Portal: {disposeEx.Message}");
                    }

                    return false;
                }

                if (openResult)
                {
                    // Создаем читатель тегов после успешного открытия проекта
                    try
                    {
                        _tagReader = new TiaPortalTagReader(_logger, this);
                        _logger.Info("OpenProjectSync: TiaPortalTagReader создан успешно");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"OpenProjectSync: Ошибка при создании TiaPortalTagReader: {ex.Message}");

                        // Несмотря на ошибку создания tagReader, проект уже открыт,
                        // поэтому мы не закрываем его, но логируем ошибку
                        _logger.Warn("OpenProjectSync: Проект открыт, но TiaPortalTagReader не создан");
                    }

                    _isConnected = true;
                    _logger.Info($"OpenProjectSync: Проект успешно открыт: {_project.Name}");

                    // Проверяем, нужно ли создать кэш проекта
                    try
                    {
                        if (_xmlManager != null && !_xmlManager.HasExportedDataForProject(_project.Name))
                        {
                            _logger.Info($"OpenProjectSync: Для проекта {_project.Name} отсутствует XML-кэш, будет создан");

                            // Запускаем асинхронный экспорт, но не ждем его завершения
                            ExportCurrentProject();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"OpenProjectSync: Ошибка при проверке/создании XML-кэша: {ex.Message}");
                    }

                    return true;
                }
                else
                {
                    _isConnected = false;
                    _tiaPortal = null;
                    _project = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"OpenProjectSync: Общая ошибка при открытии проекта TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"OpenProjectSync: Внутренняя ошибка: {ex.InnerException.Message}");
                }

                // Сбрасываем состояние в случае ошибки
                _isConnected = false;
                _tiaPortal = null;
                _project = null;
                return false;
            }
        }

        /// <summary>
        /// Установка текущего проекта для работы с XML
        /// </summary>
        private void SetCurrentProjectInXmlManager()
        {
            if (_project != null)
            {
                try
                {
                    string projectName = _project.Name;
                    _xmlManager.SetCurrentProject(projectName);
                    _logger.Info($"SetCurrentProjectInXmlManager: Установлен проект {projectName} для работы с XML");
                }
                catch (Exception ex)
                {
                    _logger.Error($"SetCurrentProjectInXmlManager: Ошибка при установке текущего проекта: {ex.Message}");
                }
            }
            else
            {
                _logger.Warn("SetCurrentProjectInXmlManager: Нет активного проекта");
            }
        }

        /// <summary>
        /// Экспорт тегов в XML
        /// </summary>
        public async Task ExportTagsToXml()
        {
            if (!IsConnected || _project == null)
            {
                _logger.Error("ExportTagsToXml: Нет подключения к TIA Portal");
                return;
            }

            try
            {
                var plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    _logger.Error("ExportTagsToXml: Не удалось получить PlcSoftware");
                    return;
                }

                await _xmlManager.ExportTagsToXml(plcSoftware);
                _logger.Info("ExportTagsToXml: Экспорт выполнен успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXml: Ошибка при экспорте: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение только тегов ПЛК из проекта
        /// </summary>
        public async Task<List<TagDefinition>> GetPlcTagsAsync()
        {
            _logger.Info($"Вызван метод {nameof(GetPlcTagsAsync)} из {new StackTrace().GetFrame(1).GetMethod().Name}");
            try
            {
                // Проверяем состояние подключения
                if (!IsConnected || CurrentProject == null)
                {
                    _logger.Warn("GetPlcTagsAsync: Нет активного подключения к TIA Portal");
                    
                    // Попробуем использовать XML-кэш, если он есть
                    if (CurrentProject != null && !string.IsNullOrEmpty(CurrentProject.Name))
                    {
                        _xmlManager.SetCurrentProject(CurrentProject.Name);
                        _logger.Info($"GetPlcTagsAsync: Попытка загрузки из XML для проекта: {CurrentProject.Name}");
                    }
                    
                    var tagsFromXml = _xmlManager.LoadPlcTagsFromXml();
                    if (tagsFromXml.Count > 0)
                    {
                        _logger.Info($"GetPlcTagsAsync: Загружено {tagsFromXml.Count} тегов из XML");
                        return tagsFromXml;
                    }
                    
                    _logger.Error("GetPlcTagsAsync: Нет подключения к TIA Portal и отсутствуют данные в XML");
                    return new List<TagDefinition>();
                }

                // Сначала проверяем наличие XML-кэша
                if (CurrentProject != null && !string.IsNullOrEmpty(CurrentProject.Name))
                {
                    _xmlManager.SetCurrentProject(CurrentProject.Name);
                    _logger.Info($"GetPlcTagsAsync: Установлен текущий проект для XML: {CurrentProject.Name}");
                    
                    var tagsFromXml = _xmlManager.LoadPlcTagsFromXml();
                    if (tagsFromXml.Count > 0)
                    {
                        _logger.Info($"GetPlcTagsAsync: Загружено {tagsFromXml.Count} тегов из XML");
                        return tagsFromXml;
                    }
                    else
                    {
                        _logger.Warn($"GetPlcTagsAsync: XML-кэш для проекта {CurrentProject.Name} пуст или поврежден");
                    }
                }

                // Проверяем, жив ли еще TIA Portal
                if (!IsTiaPortalAlive())
                {
                    _logger.Error("GetPlcTagsAsync: TIA Portal не отвечает или закрыт");
                    return new List<TagDefinition>();
                }

                // Теперь пробуем получить теги напрямую из TIA Portal
                if (_tagReader == null)
                {
                    _logger.Info("GetPlcTagsAsync: Инициализация TiaPortalTagReader");
                    
                    try
                    {
                        _tagReader = new TiaPortalTagReader(_logger, this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"GetPlcTagsAsync: Не удалось создать TiaPortalTagReader: {ex.Message}");
                        return new List<TagDefinition>();
                    }
                }

                // Используем синхронный вызов для TIA Portal Openness API
                _logger.Info("GetPlcTagsAsync: Чтение тегов ПЛК из TIA Portal");
                
                try
                {
                    // Используем блокировку для синхронизации доступа к TIA Portal
                    lock (_tiaPortalLock)
                    {
                        PlcData plcData = _tagReader.ReadAllTags();
                        
                        if (plcData != null)
                        {
                            _logger.Info($"GetPlcTagsAsync: Успешно получено {plcData.PlcTags.Count} тегов ПЛК");
                            
                            // Экспортируем полученные данные в XML для будущего использования
                            if (plcData.PlcTags.Count > 0 && CurrentProject != null)
                            {
                                ExportCurrentProject();
                            }
                            
                            return plcData.PlcTags;
                        }
                        else
                        {
                            _logger.Error("GetPlcTagsAsync: ReadAllTags вернул null");
                            return new List<TagDefinition>();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"GetPlcTagsAsync: Ошибка при чтении тегов: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error($"GetPlcTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
                    }
                    
                    // Анализируем ошибку для выявления проблем с COM-объектами
                    if (ex.Message.Contains("COM") || ex.Message.Contains("RCW") || 
                        ex.Message.Contains("0x8") || ex.Message.Contains("thread") ||
                        ex.Message.Contains("STA"))
                    {
                        _logger.Error("GetPlcTagsAsync: Обнаружена ошибка COM или потоков. Переподключение может быть необходимо.");
                        _isConnected = false;
                    }
                    
                    return new List<TagDefinition>();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"GetPlcTagsAsync: Непредвиденная ошибка: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Получение только тегов блоков данных из проекта
        /// </summary>
        public async Task<List<TagDefinition>> GetDbTagsAsync()
        {
            _logger.Info($"Вызван метод {nameof(GetDbTagsAsync)} из {new StackTrace().GetFrame(1).GetMethod().Name}");
            try
            {
                // Проверяем состояние подключения
                if (!IsConnected || CurrentProject == null)
                {
                    _logger.Warn("GetDbTagsAsync: Нет активного подключения к TIA Portal");
                    
                    // Попробуем использовать XML-кэш, если он есть
                    if (CurrentProject != null && !string.IsNullOrEmpty(CurrentProject.Name))
                    {
                        _xmlManager.SetCurrentProject(CurrentProject.Name);
                        _logger.Info($"GetDbTagsAsync: Попытка загрузки из XML для проекта: {CurrentProject.Name}");
                    }
                    
                    var dbsFromXml = _xmlManager.LoadDbTagsFromXml();
                    if (dbsFromXml.Count > 0)
                    {
                        _logger.Info($"GetDbTagsAsync: Загружено {dbsFromXml.Count} блоков данных из XML");
                        return dbsFromXml;
                    }
                    
                    _logger.Error("GetDbTagsAsync: Нет подключения к TIA Portal и отсутствуют данные в XML");
                    return new List<TagDefinition>();
                }

                // Сначала проверяем наличие XML-кэша
                if (CurrentProject != null && !string.IsNullOrEmpty(CurrentProject.Name))
                {
                    _xmlManager.SetCurrentProject(CurrentProject.Name);
                    _logger.Info($"GetDbTagsAsync: Установлен текущий проект для XML: {CurrentProject.Name}");
                    
                    var dbsFromXml = _xmlManager.LoadDbTagsFromXml();
                    if (dbsFromXml.Count > 0)
                    {
                        _logger.Info($"GetDbTagsAsync: Загружено {dbsFromXml.Count} блоков данных из XML");
                        return dbsFromXml;
                    }
                    else
                    {
                        _logger.Warn($"GetDbTagsAsync: XML-кэш для блоков данных проекта {CurrentProject.Name} пуст или поврежден");
                    }
                }

                // Проверяем, жив ли еще TIA Portal
                if (!IsTiaPortalAlive())
                {
                    _logger.Error("GetDbTagsAsync: TIA Portal не отвечает или закрыт");
                    return new List<TagDefinition>();
                }

                // Теперь пробуем получить теги напрямую из TIA Portal
                if (_tagReader == null)
                {
                    _logger.Info("GetDbTagsAsync: Инициализация TiaPortalTagReader");
                    
                    try
                    {
                        _tagReader = new TiaPortalTagReader(_logger, this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"GetDbTagsAsync: Не удалось создать TiaPortalTagReader: {ex.Message}");
                        return new List<TagDefinition>();
                    }
                }

                // Используем синхронный вызов для TIA Portal Openness API
                _logger.Info("GetDbTagsAsync: Чтение тегов DB из TIA Portal");
                
                try
                {
                    // Используем блокировку для синхронизации доступа к TIA Portal
                    lock (_tiaPortalLock)
                    {
                        PlcData plcData = _tagReader.ReadAllTags();
                        
                        if (plcData != null)
                        {
                            _logger.Info($"GetDbTagsAsync: Успешно получено {plcData.DbTags.Count} тегов DB");
                            
                            // Экспортируем полученные данные в XML для будущего использования
                            if (plcData.DbTags.Count > 0 && CurrentProject != null)
                            {
                                ExportCurrentProject();
                            }
                            
                            return plcData.DbTags;
                        }
                        else
                        {
                            _logger.Error("GetDbTagsAsync: ReadAllTags вернул null");
                            return new List<TagDefinition>();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"GetDbTagsAsync: Ошибка при чтении тегов DB: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error($"GetDbTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
                    }
                    
                    // Анализируем ошибку для выявления проблем с COM-объектами
                    if (ex.Message.Contains("COM") || ex.Message.Contains("RCW") || 
                        ex.Message.Contains("0x8") || ex.Message.Contains("thread") ||
                        ex.Message.Contains("STA"))
                    {
                        _logger.Error("GetDbTagsAsync: Обнаружена ошибка COM или потоков. Переподключение может быть необходимо.");
                        _isConnected = false;
                    }
                    
                    return new List<TagDefinition>();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"GetDbTagsAsync: Непредвиденная ошибка: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Загрузка и возврат всех тегов проекта
        /// </summary>
        public async Task<PlcData> GetAllProjectTagsAsync()
        {
            if (!IsConnected || CurrentProject == null)
            {
                _logger.Error("GetAllProjectTagsAsync: Попытка получения тегов без подключения к TIA Portal");
                return new PlcData();
            }

            try
            {
                _logger.Info("GetAllProjectTagsAsync: Запуск чтения всех тегов проекта");

                // Проверяем наличие читателя тегов
                if (_tagReader == null)
                {
                    _logger.Warn("GetAllProjectTagsAsync: TiaPortalTagReader не инициализирован, создаем новый экземпляр");
                    
                    try
                    {
                        _tagReader = new TiaPortalTagReader(_logger, this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"GetAllProjectTagsAsync: Ошибка при создании TiaPortalTagReader: {ex.Message}");
                        return new PlcData();
                    }
                }

                // Используем синхронный метод ReadAllTags для TIA Portal Openness API
                _logger.Info("GetAllProjectTagsAsync: Синхронное чтение тегов");
                PlcData plcData = new PlcData();
                
                try
                {
                    // Используем блокировку для синхронизации доступа к TIA Portal
                    lock (_tiaPortalLock)
                    {
                        // Вызываем метод синхронно в текущем STA-потоке
                        plcData = _tagReader.ReadAllTags();
                        _logger.Info("GetAllProjectTagsAsync: Метод ReadAllTags() выполнен успешно");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"GetAllProjectTagsAsync: Ошибка при вызове ReadAllTags(): {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error($"GetAllProjectTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
                    }
                    return new PlcData();
                }

                // Проверяем результаты и экспортируем при необходимости
                if (plcData.PlcTags.Count > 0 || plcData.DbTags.Count > 0)
                {
                    // Сохраняем результаты в XML для будущего использования
                    ExportCurrentProject();
                }
                
                _logger.Info($"GetAllProjectTagsAsync: Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");
                return plcData;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetAllProjectTagsAsync: Общая ошибка при получении всех тегов проекта: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"GetAllProjectTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
                }
                return new PlcData();
            }
        }

        /// <summary>
        /// Проверка состояния TIA Portal
        /// </summary>
        private bool IsTiaPortalAlive()
        {
            if (_tiaPortal == null || _project == null)
            {
                _logger.Warn("IsTiaPortalAlive: _tiaPortal или _project равны null");
                return false;
            }

            try
            {
                // Проверяем доступность проекта, пытаясь обратиться к его свойствам
                string projectName = _project.Name;
                _logger.Debug($"IsTiaPortalAlive: Успешная проверка проекта {projectName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"IsTiaPortalAlive: TIA Portal недоступен: {ex.Message}");
                // Сбрасываем флаг подключения
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Экспорт текущего проекта в XML
        /// </summary>
        private void ExportCurrentProject()
        {
            if (CurrentProject == null || !IsConnected)
            {
                _logger.Warn("ExportCurrentProject: Нет активного проекта или подключения");
                return;
            }

            try
            {
                // Перед экспортом, проверим, можем ли получить PlcSoftware
                var plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    _logger.Error("ExportCurrentProject: Не удалось получить PlcSoftware");
                    return;
                }

                // Выполняем экспорт асинхронно, но не ждем завершения
                _logger.Info("ExportCurrentProject: Начало экспорта проекта в XML");
                _xmlManager.ExportTagsToXml(plcSoftware).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportCurrentProject: Ошибка при экспорте проекта: {ex.Message}");
            }
        }

        /// <summary>
        /// Задание текущего проекта для XML-менеджера
        /// </summary>
        /// <param name="projectName">Имя проекта</param>
        public void SetXmlManagerProject(string projectName)
        {
            if (_xmlManager != null && !string.IsNullOrEmpty(projectName))
            {
                _xmlManager.SetCurrentProject(projectName);
                _logger.Info($"SetXmlManagerProject: Установлен проект {projectName}");
            }
        }

        /// <summary>
        /// Возвращает информацию о настройках XML-менеджера
        /// </summary>
        /// <returns>Строка с информацией о настройках</returns>
        public string GetXmlManagerInfo()
        {
            try
            {
                var cachedProjects = _xmlManager.GetCachedProjects();
                string projectsInfo = string.Join(", ", cachedProjects);
                
                return $"XML-менеджер: текущий проект = {(_project?.Name ?? "Не задан")}, " +
                       $"проекты в кэше: {(string.IsNullOrEmpty(projectsInfo) ? "нет" : projectsInfo)}";
            }
            catch (Exception ex)
            {
                _logger.Error($"GetXmlManagerInfo: Ошибка: {ex.Message}");
                return "Ошибка получения информации о XML-менеджере";
            }
        }

        /// <summary>
        /// Принудительно обновить кэш тегов для текущего проекта
        /// </summary>
        public async Task<bool> ForceUpdateTagsCacheAsync()
        {
            if (!IsConnected || CurrentProject == null)
            {
                _logger.Error("ForceUpdateTagsCacheAsync: Нет активного подключения к TIA Portal");
                return false;
            }

            try
            {
                _logger.Info("ForceUpdateTagsCacheAsync: Начало принудительного обновления кэша тегов");
                
                // Проверяем доступность TIA Portal
                if (!IsTiaPortalAlive())
                {
                    _logger.Error("ForceUpdateTagsCacheAsync: TIA Portal недоступен");
                    return false;
                }
                
                // Проверяем наличие читателя тегов
                if (_tagReader == null)
                {
                    _logger.Info("ForceUpdateTagsCacheAsync: Инициализация TiaPortalTagReader");
                    
                    try
                    {
                        _tagReader = new TiaPortalTagReader(_logger, this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"ForceUpdateTagsCacheAsync: Не удалось создать TiaPortalTagReader: {ex.Message}");
                        return false;
                    }
                }
                
                // Получаем PlcSoftware для экспорта
                var plcSoftware = GetPlcSoftware();
                if (plcSoftware == null)
                {
                    _logger.Error("ForceUpdateTagsCacheAsync: Не удалось получить PlcSoftware");
                    return false;
                }
                
                // Выполняем экспорт
                await _xmlManager.ExportTagsToXml(plcSoftware);
                
                _logger.Info("ForceUpdateTagsCacheAsync: Кэш тегов успешно обновлен");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"ForceUpdateTagsCacheAsync: Ошибка при обновлении кэша: {ex.Message}");
                return false;
            }
        }
    }
}