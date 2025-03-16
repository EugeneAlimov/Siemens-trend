using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq; // Добавлено для методов Any(), Where() и т.д.
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.Communication;
using SiemensTrend.Communication.S7;
using System.Windows;

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
        /// Получение тегов ПЛК
        /// </summary>
        public async Task GetPlcTagsAsync()
        {
            if (!IsConnected || _tiaPortalService == null)
            {
                _logger.Error("Невозможно получить теги ПЛК без подключения к TIA Portal");
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "Получение тегов ПЛК...";
                ProgressValue = 10;

                // Получаем теги ПЛК
                var plcTags = await _tiaPortalService.GetPlcTagsAsync();
                ProgressValue = 90;

                // Обновляем отображение тегов ПЛК
                PlcTags.Clear();
                foreach (var tag in plcTags)
                {
                    PlcTags.Add(tag);
                }

                StatusMessage = $"Получено {plcTags.Count} тегов ПЛК";
                ProgressValue = 100;

                _logger.Info($"Теги ПЛК получены успешно: {plcTags.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
                StatusMessage = "Ошибка получения тегов ПЛК";
                ProgressValue = 0;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Получение тегов блоков данных
        /// </summary>
        public async Task GetDbTagsAsync()
        {
            if (!IsConnected || _tiaPortalService == null)
            {
                _logger.Error("Невозможно получить теги DB без подключения к TIA Portal");
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "Получение тегов блоков данных...";
                ProgressValue = 10;

                // Получаем теги DB
                var dbTags = await _tiaPortalService.GetDbTagsAsync();
                ProgressValue = 90;

                // Обновляем отображение тегов DB
                DbTags.Clear();
                foreach (var tag in dbTags)
                {
                    DbTags.Add(tag);
                }

                StatusMessage = $"Получено {dbTags.Count} тегов блоков данных";
                ProgressValue = 100;

                _logger.Info($"Теги блоков данных получены успешно: {dbTags.Count} тегов");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении тегов блоков данных: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
                StatusMessage = "Ошибка получения тегов блоков данных";
                ProgressValue = 0;
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
    }
}