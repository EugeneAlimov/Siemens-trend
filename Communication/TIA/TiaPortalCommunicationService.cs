using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Siemens.Engineering;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

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

        /// <summary>
        /// Флаг подключения к TIA Portal
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Конструктор
        /// </summary>
        public TiaPortalCommunicationService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isConnected = false;
        }

        /// <summary>
        /// Получение списка открытых проектов TIA Portal
        /// </summary>
        public List<TiaProjectInfo> GetOpenProjects()
        {
            List<TiaProjectInfo> projects = new List<TiaProjectInfo>();

            try
            {
                _logger.Info("Получение списка запущенных процессов TIA Portal...");

                // Получаем все запущенные процессы TIA Portal
                var tiaProcesses = TiaPortal.GetProcesses();
                _logger.Info($"Найдено {tiaProcesses.Count} процессов TIA Portal");

                foreach (var process in tiaProcesses)
                {
                    try
                    {
                        // Подключаемся к процессу
                        var tiaPortal = process.Attach();

                        // Проверяем, есть ли открытый проект
                        if (tiaPortal.Projects.Count > 0)
                        {
                            foreach (var project in tiaPortal.Projects)
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
                _logger.Error($"Ошибка при получении списка процессов TIA Portal: {ex.Message}");
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
                if (projectInfo == null)
                {
                    _logger.Error("Ошибка: projectInfo не может быть null");
                    return false;
                }

                _logger.Info($"Подключение к проекту: {projectInfo.Name}");

                // Сохраняем ссылки на объекты
                _tiaPortal = projectInfo.TiaPortalInstance;
                _project = projectInfo.Project;

                _isConnected = true;
                _logger.Info($"Успешное подключение к проекту: {_project.Name}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении к проекту: {ex.Message}");
                _isConnected = false;
                return false;
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
                    _logger.Error("Ошибка: путь к проекту не может быть пустым");
                    return false;
                }

                _logger.Info($"Открытие проекта TIA Portal: {projectPath}");

                // Создаем новый экземпляр TIA Portal с пользовательским интерфейсом
                _tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface);

                // Открываем проект в отдельном потоке, чтобы не блокировать UI
                var openResult = await Task.Run(() =>
                {
                    try
                    {
                        // Открываем проект
                        _project = _tiaPortal.Projects.Open(new System.IO.FileInfo(projectPath));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при открытии проекта: {ex.Message}");
                        return false;
                    }
                });

                if (openResult)
                {
                    _isConnected = true;
                    _logger.Info($"Проект успешно открыт: {_project.Name}");
                    return true;
                }
                else
                {
                    _isConnected = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при открытии проекта TIA Portal: {ex.Message}");
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
                _logger.Info("Отключение от TIA Portal");

                // Освобождаем ресурсы
                _project = null;
                _tiaPortal = null;
                _isConnected = false;

                _logger.Info("Отключение от TIA Portal выполнено успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отключении от TIA Portal: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение PlcSoftware из проекта
        /// </summary>
        /// <returns>Объект PlcSoftware или null, если не найден</returns>
        public PlcSoftware GetPlcSoftware()
        {
            if (_project == null)
            {
                _logger.Error("Ошибка: Проект TIA Portal не открыт.");
                return null;
            }

            _logger.Info($"Поиск PLC Software в проекте {_project.Name}...");

            try
            {
                foreach (var device in _project.Devices)
                {
                    _logger.Info($"Проверка устройства: {device.Name}");

                    foreach (var deviceItem in device.DeviceItems)
                    {
                        var softwareContainer = deviceItem.GetService<SoftwareContainer>();

                        if (softwareContainer?.Software is PlcSoftware plcSoftware)
                        {
                            _logger.Info($"✅ Найден PLC Software в устройстве: {device.Name}");
                            return plcSoftware;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске PLC Software: {ex.Message}");
            }

            _logger.Error("❌ Ошибка: PLC Software не найдено в проекте.");
            return null;
        }

        /// <summary>
        /// Загрузка и возврат всех тегов проекта
        /// </summary>
        /// <returns>Объект PlcData, содержащий теги ПЛК и DB</returns>
        public async Task<PlcData> GetAllProjectTagsAsync()
        {
            if (!IsConnected || _project == null)
            {
                _logger.Error("Попытка получения тегов без подключения к TIA Portal");
                return new PlcData();
            }

            try
            {
                _logger.Info("Запуск чтения всех тегов проекта...");

                // Создаем считыватель тегов
                var reader = new TiaPortalTagReader(_logger, this);

                // Получаем все теги
                var plcData = await reader.ReadAllTagsAsync();

                _logger.Info($"Загружено {plcData.PlcTags.Count} тегов ПЛК и {plcData.DbTags.Count} тегов DB");

                return plcData;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении всех тегов проекта: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
                return new PlcData();
            }
        }

        /// <summary>
        /// Получение списка тегов для отображения в TagBrowserViewModel
        /// </summary>
        /// <returns>Список тегов для отображения</returns>
        public async Task<List<TagDefinition>> GetTagsFromProjectAsync()
        {
            try
            {
                var plcData = await GetAllProjectTagsAsync();
                return plcData.AllTags;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов для отображения: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Получение только тегов ПЛК из проекта
        /// </summary>
        /// <returns>Список тегов ПЛК</returns>
        public async Task<List<TagDefinition>> GetPlcTagsAsync()
        {
            try
            {
                var plcData = await GetAllProjectTagsAsync();
                return plcData.PlcTags;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Получение только тегов блоков данных из проекта
        /// </summary>
        /// <returns>Список тегов блоков данных</returns>
        public async Task<List<TagDefinition>> GetDbTagsAsync()
        {
            try
            {
                var plcData = await GetAllProjectTagsAsync();
                return plcData.DbTags;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов блоков данных: {ex.Message}");
                return new List<TagDefinition>();
            }
        }
    }
}