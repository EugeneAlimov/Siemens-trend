//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Siemens.Engineering;
//using Siemens.Engineering.HW.Features;
//using Siemens.Engineering.SW;
//using SiemensTrend.Core.Logging;
//using SiemensTrend.Core.Models;

//namespace SiemensTrend.Communication.TIA
//{
//    /// <summary>
//    /// Сервис для коммуникации с TIA Portal
//    /// </summary>
//    public class TiaPortalCommunicationService
//    {
//        private readonly Logger _logger;
//        private TiaPortal _tiaPortal;
//        private Project _project;
//        private bool _isConnected;

//        /// <summary>
//        /// Флаг подключения к TIA Portal
//        /// </summary>
//        public bool IsConnected => _isConnected;

//        /// <summary>
//        /// Конструктор
//        /// </summary>
//        public TiaPortalCommunicationService(Logger logger)
//        {
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            _isConnected = false;
//        }

//        /// <summary>
//        /// Получение списка открытых проектов TIA Portal
//        /// </summary>
//        public List<TiaProjectInfo> GetOpenProjects()
//        {
//            List<TiaProjectInfo> projects = new List<TiaProjectInfo>();

//            try
//            {
//                _logger.Info("Получение списка запущенных процессов TIA Portal...");

//                // Получаем все запущенные процессы TIA Portal
//                var tiaProcesses = TiaPortal.GetProcesses();
//                _logger.Info($"Найдено {tiaProcesses.Count} процессов TIA Portal");

//                foreach (var process in tiaProcesses)
//                {
//                    try
//                    {
//                        // Подключаемся к процессу
//                        var tiaPortal = process.Attach();

//                        // Проверяем, есть ли открытый проект
//                        if (tiaPortal.Projects.Count > 0)
//                        {
//                            foreach (var project in tiaPortal.Projects)
//                            {
//                                var projectInfo = new TiaProjectInfo
//                                {
//                                    Name = project.Name,
//                                    Path = project.Path.ToString(),
//                                    TiaProcess = process,
//                                    TiaPortalInstance = tiaPortal,
//                                    Project = project
//                                };

//                                projects.Add(projectInfo);
//                                _logger.Info($"Найден открытый проект: {project.Name} в процессе {process.Id}");
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
//                _logger.Error($"Ошибка при получении списка процессов TIA Portal: {ex.Message}");
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
//                if (projectInfo == null)
//                {
//                    _logger.Error("Ошибка: projectInfo не может быть null");
//                    return false;
//                }

//                _logger.Info($"Подключение к проекту: {projectInfo.Name}");

//                // Сохраняем ссылки на объекты
//                _tiaPortal = projectInfo.TiaPortalInstance;
//                _project = projectInfo.Project;

//                _isConnected = true;
//                _logger.Info($"Успешное подключение к проекту: {_project.Name}");

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при подключении к проекту: {ex.Message}");
//                _isConnected = false;
//                return false;
//            }
//        }

//        /// <summary>
//        /// Открытие проекта TIA Portal
//        /// </summary>
//        public async Task<bool> OpenProjectAsync(string projectPath)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(projectPath))
//                {
//                    _logger.Error("Ошибка: путь к проекту не может быть пустым");
//                    return false;
//                }

//                _logger.Info($"Открытие проекта TIA Portal: {projectPath}");

//                // Создаем новый экземпляр TIA Portal с пользовательским интерфейсом
//                _tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface);

//                // Открываем проект в отдельном потоке, чтобы не блокировать UI
//                var openResult = await Task.Run(() =>
//                {
//                    try
//                    {
//                        // Открываем проект
//                        _project = _tiaPortal.Projects.Open(new System.IO.FileInfo(projectPath));
//                        return true;
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при открытии проекта: {ex.Message}");
//                        return false;
//                    }
//                });

//                if (openResult)
//                {
//                    _isConnected = true;
//                    _logger.Info($"Проект успешно открыт: {_project.Name}");
//                    return true;
//                }
//                else
//                {
//                    _isConnected = false;
//                    return false;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при открытии проекта TIA Portal: {ex.Message}");
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
//                _logger.Info("Отключение от TIA Portal");

