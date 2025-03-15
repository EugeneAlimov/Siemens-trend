using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Сервис для коммуникации с TIA Portal через Openness API
    /// </summary>
    public class TiaPortalCommunicationService : ICommunicationService
    {
        private readonly Logger _logger;
        private bool _isConnected;
        private int _pollingIntervalMs = 1000;

        private TiaPortal _tiaPortal;
        private Project _project;

        /// <summary>
        /// Путь к проекту TIA Portal
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// Текущий проект TIA Portal
        /// </summary>
        public Project CurrentProject => _project;

        /// <summary>
        /// Событие получения новых данных
        /// </summary>
        public event EventHandler<TagDataReceivedEventArgs> DataReceived;

        /// <summary>
        /// Событие изменения состояния соединения
        /// </summary>
        public event EventHandler<bool> ConnectionStateChanged;

        /// <summary>
        /// Состояние соединения
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectionStateChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Интервал опроса тегов в миллисекундах
        /// </summary>
        public int PollingIntervalMs
        {
            get => _pollingIntervalMs;
            set
            {
                if (value < 100)
                    throw new ArgumentException("Интервал опроса должен быть не менее 100 мс");

                _pollingIntervalMs = value;
            }
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logger">Логер</param>
        public TiaPortalCommunicationService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Получение списка запущенных экземпляров TIA Portal
        /// </summary>
        /// <returns>Список запущенных экземпляров</returns>
        public List<TiaPortalProcess> GetRunningTiaPortalInstances()
        {
            try
            {
                _logger.Info("Поиск запущенных экземпляров TIA Portal...");
                var processes = TiaPortal.GetProcesses();
                _logger.Info($"Найдено {processes.Count} запущенных экземпляров TIA Portal");
                return processes.ToList();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске запущенных экземпляров TIA Portal: {ex.Message}");
                return new List<TiaPortalProcess>();
            }
        }

        /// <summary>
        /// Получение списка открытых проектов во всех запущенных экземплярах TIA Portal
        /// </summary>
        /// <returns>Список информации об открытых проектах</returns>
        public List<TiaProjectInfo> GetOpenProjects()
        {
            try
            {
                _logger.Info("Поиск открытых проектов TIA Portal...");
                var projectList = new List<TiaProjectInfo>();
                var processes = GetRunningTiaPortalInstances();

                foreach (var process in processes)
                {
                    try
                    {
                        // Подключаемся к экземпляру TIA Portal
                        var tiaPortal = process.Attach();

                        // Получаем открытые проекты в этом экземпляре
                        foreach (var project in tiaPortal.Projects)
                        {
                            projectList.Add(new TiaProjectInfo
                            {
                                Name = project.Name,
                                Path = project.Path.ToString(),
                                TiaProcess = process,
                                TiaPortalInstance = tiaPortal,
                                Project = project
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при получении проектов из экземпляра TIA Portal: {ex.Message}");
                    }
                }

                _logger.Info($"Найдено {projectList.Count} открытых проектов TIA Portal");
                return projectList;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске открытых проектов TIA Portal: {ex.Message}");
                return new List<TiaProjectInfo>();
            }
        }

        // Добавьте в класс TiaPortalCommunicationService
        private T ExecuteInUIThread<T>(Func<T> action)
        {
            if (System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA)
            {
                // Уже в STA-потоке, просто выполняем
                return action();
            }
            else
            {
                // Создаем TaskCompletionSource для получения результата
                var tcs = new TaskCompletionSource<T>();

                // Используем DispatcherInvoke для выполнения в UI-потоке
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var result = action();
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                return tcs.Task.Result;
            }
        }

        // Использование:
        // _project = ExecuteInUIThread(() => _tiaPortal.Projects.Open(new FileInfo(projectPath)));

        /// <summary>
        /// Открытие проекта TIA Portal
        /// </summary>
        /// <param name="projectPath">Путь к файлу проекта</param>
        /// <returns>True если открытие успешно</returns>
        public async Task<bool> OpenProjectAsync(string projectPath)
        {
            try
            {
                _logger.Info($"Открытие проекта TIA Portal: {projectPath}");

                if (!File.Exists(projectPath))
                {
                    _logger.Error($"Файл проекта не найден: {projectPath}");
                    return false;
                }

                // Создаем новый экземпляр TIA Portal с UI интерфейсом
                _logger.Info("🚀 Запускаем TIA Portal...");
                _tiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface);

                // ВАЖНО: Вызываем синхронно, БЕЗ использования Task.Run
                _logger.Info("📂 Открываем проект...");
                _project = _tiaPortal.Projects.Open(new FileInfo(projectPath));

                // Эта строка сделает метод "псевдо-асинхронным" и позволит UI не замораживаться
                await Task.Yield();

                // Сохраняем путь к проекту
                ProjectPath = projectPath;

                if (_project != null)
                {
                    _logger.Info($"✅ Проект успешно открыт: {_project.Name}");
                    IsConnected = true;
                    return true;
                }
                else
                {
                    _logger.Error("❌ Ошибка: Проект не открылся.");
                    IsConnected = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Ошибка при открытии проекта TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутреннее исключение: {ex.InnerException.Message}");
                }
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Подключение к уже открытому проекту
        /// </summary>
        /// <param name="projectInfo">Информация о проекте</param>
        /// <returns>True если подключение успешно</returns>
        public bool ConnectToProject(TiaProjectInfo projectInfo)
        {
            try
            {
                _logger.Info($"Подключение к проекту TIA Portal: {projectInfo.Name}");

                // Используем существующий экземпляр и проект
                _tiaPortal = projectInfo.TiaPortalInstance;
                _project = projectInfo.Project;

                // Сохраняем путь к проекту
                ProjectPath = projectInfo.Path;

                _logger.Info($"Подключено к проекту: {_project.Name}");
                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении к проекту TIA Portal: {ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Подключение к TIA Portal
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                _logger.Info("Подключение к TIA Portal...");

                // Проверяем наличие запущенных экземпляров TIA Portal
                var processes = GetRunningTiaPortalInstances();

                // Если нет запущенных экземпляров, возвращаем false
                // Это будет сигналом для UI, что нужно показать диалог выбора проекта
                if (processes.Count == 0)
                {
                    _logger.Info("Не найдено запущенных экземпляров TIA Portal");
                    return false;
                }

                // Если уже есть подключение, возвращаем успех
                if (IsConnected && _tiaPortal != null && _project != null)
                {
                    _logger.Info("Уже подключено к TIA Portal");
                    return true;
                }

                _logger.Info("Подключение к TIA Portal установлено успешно");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении к TIA Portal: {ex.Message}");
                IsConnected = false;
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
                // Останавливаем мониторинг перед отключением
                StopMonitoringAsync().Wait();

                _logger.Info("Отключение от TIA Portal");

                // Сбрасываем ссылки на проект и TIA Portal
                _project = null;
                _tiaPortal = null;
                ProjectPath = null;

                IsConnected = false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отключении от TIA Portal: {ex.Message}");
            }
        }

        // Остальные методы остаются без изменений...

        /// <summary>
        /// Получение тегов из проекта TIA Portal
        /// </summary>
        public async Task<List<TagDefinition>> GetTagsFromProjectAsync()
        {
            if (!IsConnected || _project == null)
            {
                _logger.Error("Попытка получения тегов без подключения к TIA Portal");
                return new List<TagDefinition>();
            }

            try
            {
                _logger.Info("Получение тегов из проекта TIA Portal...");

                var tagList = new List<TagDefinition>();

                await Task.Run(() =>
                {
                    // Здесь будет реальный код для получения тегов из проекта
                    // Это сложная задача, требующая обхода древовидной структуры проекта

                    // Пока возвращаем тестовые данные
                    tagList.Add(new TagDefinition
                    {
                        Name = "Motor1_Speed",
                        Address = "DB1.DBD0",
                        DataType = TagDataType.Real,
                        GroupName = "Motors",
                        Comment = "Speed of motor 1"
                    });

                    tagList.Add(new TagDefinition
                    {
                        Name = "Motor1_Running",
                        Address = "DB1.DBX4.0",
                        DataType = TagDataType.Bool,
                        GroupName = "Motors",
                        Comment = "Motor 1 running status"
                    });

                    tagList.Add(new TagDefinition
                    {
                        Name = "Temperature",
                        Address = "DB2.DBD0",
                        DataType = TagDataType.Real,
                        GroupName = "Sensors",
                        Comment = "Temperature sensor"
                    });
                });

                _logger.Info($"Получено {tagList.Count} тегов из проекта");
                return tagList;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов из проекта: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Чтение значения тега
        /// </summary>
        public Task<object> ReadTagAsync(TagDefinition tag)
        {
            // В текущей реализации TIA Portal используется только для 
            // извлечения структуры проекта, а не для онлайн доступа к данным
            _logger.Warn("TIA Portal Openness не поддерживает чтение тегов в реальном времени");
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Начало мониторинга тегов
        /// </summary>
        public Task StartMonitoringAsync(IEnumerable<TagDefinition> tags)
        {
            // В текущей реализации TIA Portal используется только для 
            // извлечения структуры проекта, а не для онлайн доступа к данным
            _logger.Warn("TIA Portal Openness не поддерживает мониторинг тегов в реальном времени");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Остановка мониторинга тегов
        /// </summary>
        public Task StopMonitoringAsync()
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Информация о проекте TIA Portal
    /// </summary>
    public class TiaProjectInfo
    {
        /// <summary>
        /// Имя проекта
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Путь к проекту
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Процесс TIA Portal
        /// </summary>
        public TiaPortalProcess TiaProcess { get; set; }

        /// <summary>
        /// Экземпляр TIA Portal
        /// </summary>
        public TiaPortal TiaPortalInstance { get; set; }

        /// <summary>
        /// Объект проекта
        /// </summary>
        public Project Project { get; set; }

        /// <summary>
        /// Строковое представление
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}