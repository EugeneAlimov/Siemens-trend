using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.ViewModels;
using SiemensTrend.Storage.TagManagement;
using SiemensTrend.Communication.TIA;
using SiemensTrend.Communication;
using SiemensTrend.Communication.S7;
using System.Windows.Threading;
namespace SiemensTrend.Views
{
    /// <summary>
    /// ии ииии?? и MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// ии ииии?
        /// </summary>
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// ии и ии ии?
        /// </summary>
        private readonly Logger _logger;

        private readonly ICommunicationService _communicationService;

        /// <summary>
        /// Initializes the UI components and sets up any required configurations.
        /// </summary>
        private void InitializeUI()
        {
            // Add your UI initialization logic here.
            // For example, setting up default values, binding data, or configuring controls.
            _logger.Info("UI initialized successfully.");
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public MainWindow(Logger logger, MainViewModel viewModel, ICommunicationService communicationService)
        {
            InitializeComponent();

            // Используем инжектированные зависимости
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));

            // Логируем тип коммуникационного сервиса
            _logger.Info($"MainWindow: Инициализирован с коммуникационным сервисом типа {_communicationService.GetType().FullName}");

            // Устанавливаем контекст данных
            DataContext = _viewModel;

            // Обновляем состояние интерфейса
            UpdateConnectionState();

            // Инициализируем логгер
            _logger = new Logger();

            DataContext = _viewModel;

            // Инициализируем интерфейс
            InitializeUI();

            // Загружаем теги при запуске
            _viewModel.Initialize();

            // Обновляем состояние интерфейса
            UpdateConnectionState();
        }

        /// <summary>
        /// Обработчик события получения новых данных
        /// </summary>
        private void CommunicationService_DataReceived(object sender, TagDataReceivedEventArgs e)
        {
            try
            {
                // Добавляем данные на график
                Dispatcher.Invoke(() =>
                {
                    // Проверяем, что компонент графика существует
                    if (chartView != null)
                    {
                        chartView.AddDataPoints(e.DataPoints);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке новых данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Переопределяем OnContentRendered для вызова InitializeChart после загрузки UI
        /// </summary>
        private void InitializeDurationTimer()
        {
            _durationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _durationTimer.Tick += (s, e) =>
            {
                if (!_isPaused)
                {
                    UpdateDuration();
                }
            };

            _durationTimer.Start();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            InitializeChart();
        }

        /// <summary>
        /// Метод инициализации графика
        /// </summary>
        private void InitializeChart()
        {
            try
            {
                // Подписываемся на событие изменения временного интервала
                chartView.TimeRangeChanged += (s, interval) =>
                {
                    _logger.Info($"Изменен интервал графика: {interval} сек");
                };

                // Подписываемся на событие получения данных от коммуникационного сервиса
                if (_viewModel.TiaPortalService != null)
                {
                    _viewModel.TiaPortalService.DataReceived += CommunicationService_DataReceived;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при инициализации графика: {ex.Message}");
            }

            _viewModel.TagAddedToMonitoring += (s, tag) =>
            {
                Dispatcher.Invoke(() =>
                {
                    chartView.AddTagToChart(tag);
                });
            };

            _viewModel.TagRemovedFromMonitoring += (s, tag) =>
            {
                Dispatcher.Invoke(() =>
                {
                    chartView.RemoveTagFromChart(tag);
                });
            };
        }

        /// <summary>
        /// Переопределяем OnLoaded для вызова InitializeChart после загрузки UI
        /// </summary>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            InitializeChart();
        }

        /// <summary>
        /// Переопределяем OnClosing для отписки от событий
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // Отписываемся от событий
            if (_communicationService != null)
            {
                _communicationService.DataReceived -= CommunicationService_DataReceived;
            }
        }
    }
}