//                // Освобождаем ресурсы
//                _project = null;
//                _tiaPortal = null;
//                _isConnected = false;

//                _logger.Info("Отключение от TIA Portal выполнено успешно");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при отключении от TIA Portal: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Получение PlcSoftware из проекта
//        /// </summary>
//        /// <returns>Объект PlcSoftware или null, если не найден</returns>
//        public PlcSoftware GetPlcSoftware()
//        {
//            if (_project == null)
//            {
//                _logger.Error("Ошибка: Проект TIA Portal не открыт.");
//                return null;
//            }

//            _logger.Info($"Поиск PLC Software в проекте {_project.Name}...");

//            try
//            {
//                foreach (var device in _project.Devices)
//                {
//                    _logger.Info($"Проверка устройства: {device.Name}");

//                    foreach (var deviceItem in device.DeviceItems)
//                    {
//                        var softwareContainer = deviceItem.GetService<SoftwareContainer>();

//                        if (softwareContainer?.Software is PlcSoftware plcSoftware)
//                        {
//                            _logger.Info($"✅ Найден PLC Software в устройстве: {device.Name}");
//                            return plcSoftware;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при поиске PLC Software: {ex.Message}");
//            }

//            _logger.Error("❌ Ошибка: PLC Software не найдено в проекте.");
//            return null;
//        }

//        /// <summary>
//        /// Загрузка и возврат всех тегов проекта
//        /// </summary>
//        /// <returns>Объект PlcData, содержащий теги ПЛК и DB</returns>
//        public async Task<PlcData> GetAllProjectTagsAsync()
//        {
//            if (!IsConnected || _project == null)
//            {
//                _logger.Error("Попытка получения тегов без подключения к TIA Portal");
//                return new PlcData();
//            }

//            try
//            {
//                _logger.Info("Запуск чтения всех тегов проекта...");

//                // Создаем считыватель тегов
//                var reader = new TiaPortalTagReader(_logger, this);

//                // Получаем все теги
//                var plcData = await reader.ReadAllTagsAsync();

//                _logger.Info($"Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");

//                return plcData;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении всех тегов проекта: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//                return new PlcData();
//            }
//        }

//        /// <summary>
//        /// Получение списка тегов для отображения в TagBrowserViewModel
//        /// </summary>
//        /// <returns>Список тегов для отображения</returns>
//        public async Task<List<TagDefinition>> GetTagsFromProjectAsync()
//        {
//            try
//            {
//                var plcData = await GetAllProjectTagsAsync();
//                return plcData.AllTags;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении тегов для отображения: {ex.Message}");
//                return new List<TagDefinition>();
//            }
//        }

//        /// <summary>
//        /// Получение только тегов ПЛК из проекта
//        /// </summary>
//        /// <returns>Список тегов ПЛК</returns>
//        public async Task<List<TagDefinition>> GetPlcTagsAsync()
//        {
//            try
//            {
//                var plcData = await GetAllProjectTagsAsync();
//                return plcData.PlcTags;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
//                return new List<TagDefinition>();
//            }
//        }

//        /// <summary>
//        /// Получение только тегов блоков данных из проекта
//        /// </summary>
//        /// <returns>Список тегов блоков данных</returns>
//        public async Task<List<TagDefinition>> GetDbTagsAsync()
//        {
//            try
//            {
//                var plcData = await GetAllProjectTagsAsync();
//                return plcData.DbTags;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении тегов блоков данных: {ex.Message}");
//                return new List<TagDefinition>();
//            }
//        }
//    }
//}

//======================================================================


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.IO;
//using Siemens.Engineering;
//using Siemens.Engineering.HW;
//using Siemens.Engineering.HW.Features;
//using Siemens.Engineering.SW;
//using SiemensTrend.Core.Logging;
//using SiemensTrend.Core.Models;

