using System;
using System.Collections.ObjectModel;
using System.Linq;
using SiemensTrend.Communication;
using SiemensTrend.Communication.S7;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.Helpers;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Основная модель представления
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Логгер для записи событий
        /// </summary>
        protected readonly Logger _logger;

        /// <summary>
        /// Сервис для коммуникации с ПЛК
        /// </summary>
        protected ICommunicationService _communicationService;

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
        /// <param name="logger">Логгер</param>
        public MainViewModel(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Info("Инициализация MainViewModel");

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

            // Важно: не вызываем методы получения тегов в конструкторе!

            _logger.Info("MainViewModel инициализирован успешно");
        }

        /// <summary>
        /// Инициализация после подключения к TIA Portal
        /// </summary>
        public void InitializeAfterConnection()
        {
            if (!IsConnected)
            {
                _logger.Warn("InitializeAfterConnection: Вызов без активного подключения");
                return;
            }

            _logger.Info("InitializeAfterConnection: Инициализация после подключения");

            try
            {
                // Инициализируем обозреватель тегов
                InitializeTagBrowser();

                // ВАЖНО: НЕ запускаем автоматическую загрузку тегов!
                // Это будет делаться по явному запросу пользователя через UI

                StatusMessage = "Подключено к TIA Portal. Используйте кнопки для загрузки тегов.";
            }
            catch (Exception ex)
            {
                _logger.Error($"InitializeAfterConnection: Ошибка: {ex.Message}");
                StatusMessage = "Ошибка при инициализации после подключения";
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
        /// Получение экземпляра XmlManager из TiaPortalCommunicationService
        /// </summary>
        protected TiaPortalXmlManager GetXmlManager()
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
                        var xmlManager = xmlManagerProperty.GetValue(_tiaPortalService) as TiaPortalXmlManager;
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
                        var xmlManager = field.GetValue(_tiaPortalService) as TiaPortalXmlManager;
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
                return new TiaPortalXmlManager(_logger);
            }
            catch (Exception ex)
            {
                _logger.Error($"GetXmlManager: Ошибка: {ex.Message}");

                // Даже в случае ошибки, попробуем создать новый экземпляр
                try
                {
                    return new TiaPortalXmlManager(_logger);
                }
                catch
                {
                    return null;
                }
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