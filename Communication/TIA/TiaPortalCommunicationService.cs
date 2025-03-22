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

        public enum ExportTagType
        {
            All,
            PlcTags,
            DbTags
        }

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
        /// Открытие проекта TIA Portal (синхронный метод)
        /// </summary>
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
                    if (!System.IO.File.Exists(projectPath))
                    {
                        _logger.Error($"OpenProjectSync: Файл проекта не существует: {projectPath}");
                        return false;
                    }

                    // Создаем FileInfo для проекта
                    var projectFile = new System.IO.FileInfo(projectPath);

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
                    catch { }

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
                        return false;
                    }

                    _isConnected = true;
                    _logger.Info($"OpenProjectSync: Проект успешно открыт: {_project.Name}");
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
                _logger.Error($"OpenProjectSync: Ошибка при открытии проекта TIA Portal: {ex.Message}");
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
        /// Открытие Созlадем xml manager
        /// </summary>
        private TiaPortalXmlManager _xmlManager;

        /// <summary>
        /// Установка текущего проекта для работы с XML
        /// </summary>
        public void SetCurrentProjectInXmlManager()
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
        /// "Экспортируем теги в xml"
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
            if (_project != null && !string.IsNullOrEmpty(_project.Name))
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
                    await _xmlManager.ExportTagsToXml(ExportTagType.All);
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

        /// <summary>
        /// Получение только тегов ПЛК из проекта
        /// </summary>
        public async Task<List<TagDefinition>> GetPlcTagsAsync()
        {
            try
            {
                // Убедимся, что текущий проект установлен в XML менеджере
                if (_project != null && !string.IsNullOrEmpty(_project.Name))
                {
                    SetCurrentProjectInXmlManager();
                }

                // Проверка наличия XML-файлов
                var tagsFromXml = _xmlManager.LoadPlcTagsFromXml();
                if (tagsFromXml.Count > 0)
                {
                    _logger.Info($"GetPlcTagsAsync: Загружено {tagsFromXml.Count} тегов из XML");
                    return tagsFromXml;
                }

                // Если XML нет или они пустые, экспортируем и затем загружаем
                if (IsConnected && _project != null)
                {
                    await ExportTagsToXml(ExportTagType.PlcTags); // Только теги ПЛК
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
                // Убедимся, что текущий проект установлен в XML менеджере
                if (_project != null && !string.IsNullOrEmpty(_project.Name))
                {
                    SetCurrentProjectInXmlManager();
                }

                // Проверка наличия XML-файлов
                var dbsFromXml = _xmlManager.LoadDbTagsFromXml();
                if (dbsFromXml.Count > 0)
                {
                    _logger.Info($"GetDbTagsAsync: Загружено {dbsFromXml.Count} блоков данных из XML");
                    return dbsFromXml;
                }

                // Если XML нет или они пустые, экспортируем и затем загружаем
                if (IsConnected && _project != null)
                {
                    await ExportTagsToXml(ExportTagType.DbTags); // Только теги DB
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

        /// <summary>
        /// Добавьте это свойство в класс TiaPortalCommunicationService для публичного доступа к XmlManager
        /// </summary>
        public Helpers.TiaPortalXmlManager XmlManager
        {
            get { return _xmlManager; }
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


    }
}