//namespace SiemensTrend.Communication.TIA
//{
//    /// <summary>
//    /// Сервис для коммуникации с TIA Portal
//    /// </summary>
//    public class TiaPortalCommunicationService
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
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            _isConnected = false;
//        }

//        /// <summary>
//        /// Получение списка запущенных экземпляров TIA Portal
//        /// </summary>
//        public List<TiaProcessInfo> GetTiaProcesses()
//        {
//            List<TiaProcessInfo> processes = new List<TiaProcessInfo>();

//            try
//            {
//                _logger.Info("Получение списка запущенных процессов TIA Portal");
//                var tiaProcesses = TiaPortal.GetProcesses();
//                _logger.Info($"Найдено {tiaProcesses.Count} процессов TIA Portal");

//                // Добавляем найденные процессы в список
//                foreach (var process in tiaProcesses)
//                {
//                    processes.Add(new TiaProcessInfo
//                    {
//                        Id = process.Id,
//                        Process = process
//                    });
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении списка процессов TIA Portal: {ex.Message}");
//            }

//            return processes;
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
//                        var tiaPortal = process.Attach();

//                        // Проверяем, есть ли открытый проект
//                        if (tiaPortal.Projects.Count > 0)
//                        {
//                            foreach (var project in tiaPortal.Projects)
//                            {
//                                var projectInfo = new TiaProjectInfo
//                                {
//                                    Name = project.Name,
//                                    Path = project.Path.ToString(),
//                                    TiaProcess = process,
//                                    TiaPortalInstance = tiaPortal,
//                                    Project = project
//                                };

//                                projects.Add(projectInfo);
//                                _logger.Info($"Найден открытый проект: {project.Name} в процессе {process.Id}");
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
//            }

//            return projects;
//        }

//        /// <summary>
//        /// Запуск нового экземпляра TIA Portal
//        /// </summary>
//        public async Task<bool> StartTiaPortalAsync()
//        {
//            try
//            {
//                _logger.Info("Запуск нового экземпляра TIA Portal");

//                // Запускаем асинхронно, чтобы не блокировать UI
//                await Task.Run(() => {
//                    _tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface);
//                });

//                _logger.Info("TIA Portal успешно запущен");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при запуске TIA Portal: {ex.Message}");
//                return false;
//            }
//        }

//        /// <summary>
//        /// Подключение к выбранному проекту
//        /// </summary>
//        public bool ConnectToProject(TiaProjectInfo projectInfo)
//        {
//            try
//            {
//                if (projectInfo == null)
//                {
//                    _logger.Error("Ошибка: projectInfo не может быть null");
//                    return false;
//                }

//                _logger.Info($"Подключение к проекту: {projectInfo.Name}");

//                // Проверяем корректность projectInfo
//                if (projectInfo.TiaPortalInstance == null || projectInfo.Project == null)
//                {
//                    _logger.Error("Ошибка: TiaPortalInstance или Project в projectInfo равны null");
//                    return false;
//                }

//                // Сохраняем ссылки на объекты
//                _tiaPortal = projectInfo.TiaPortalInstance;
//                _project = projectInfo.Project;

//                // Проверяем доступность проекта
//                if (!IsProjectAccessible(_project))
//                {
//                    _logger.Error("Ошибка: Проект недоступен или закрыт");
//                    return false;
//                }

//                // Создаем читатель тегов
//                _tagReader = new TiaPortalTagReader(_logger, this);

//                // Дополнительная проверка - попытка получить PlcSoftware
//                var plcSoftware = GetPlcSoftware();
//                if (plcSoftware == null)
//                {
//                    _logger.Error("Ошибка: Не удалось получить PlcSoftware из проекта");
//                    return false;
//                }

//                _isConnected = true;
//                _logger.Info($"Успешное подключение к проекту: {_project.Name}");

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при подключении к проекту: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//                _isConnected = false;
//                return false;
//            }
//        }

//        // Новый метод для проверки доступности проекта
//        private bool IsProjectAccessible(Project project)
//        {
//            try
//            {
//                if (project == null)
//                    return false;

