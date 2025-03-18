using System;
using System.Collections.ObjectModel;
using System.Linq; // Добавлено для методов Any(), Where() и т.д.
using System.Threading.Tasks;
using SiemensTrend.Communication;
using SiemensTrend.Communication.S7;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Основная модель представления
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        private readonly Logger _logger;
        private ICommunicationService _communicationService;

        private bool _isConnected;
        private bool _isLoading;
        private string _statusMessage;
        private int _progressValue;

        private ObservableCollection<TagDefinition> _availableTags;
        private ObservableCollection<TagDefinition> _monitoredTags;
        private ObservableCollection<TagDefinition> _plcTags;
        private ObservableCollection<TagDefinition> _dbTags;

        /// <summary>
        /// Статус подключения
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        /// <summary>
        /// Флаг загрузки (операция в процессе)
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Сообщение о статусе
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Значение прогресса (0-100)
        /// </summary>
        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        /// <summary>
        /// Доступные теги
        /// </summary>
        public ObservableCollection<TagDefinition> AvailableTags
        {
            get => _availableTags;
            private set => SetProperty(ref _availableTags, value);
        }

        /// <summary>
        /// Отслеживаемые теги
        /// </summary>
        public ObservableCollection<TagDefinition> MonitoredTags
        {
            get => _monitoredTags;
            private set => SetProperty(ref _monitoredTags, value);
        }

        /// <summary>
        /// Теги ПЛК
        /// </summary>
        public ObservableCollection<TagDefinition> PlcTags
        {
            get => _plcTags;
            private set => SetProperty(ref _plcTags, value);
        }

        /// <summary>
        /// Теги блоков данных
        /// </summary>
        public ObservableCollection<TagDefinition> DbTags
        {
            get => _dbTags;
            private set => SetProperty(ref _dbTags, value);
        }

        /// <summary>
        /// Модель представления для обозревателя тегов
        /// </summary>
        public TagBrowserViewModel TagBrowserViewModel { get; private set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="logger">Логер</param>
        public MainViewModel(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Инициализируем коллекции
            AvailableTags = new ObservableCollection<TagDefinition>();
            MonitoredTags = new ObservableCollection<TagDefinition>();
            PlcTags = new ObservableCollection<TagDefinition>();
            DbTags = new ObservableCollection<TagDefinition>();

            // Инициализируем начальные значения
            IsConnected = false;
            IsLoading = false;
            StatusMessage = "Готово к работе";
            ProgressValue = 0;

            // Создаем тестовый сервис коммуникации
            var s7Parameters = new S7ConnectionParameters
            {
                IpAddress = "192.168.0.1",
                CpuType = S7CpuType.S71500,
                Rack = 0,
                Slot = 1
            };

            _communicationService = new S7CommunicationService(logger, s7Parameters);

            // Подписываемся на события
            _communicationService.ConnectionStateChanged += (sender, isConnected) =>
            {
                IsConnected = isConnected;
                StatusMessage = isConnected ? "Подключено к ПЛК" : "Отключено от ПЛК";
            };

            _communicationService.DataReceived += (sender, args) =>
            {
                // Обрабатываем полученные данные
                // (в реальном приложении здесь будет логика обновления графиков)
                _logger.Debug($"Получено {args.DataPoints.Count} точек данных");
            };
        }

        /// <summary>
        /// Подключение к ПЛК
        /// </summary>
        public async Task ConnectAsync()
        {
            if (IsLoading || IsConnected)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Подключение к ПЛК...";
                ProgressValue = 0;

                bool result = await _communicationService.ConnectAsync();

                if (!result)
                {
                    StatusMessage = "Ошибка подключения";
                    _logger.Error("Не удалось подключиться к ПЛК");
                }
                else
                {
                    // Подключились успешно, загружаем список тегов
                    await LoadTagsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении: {ex.Message}");
                StatusMessage = "Ошибка подключения";
            }
            finally
            {
                IsLoading = false;
                ProgressValue = 100;
            }
        }

        /// <summary>
        /// Название текущего проекта
        /// </summary>
        public string CurrentProjectName
        {
            get
            {
                if (_tiaPortalService != null && _tiaPortalService.CurrentProject != null)
                    return _tiaPortalService.CurrentProject.Name;
                return "Нет проекта";
            }
        }

        /// <summary>
        /// Максимальное количество тегов для мониторинга
        /// </summary>
        public int MaxMonitoredTags => 10;

        /// <summary>
        /// Обработчик выбора тега для мониторинга
        /// </summary>
        public void OnTagSelected(object sender, TagDefinition tag)
        {
            if (tag == null) return;

            // Проверяем, не выбран ли уже этот тег
            if (MonitoredTags.Any(t => t.Name == tag.Name))
            {
                _logger.Warn($"Тег {tag.Name} уже добавлен в мониторинг");
                return;
            }

            // Проверяем лимит тегов
            if (MonitoredTags.Count >= MaxMonitoredTags)
            {
                _logger.Warn($"Достигнут лимит тегов для мониторинга ({MaxMonitoredTags})");
                // Здесь можно показать диалог с предупреждением
                return;
            }

            // Добавляем тег в мониторинг
            MonitoredTags.Add(tag);
            _logger.Info($"Тег {tag.Name} добавлен в мониторинг");
        }

        /// <summary>
        /// Отключение от ПЛК
        /// </summary>
        public void Disconnect()
        {
            if (IsLoading || !IsConnected)
                return;

            try
            {
                _communicationService.Disconnect();

                // Если есть TIA Portal сервис, отключаем и его
                _tiaPortalService?.Disconnect();

                StatusMessage = "Отключено от ПЛК";

                // Очищаем списки тегов
                AvailableTags.Clear();
                MonitoredTags.Clear();
                PlcTags.Clear();
                DbTags.Clear();

                IsConnected = false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отключении: {ex.Message}");
                StatusMessage = "Ошибка при отключении";
            }
        }

        /// <summary>
        /// Добавьте этот метод в класс MainViewModel для проверки кэшированных данных
        /// </summary>
        public bool CheckCachedProjectData(string projectName)
        {
            try
            {
                if (string.IsNullOrEmpty(projectName))
                {
                    _logger.Warn("CheckCachedProjectData: Имя проекта не может быть пустым");
                    return false;
                }

                // Проверяем, инициализирован ли TIA сервис
                if (_tiaPortalService == null)
                {
                    _logger.Info("CheckCachedProjectData: Создаем новый экземпляр TiaPortalCommunicationService");
                    _tiaPortalService = new Communication.TIA.TiaPortalCommunicationService(_logger);
                }

                // Получаем доступ к XML Manager через отражение или с помощью нового метода
                var xmlManager = GetXmlManager();
                if (xmlManager == null)
                {
                    _logger.Error("CheckCachedProjectData: Не удалось получить доступ к XmlManager");
                    return false;
                }

                // Проверяем наличие кэшированных данных
                var hasData = xmlManager.HasExportedDataForProject(projectName);
                _logger.Info($"CheckCachedProjectData: Проект {projectName} {(hasData ? "имеет" : "не имеет")} кэшированные данные");

                return hasData;
            }
            catch (Exception ex)
            {
                _logger.Error($"CheckCachedProjectData: Ошибка: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получение экземпляра XmlManager из TiaPortalCommunicationService
        /// </summary>
        private Helpers.TiaPortalXmlManager GetXmlManager()
        {
            try
            {
                // Если есть доступ через TiaPortalCommunicationService
                if (_tiaPortalService != null)
                {
                    // Пробуем получить через публичное свойство, если оно есть
                    var xmlManagerProperty = _tiaPortalService.GetType().GetProperty("XmlManager");
                    if (xmlManagerProperty != null)
                    {
                        var xmlManager = xmlManagerProperty.GetValue(_tiaPortalService) as Helpers.TiaPortalXmlManager;
                        if (xmlManager != null)
                        {
                            return xmlManager;
                        }
                    }

                    // Или через приватное поле с помощью рефлексии
                    var field = _tiaPortalService.GetType().GetField("_xmlManager",
                                      System.Reflection.BindingFlags.NonPublic |
                                      System.Reflection.BindingFlags.Instance);

                    if (field != null)
                    {
                        var xmlManager = field.GetValue(_tiaPortalService) as Helpers.TiaPortalXmlManager;
                        if (xmlManager != null)
                        {
                            return xmlManager;
                        }
                    }

                    // Если не смогли получить через TiaPortalCommunicationService, создаем новый
                    _logger.Warn("GetXmlManager: Не удалось получить XmlManager через TiaPortalCommunicationService, создаем новый");
                }

                // Создаем новый XmlManager если:
                // 1. _tiaPortalService == null
                // 2. Не смогли получить через свойство или рефлексию
                // 3. Полученный xmlManager == null

                _logger.Info("GetXmlManager: Создание нового экземпляра TiaPortalXmlManager");
                return new Helpers.TiaPortalXmlManager(_logger);
            }
            catch (Exception ex)
            {
                _logger.Error($"GetXmlManager: Ошибка: {ex.Message}");

                // Даже в случае ошибки, попробуем создать новый экземпляр
                try
                {
                    return new Helpers.TiaPortalXmlManager(_logger);
                }
                catch
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// Загрузка кэшированных данных проекта
        /// </summary>
        public async Task<bool> LoadCachedProjectDataAsync(string projectName)
        {
            try
            {
                if (string.IsNullOrEmpty(projectName))
                {
                    _logger.Warn("LoadCachedProjectDataAsync: Имя проекта не может быть пустым");
                    return false;
                }

                IsLoading = true;
                StatusMessage = $"Загрузка кэшированных данных проекта {projectName}...";
                ProgressValue = 10;

                // Проверяем, инициализирован ли TIA сервис
                if (_tiaPortalService == null)
                {
                    _tiaPortalService = new Communication.TIA.TiaPortalCommunicationService(_logger);
                }

                // Устанавливаем имя текущего проекта в XML Manager
                var xmlManager = GetXmlManager();
                if (xmlManager != null)
                {
                    xmlManager.SetCurrentProject(projectName);
                    _logger.Info($"LoadCachedProjectDataAsync: Установлен текущий проект {projectName} в XmlManager");
                }
                else
                {
                    _logger.Error("LoadCachedProjectDataAsync: Не удалось получить доступ к XmlManager");
                    IsLoading = false;
                    return false;
                }

                // Загружаем теги ПЛК из кэша
                ProgressValue = 30;
                StatusMessage = "Загрузка тегов ПЛК из кэша...";

                var plcTags = await _tiaPortalService.GetPlcTagsAsync();
                PlcTags.Clear();
                foreach (var tag in plcTags)
                {
                    PlcTags.Add(tag);
                }
                _logger.Info($"LoadCachedProjectDataAsync: Загружено {plcTags.Count} тегов ПЛК");

                // Загружаем теги DB из кэша
                ProgressValue = 60;
                StatusMessage = "Загрузка блоков данных из кэша...";

                var dbTags = await _tiaPortalService.GetDbTagsAsync();
                DbTags.Clear();
                foreach (var tag in dbTags)
                {
                    DbTags.Add(tag);
                }
                _logger.Info($"LoadCachedProjectDataAsync: Загружено {dbTags.Count} блоков данных");

                ProgressValue = 100;
                StatusMessage = $"Загрузка кэшированных данных проекта {projectName} завершена";

                // Устанавливаем статус подключения, хотя реального подключения к TIA Portal нет
                IsConnected = true;

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadCachedProjectDataAsync: Ошибка: {ex.Message}");
                StatusMessage = "Ошибка при загрузке кэшированных данных";
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Загрузка тегов
        /// </summary>
        private async Task LoadTagsAsync()
        {
            if (!IsConnected)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка тегов...";
                ProgressValue = 0;

                // Для демонстрации просто добавляем тестовые теги
                AvailableTags.Clear();

                // Эмулируем задержку загрузки тегов
                await Task.Delay(1000);

                // Добавляем тестовые теги
                AvailableTags.Add(new TagDefinition { Name = "Motor1_Speed", Address = "DB1.DBD0", DataType = TagDataType.Real, GroupName = "Motors" });
                AvailableTags.Add(new TagDefinition { Name = "Motor1_Running", Address = "DB1.DBX4.0", DataType = TagDataType.Bool, GroupName = "Motors" });
                AvailableTags.Add(new TagDefinition { Name = "Motor2_Speed", Address = "DB1.DBD8", DataType = TagDataType.Real, GroupName = "Motors" });
                AvailableTags.Add(new TagDefinition { Name = "Motor2_Running", Address = "DB1.DBX12.0", DataType = TagDataType.Bool, GroupName = "Motors" });
                AvailableTags.Add(new TagDefinition { Name = "Temperature", Address = "DB2.DBD0", DataType = TagDataType.Real, GroupName = "Sensors" });
                AvailableTags.Add(new TagDefinition { Name = "Pressure", Address = "DB2.DBD4", DataType = TagDataType.Real, GroupName = "Sensors" });
                AvailableTags.Add(new TagDefinition { Name = "Level", Address = "DB2.DBD8", DataType = TagDataType.Real, GroupName = "Sensors" });
                AvailableTags.Add(new TagDefinition { Name = "Alarm", Address = "DB3.DBX0.0", DataType = TagDataType.Bool, GroupName = "System" });

                StatusMessage = $"Загружено {AvailableTags.Count} тегов";
                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при загрузке тегов: {ex.Message}");
                StatusMessage = "Ошибка при загрузке тегов";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Добавление тега в мониторинг
        /// </summary>
        public void AddTagToMonitoring(TagDefinition tag)
        {
            if (tag == null || MonitoredTags.Contains(tag))
                return;

            MonitoredTags.Add(tag);
            _logger.Info($"Тег {tag.Name} добавлен в мониторинг");
        }

        /// <summary>
        /// Удаление тега из мониторинга
        /// </summary>
        public void RemoveTagFromMonitoring(TagDefinition tag)
        {
            if (tag == null || !MonitoredTags.Contains(tag))
                return;

            MonitoredTags.Remove(tag);
            _logger.Info($"Тег {tag.Name} удален из мониторинга");
        }

        /// <summary>
        /// Начало мониторинга выбранных тегов
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            if (!IsConnected || MonitoredTags.Count == 0)
                return;

            try
            {
                StatusMessage = "Запуск мониторинга...";
                await _communicationService.StartMonitoringAsync(MonitoredTags);
                StatusMessage = $"Мониторинг запущен для {MonitoredTags.Count} тегов";
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при запуске мониторинга: {ex.Message}");
                StatusMessage = "Ошибка при запуске мониторинга";
            }
        }

        /// <summary>
        /// Остановка мониторинга
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            if (!IsConnected)
                return;

            try
            {
                StatusMessage = "Остановка мониторинга...";
                await _communicationService.StopMonitoringAsync();
                StatusMessage = "Мониторинг остановлен";
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при остановке мониторинга: {ex.Message}");
                StatusMessage = "Ошибка при остановке мониторинга";
            }
        }

        /// <summary>
        /// Получение тегов ПЛК из проекта
        /// </summary>
        public async Task GetPlcTagsAsync()
        {
            try
            {
                if (_tiaPortalService == null)
                {
                    _logger.Error("GetPlcTagsAsync: Сервис TIA Portal не инициализирован");
                    StatusMessage = "Ошибка: сервис TIA Portal не инициализирован";
                    return;
                }

                IsLoading = true;
                StatusMessage = "Получение тегов ПЛК...";
                ProgressValue = 10;

                var plcTags = await _tiaPortalService.GetPlcTagsAsync();
                ProgressValue = 90;

                PlcTags.Clear();
                foreach (var tag in plcTags)
                {
                    PlcTags.Add(tag);
                }

                StatusMessage = $"Получено {plcTags.Count} тегов ПЛК";
                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetPlcTagsAsync: Ошибка: {ex.Message}");
                StatusMessage = "Ошибка получения тегов ПЛК";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Получение тегов блоков данных из проекта
        /// </summary>
        public async Task GetDbTagsAsync()
        {
            try
            {
                if (_tiaPortalService == null)
                {
                    _logger.Error("GetDbTagsAsync: Сервис TIA Portal не инициализирован");
                    StatusMessage = "Ошибка: сервис TIA Portal не инициализирован";
                    return;
                }

                IsLoading = true;
                StatusMessage = "Получение тегов DB...";
                ProgressValue = 10;

                var dbTags = await _tiaPortalService.GetDbTagsAsync();
                ProgressValue = 90;

                DbTags.Clear();
                foreach (var tag in dbTags)
                {
                    DbTags.Add(tag);
                }

                StatusMessage = $"Получено {dbTags.Count} тегов DB";
                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetDbTagsAsync: Ошибка: {ex.Message}");
                StatusMessage = "Ошибка получения тегов DB";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Инициализация обозревателя тегов
        /// </summary>
        public void InitializeTagBrowser()
        {
            if (_tiaPortalService != null && TagBrowserViewModel == null)
            {
                TagBrowserViewModel = new TagBrowserViewModel(_logger, _tiaPortalService);

                // Подписываемся на событие выбора тега
                TagBrowserViewModel.TagSelected += OnTagSelected;
            }
        }

        // Добавим метод для экспорта в XML
        public async Task ExportTagsToXml()
        {
            try
            {
                if (_tiaPortalService == null || !IsConnected)
                {
                    StatusMessage = "Необходимо сначала подключиться к TIA Portal";
                    return;
                }

                IsLoading = true;
                StatusMessage = "Экспорт тегов в XML...";
                ProgressValue = 10;

                await _tiaPortalService.ExportTagsToXml();

                ProgressValue = 100;
                StatusMessage = "Экспорт тегов в XML завершен";
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXml: Ошибка: {ex.Message}");
                StatusMessage = "Ошибка при экспорте тегов";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}