//                // Проверяем доступность проекта, пытаясь обратиться к его свойствам
//                var name = project.Name;
//                var devices = project.Devices.Count;

//                _logger.Info($"Проект доступен. Имя: {name}, устройств: {devices}");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Проект недоступен: {ex.Message}");
//                return false;
//            }
//        }

//        /// <summary>
//        /// Открытие проекта TIA Portal
//        /// </summary>
//        public async Task<bool> OpenProjectAsync(string projectPath)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(projectPath))
//                {
//                    _logger.Error("Ошибка: путь к проекту не может быть пустым");
//                    return false;
//                }

//                _logger.Info($"Открытие проекта TIA Portal: {projectPath}");

//                // Проверяем, существует ли файл проекта
//                if (!File.Exists(projectPath))
//                {
//                    _logger.Error($"Файл проекта не найден: {projectPath}");
//                    return false;
//                }

//                // Если TIA Portal еще не запущен, запускаем его
//                if (_tiaPortal == null)
//                {
//                    bool started = await StartTiaPortalAsync();
//                    if (!started)
//                    {
//                        _logger.Error("Не удалось запустить TIA Portal");
//                        return false;
//                    }
//                }

//                // Открываем проект в отдельном потоке, чтобы не блокировать UI
//                var openResult = await Task.Run(() =>
//                {
//                    try
//                    {
//                        // Открываем проект
//                        _project = _tiaPortal.Projects.Open(new FileInfo(projectPath));
//                        return true;
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при открытии проекта: {ex.Message}");
//                        return false;
//                    }
//                });

//                if (openResult)
//                {
//                    // Создаем читатель тегов
//                    _tagReader = new TiaPortalTagReader(_logger, this);

//                    _isConnected = true;
//                    _logger.Info($"Проект успешно открыт: {_project.Name}");
//                    return true;
//                }
//                else
//                {
//                    _isConnected = false;
//                    return false;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при открытии проекта TIA Portal: {ex.Message}");
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
//                _logger.Info("Отключение от TIA Portal");

//                // Освобождаем ресурсы
//                _tagReader = null;
//                _project = null;
//                _tiaPortal = null;
//                _isConnected = false;

//                _logger.Info("Отключение от TIA Portal выполнено успешно");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при отключении от TIA Portal: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Получение PlcSoftware из проекта
//        /// </summary>
//        /// <returns>Объект PlcSoftware или null, если не найден</returns>
//        public PlcSoftware GetPlcSoftware()
//        {
//            if (_project == null)
//            {
//                _logger.Error("Ошибка: Проект TIA Portal не открыт.");
//                return null;
//            }

//            _logger.Info($"Поиск PLC Software в проекте {_project.Name}");
//            _logger.Info($"Количество устройств в проекте: {_project.Devices.Count}");

//            foreach (var device in _project.Devices)
//            {
//                _logger.Info($"Проверка устройства: {device.Name} (Тип: {device.TypeIdentifier})");
//                _logger.Info($"Количество элементов устройства: {device.DeviceItems.Count}");

//                foreach (var deviceItem in device.DeviceItems)
//                {
//                    _logger.Info($"Проверка элемента устройства: {deviceItem.Name}");

//                    try
//                    {
//                        var softwareContainer = deviceItem.GetService<SoftwareContainer>();
//                        if (softwareContainer != null)
//                        {
//                            _logger.Info("SoftwareContainer получен");

//                            if (softwareContainer.Software is PlcSoftware plcSoftware)
//                            {
//                                _logger.Info($"Найден PLC Software в устройстве: {device.Name}");
//                                return plcSoftware;
//                            }
//                            else
//                            {
//                                _logger.Info($"SoftwareContainer не содержит PlcSoftware");
//                            }
//                        }
//                        else
//                        {
//                            _logger.Info($"Не удалось получить SoftwareContainer для элемента {deviceItem.Name}");
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.Error($"Ошибка при проверке элемента устройства {deviceItem.Name}: {ex.Message}");
//                    }
//                }
//            }

//            _logger.Error("PLC Software не найдено в проекте");
//            return null;
//        }

//        /// <summary>
//        /// Загрузка и возврат всех тегов проекта
//        /// </summary>
//        /// <returns>Объект PlcData, содержащий теги ПЛК и DB</returns>
//        public async Task<PlcData> GetAllProjectTagsAsync()
//        {
//            if (!IsConnected || _project == null)
//            {
//                _logger.Error("Попытка получения тегов без подключения к TIA Portal");
//                return new PlcData();
//            }

//            try
//            {
//                _logger.Info("Запуск чтения всех тегов проекта");

//                // Проверяем, создан ли читатель тегов
//                if (_tagReader == null)
//                {
//                    _tagReader = new TiaPortalTagReader(_logger, this);
//                }

//                // Получаем все теги
//                var plcData = await _tagReader.ReadAllTagsAsync();

//                _logger.Info($"Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");

//                return plcData;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении всех тегов проекта: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//                return new PlcData();
//            }
//        }

//        /// <summary>
//        /// Получение только тегов ПЛК из проекта
//        /// </summary>
//        /// <returns>Список тегов ПЛК</returns>
//        public async Task<List<TagDefinition>> GetPlcTagsAsync()
//        {
//            try
//            {
//                if (!IsConnected || _project == null)
//                {
//                    _logger.Error("Попытка получения тегов без подключения к TIA Portal");
//                    return new List<TagDefinition>();
//                }

//                _logger.Info("Запуск чтения тегов ПЛК из проекта...");

//                // Получаем программное обеспечение ПЛК
//                var plcSoftware = GetPlcSoftware();
//                if (plcSoftware == null)
//                {
//                    _logger.Error("Не удалось получить PlcSoftware из проекта");
//                    return new List<TagDefinition>();
//                }

//                // ВАЖНО: Мы не используем Task.Run для Openness API!
//                // Вместо этого используем await Task.FromResult с синхронным методом
//                var plcData = await Task.FromResult(_tagReader.ReadAllTags());

//                _logger.Info($"Чтение завершено, найдено {plcData.PlcTags.Count} тегов ПЛК");
//                return plcData.PlcTags;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
//                throw; // Пробрасываем исключение для обработки в UI
//            }
//        }

//        /// <summary>
//        /// Получение только тегов блоков данных из проекта
//        /// </summary>
//        /// <returns>Список тегов блоков данных</returns>
//        public async Task<List<TagDefinition>> GetDbTagsAsync()
//        {
//            try
//            {
//                _logger.Info("Начало получения тегов блоков данных...");

//                var plcSoftware = GetPlcSoftware();
//                if (plcSoftware == null)
//                {
//                    _logger.Error("Не удалось получить PlcSoftware - объект равен null");
//                    throw new Exception("Не удалось получить программное обеспечение ПЛК");
//                }

//                _logger.Info("PlcSoftware получен успешно");

//                // ВАЖНО: Не используем многопоточность для Openness API
//                var plcData = await Task.FromResult(GetAllProjectTags());
//                _logger.Info($"Чтение завершено: найдено {plcData.DbTags.Count} тегов DB");

//                return plcData.DbTags;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении тегов блоков данных: {ex.Message}");
//                if (ex.InnerException != null)
//                {
//                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
//                }
//                throw; // Пробрасываем исключение дальше для обработки в UI
//            }
//        }

//        /// <summary>
//        /// Синхронный метод для получения всех тегов проекта
//        /// </summary>
//        private PlcData GetAllProjectTags()
//        {
//            _logger.Info("Запуск чтения всех тегов проекта");

//            if (!IsConnected || _project == null)
//            {
//                _logger.Error("Попытка получения тегов без подключения к TIA Portal");
//                throw new InvalidOperationException("Нет подключения к TIA Portal");
//            }

//            // Проверяем, создан ли читатель тегов
//            if (_tagReader == null)
//            {
//                _logger.Info("Создание нового экземпляра TiaPortalTagReader");
//                _tagReader = new TiaPortalTagReader(_logger, this);
//            }

//            // Получаем все теги (синхронно, без Task.Run)
//            var plcData = _tagReader.ReadAllTags();

//            _logger.Info($"Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");

//            return plcData;
//        }

//        /// <summary>
//        /// Получение списка ПЛК в проекте
//        /// </summary>
//        /// <returns>Список устройств ПЛК</returns>
//        public List<DeviceInfo> GetPlcDevices()
//        {
//            if (!IsConnected || _project == null)
//            {
//                _logger.Error("Попытка получения устройств без подключения к TIA Portal");
//                return new List<DeviceInfo>();
//            }

//            try
//            {
//                var devices = new List<DeviceInfo>();

//                foreach (var device in _project.Devices)
//                {
//                    bool isPlc = false;

//                    // Проверяем, является ли устройство ПЛК
//                    foreach (var deviceItem in device.DeviceItems)
//                    {
//                        var softwareContainer = deviceItem.GetService<SoftwareContainer>();
//                        if (softwareContainer?.Software is PlcSoftware)
//                        {
//                            isPlc = true;
//                            break;
//                        }
//                    }

//                    if (isPlc)
//                    {
//                        // Добавляем устройство в список
//                        devices.Add(new DeviceInfo
//                        {
//                            Name = device.Name,
//                            Type = GetPlcType(device),
//                            Device = device
//                        });
//                    }
//                }

//                _logger.Info($"Найдено {devices.Count} устройств ПЛК");
//                return devices;
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Ошибка при получении списка устройств ПЛК: {ex.Message}");
//                return new List<DeviceInfo>();
//            }
//        }

//        /// <summary>
//        /// Определение типа ПЛК
//        /// </summary>
//        private string GetPlcType(Device device)
//        {
//            try
//            {
//                // Получаем тип ПЛК из свойств устройства
//                string deviceType = "Unknown";

//                if (device.TypeIdentifier.Contains("S7-1200"))
//                {
//                    deviceType = "S7-1200";
//                }
//                else if (device.TypeIdentifier.Contains("S7-1500"))
//                {
//                    deviceType = "S7-1500";
//                }
//                else if (device.TypeIdentifier.Contains("S7-300"))
//                {
//                    deviceType = "S7-300";
//                }
//                else if (device.TypeIdentifier.Contains("S7-400"))
//                {
//                    deviceType = "S7-400";
//                }
//                else if (device.TypeIdentifier.Contains("S7-1200F"))
//                {
//                    deviceType = "S7-1200F";
//                }
//                else if (device.TypeIdentifier.Contains("S7-1500F"))
//                {
//                    deviceType = "S7-1500F";
//                }

//                return deviceType;
//            }
//            catch
//            {
//                return "Unknown";
//            }
//        }
//    }

//    /// <summary>
//    /// Информация о процессе TIA Portal
//    /// </summary>
//    public class TiaProcessInfo
//    {
//        /// <summary>
//        /// Идентификатор процесса
//        /// </summary>
//        public int Id { get; set; }

//        /// <summary>
//        /// Процесс TIA Portal
//        /// </summary>
//        public TiaPortalProcess Process { get; set; }

//        /// <summary>
//        /// Строковое представление
//        /// </summary>
//        public override string ToString()
//        {
//            return $"TIA Portal (ID: {Id})";
//        }
//    }

//    /// <summary>
//    /// Информация об устройстве ПЛК
//    /// </summary>
//    public class DeviceInfo
//    {
//        /// <summary>
//        /// Имя устройства
//        /// </summary>
//        public string Name { get; set; }

//        /// <summary>
//        /// Тип устройства
//        /// </summary>
//        public string Type { get; set; }

//        /// <summary>
//        /// Устройство
//        /// </summary>
//        public Device Device { get; set; }

//        /// <summary>
//        /// Строковое представление
//        /// </summary>
//        public override string ToString()
//        {
//            return $"{Name} ({Type})";
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
//using Siemens.Collaboration.Net.Logging;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Сервис для коммуникации с TIA Portal (исправленная версия)
    /// </summary>
    public partial class TiaPortalCommunicationService
    {
        private readonly Logger _logger;
        private TiaPortal _tiaPortal;
        private Project _project;
        private bool _isConnected;
        private TiaPortalTagReader _tagReader;

        /// <summary>
        /// Флаг подключения к TIA Portal
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Текущий проект TIA Portal
        /// </summary>
        public Project CurrentProject => _project;

        /// <summary>
        /// Конструктор
        /// </summary>
        public TiaPortalCommunicationService(Logger logger)
        {
            _logger = logger;
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

                // Логируем информацию о проекте
                _logger.Info($"ConnectToProject: Успешное подключение к TIA Portal. Проект: {_project.Name}, Путь: {_project.Path}");

                // Создаем читатель тегов после успешного подключения
                try
                {
                    _tagReader = new TiaPortalTagReader(_logger, this);
                    _logger.Info("ConnectToProject: TiaPortalTagReader создан успешно");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ConnectToProject: Ошибка при создании TiaPortalTagReader: {ex.Message}");
                    return false;
                }

                // Устанавливаем флаг подключения
                _isConnected = true;
                _logger.Info($"ConnectToProject: Успешное подключение к проекту: {_project.Name}");

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
                                var softwareContainer = deviceItem.GetService<Siemens.Engineering.HW.Features.SoftwareContainer>();
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
        /// Открытие проекта TIA Portal
        /// </summary>
        public async Task<bool> OpenProjectAsync(string projectPath)
        {
            try
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    _logger.Error("OpenProjectAsync: путь к проекту не может быть пустым");
                    return false;
                }

                _logger.Info($"OpenProjectAsync: Открытие проекта TIA Portal: {projectPath}");

                // Если уже подключены к какому-то проекту, сначала отключаемся
                if (_isConnected || _project != null || _tiaPortal != null)
                {
                    _logger.Info("OpenProjectAsync: Обнаружено активное подключение, выполняем отключение");
                    Disconnect();
                }

                // Создаем новый экземпляр TIA Portal с пользовательским интерфейсом
                _logger.Info("OpenProjectAsync: Создание нового экземпляра TIA Portal");
                _tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface);
                _logger.Info("OpenProjectAsync: Экземпляр TIA Portal создан успешно");

                // Открываем проект
                // ВАЖНО: Не используем Task.Run, так как TIA Portal API требует STA потока
                // Используем базовый Task для асинхронности UI, но выполняем операцию в том же потоке
                bool openResult = false;

                try
                {
                    _logger.Info($"OpenProjectAsync: Попытка открыть проект {projectPath}");

                    // Проверяем существование файла проекта
                    if (!System.IO.File.Exists(projectPath))
                    {
                        _logger.Error($"OpenProjectAsync: Файл проекта не существует: {projectPath}");
                        return false;
                    }

                    // Создаем FileInfo для проекта
                    var projectFile = new System.IO.FileInfo(projectPath);

                    // Открываем проект напрямую (без Task.Run)
                    _project = _tiaPortal.Projects.Open(projectFile);

                    // Если проект успешно открыт
                    if (_project != null)
                    {
                        _logger.Info($"OpenProjectAsync: Проект успешно открыт: {_project.Name}");
                        openResult = true;
                    }
                    else
                    {
                        _logger.Error("OpenProjectAsync: Проект не удалось открыть, Projects.Open вернул null");
                        openResult = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"OpenProjectAsync: Ошибка при открытии проекта: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error($"OpenProjectAsync: Внутренняя ошибка: {ex.InnerException.Message}");
                    }
                    openResult = false;
                }

                if (openResult)
                {
                    // Создаем читатель тегов после успешного открытия проекта
                    try
                    {
                        _tagReader = new TiaPortalTagReader(_logger, this);
                        _logger.Info("OpenProjectAsync: TiaPortalTagReader создан успешно");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"OpenProjectAsync: Ошибка при создании TiaPortalTagReader: {ex.Message}");
                        return false;
                    }

                    _isConnected = true;
                    _logger.Info($"OpenProjectAsync: Проект успешно открыт: {_project.Name}");
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
                _logger.Error($"OpenProjectAsync: Ошибка при открытии проекта TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"OpenProjectAsync: Внутренняя ошибка: {ex.InnerException.Message}");
                }

                // Сбрасываем состояние в случае ошибки
                _isConnected = false;
                _tiaPortal = null;
                _project = null;
                return false;
            }
        }

        private TiaPortalXmlManager _xmlManager;



        public async Task ExportTagsToXml()
        {
            if (!IsConnected || _project == null)
            {
                _logger.Error("ExportTagsToXml: Нет подключения к TIA Portal");
                return;
            }

            var plcSoftware = GetPlcSoftware();
            if (plcSoftware == null) return;

            await _xmlManager.ExportTagsToXml(plcSoftware);
        }

        /// <summary>
        /// Получение только тегов ПЛК из проекта
        /// </summary>
        public async Task<List<TagDefinition>> GetPlcTagsAsync()
        {
            try
            {
                // Проверка наличия XML-файлов
                var tagsFromXml = _xmlManager.LoadPlcTagsFromXml();
                if (tagsFromXml.Count > 0)
                {
                    _logger.Info($"GetPlcTagsAsync: Загружено {tagsFromXml.Count} тегов из XML");
                    return tagsFromXml;
                }

                // Если XML нет, экспортируем и затем загружаем
                if (IsConnected && _project != null)
                {
                    await ExportTagsToXml();
                    tagsFromXml = _xmlManager.LoadPlcTagsFromXml();
                    _logger.Info($"GetPlcTagsAsync: Экспортировано и загружено {tagsFromXml.Count} тегов");
                    return tagsFromXml;
                }

                _logger.Error("GetPlcTagsAsync: Нет подключения к TIA Portal и отсутствуют XML");
                return new List<TagDefinition>();
            }
            catch (Exception ex)
            {
                _logger.Error($"GetPlcTagsAsync: Ошибка: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Загрузка и возврат всех тегов проекта
        /// </summary>
        public async Task<PlcData> GetAllProjectTagsAsync()
        {
            if (!IsConnected || _project == null)
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
                    _tagReader = new TiaPortalTagReader(_logger, this);
                }

                // ВАЖНО: Не используем Task.Run для Openness API!
                // Вместо этого используем Task.FromResult для асинхронной обертки синхронного метода
                var plcData = await Task.FromResult(_tagReader.ReadAllTags());

                _logger.Info($"GetAllProjectTagsAsync: Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");
                return plcData;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetAllProjectTagsAsync: Ошибка при получении всех тегов проекта: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"GetAllProjectTagsAsync: Внутренняя ошибка: {ex.InnerException.Message}");
                }
                return new PlcData();
            }
        }

        /// <summary>
        /// Получение только тегов блоков данных из проекта
        /// </summary>
        public async Task<List<TagDefinition>> GetDbTagsAsync()
        {
            try
            {
                // Проверка наличия XML-файлов
                var dbsFromXml = _xmlManager.LoadDbTagsFromXml();
                if (dbsFromXml.Count > 0)
                {
                    _logger.Info($"GetDbTagsAsync: Загружено {dbsFromXml.Count} блоков данных из XML");
                    return dbsFromXml;
                }

                // Если XML нет, экспортируем и затем загружаем
                if (IsConnected && _project != null)
                {
                    await ExportTagsToXml();
                    dbsFromXml = _xmlManager.LoadDbTagsFromXml();
                    _logger.Info($"GetDbTagsAsync: Экспортировано и загружено {dbsFromXml.Count} блоков данных");
                    return dbsFromXml;
                }

                _logger.Error("GetDbTagsAsync: Нет подключения к TIA Portal и отсутствуют XML");
                return new List<TagDefinition>();
            }
            catch (Exception ex)
            {
                _logger.Error($"GetDbTagsAsync: Ошибка: {ex.Message}");
                return new List<TagDefinition>();
            }
        }
    }
}