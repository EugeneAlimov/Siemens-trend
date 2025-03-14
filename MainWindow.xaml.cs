using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SiemensTagExporter.ViewModel;
using SiemensTagExporter.Model;
using SiemensTagExporter.Utils;
using Microsoft.VisualBasic.ApplicationServices;
//using Siemens_trend.Utils;
//using Siemens_trend.ViewModel;

namespace SiemensTagExporter
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly SimpleLogger _logger;

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация логгера
            _logger = new SimpleLogger();
            _logger.LogEvent += OnLogEvent;

            // Инициализация и установка модели представления
            _viewModel = new MainViewModel(_logger);
            DataContext = _viewModel;

            // Подписка на события
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.StatusChanged += ViewModel_StatusChanged;

            // Начальная инициализация
            UpdateUIState();

            // Вывод информации при запуске
            LogMessage("Приложение запущено. Ожидание подключения к TIA Portal...");
        }

        #region Обработчики событий

        /// <summary>
        /// Обработчик изменения свойств в ViewModel
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.IsConnected):
                case nameof(MainViewModel.IsLoading):
                case nameof(MainViewModel.SelectedPlc):
                case nameof(MainViewModel.SelectedDb):
                    UpdateUIState();
                    break;
            }
        }

        /// <summary>
        /// Обработчик изменения статуса
        /// </summary>
        private void ViewModel_StatusChanged(object sender, StatusEventArgs e)
        {
            LogMessage(e.Message);
            UpdateProgress(e.ProgressValue);
        }

        /// <summary>
        /// Обработчик события логирования
        /// </summary>
        private void OnLogEvent(object sender, LogEventArgs e)
        {
            // Выполняем обновление UI в потоке UI
            Dispatcher.Invoke(() =>
            {
                string message = $"[{DateTime.Now:HH:mm:ss}] [{e.Level}] {e.Message}";
                LogMessage(message);
            });
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Обновление состояния элементов UI
        /// </summary>
        private void UpdateUIState()
        {
            // Статус подключения
            statusConnectionState.Text = _viewModel.IsConnected ? "Подключено" : "Отключено";
            statusConnectionState.Foreground = _viewModel.IsConnected
                ? new SolidColorBrush(Colors.Green)
                : new SolidColorBrush(Colors.Red);

            // Текущий проект
            statusProjectName.Text = _viewModel.ProjectName;

            // Обновление индикатора загрузки
            if (_viewModel.IsLoading)
            {
                statusProgressBar.Visibility = Visibility.Visible;
                progressRing.Visibility = Visibility.Visible;
            }
            else
            {
                statusProgressBar.Visibility = Visibility.Collapsed;
                progressRing.Visibility = Visibility.Collapsed;
            }

            // Обновление состояния кнопок
            btnConnect.IsEnabled = !_viewModel.IsLoading && !_viewModel.IsConnected;
            btnDisconnect.IsEnabled = !_viewModel.IsLoading && _viewModel.IsConnected;
            btnGetPlcs.IsEnabled = !_viewModel.IsLoading && _viewModel.IsConnected;
            btnGetPlcTags.IsEnabled = !_viewModel.IsLoading && _viewModel.SelectedPlc != null;
            btnGetDbs.IsEnabled = !_viewModel.IsLoading && _viewModel.SelectedPlc != null;
            btnGetDbTags.IsEnabled = !_viewModel.IsLoading && _viewModel.SelectedDb != null;
            btnExportTags.IsEnabled = !_viewModel.IsLoading &&
                (_viewModel.PlcTags?.Count > 0 || _viewModel.DbTags?.Count > 0);
        }

        /// <summary>
        /// Добавление сообщения в лог
        /// </summary>
        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText(message + Environment.NewLine);
                txtLog.ScrollToEnd();
            });
        }

        /// <summary>
        /// Обновление индикатора прогресса
        /// </summary>
        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                statusProgressBar.Value = value;
            });
        }

        /// <summary>
        /// Сохранение лога в файл
        /// </summary>
        private void SaveLogToFile()
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt",
                    Title = "Сохранить лог",
                    FileName = $"TiaExporter_Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, txtLog.Text);
                    MessageBox.Show($"Лог успешно сохранен в файл: {saveFileDialog.FileName}",
                        "Сохранение лога", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении лога: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Обработчики кнопок UI

        /// <summary>
        /// Обработчик кнопки "Подключиться"
        /// </summary>
        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.ConnectAsync();
        }

        /// <summary>
        /// Обработчик кнопки "Отключиться"
        /// </summary>
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Disconnect();
        }

        /// <summary>
        /// Обработчик кнопки "Получить ПЛК"
        /// </summary>
        private async void BtnGetPlcs_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.GetPlcsAsync();
        }

        /// <summary>
        /// Обработчик кнопки "Получить теги ПЛК"
        /// </summary>
        private async void BtnGetPlcTags_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.GetPlcTagsAsync();
        }

        /// <summary>
        /// Обработчик кнопки "Получить DB"
        /// </summary>
        private async void BtnGetDbs_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.GetDataBlocksAsync();
        }

        /// <summary>
        /// Обработчик кнопки "Получить теги DB"
        /// </summary>
        private async void BtnGetDbTags_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.GetDbTagsAsync();
        }

        /// <summary>
        /// Обработчик кнопки "Экспорт тегов"
        /// </summary>
        private async void BtnExportTags_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV-файлы (*.csv)|*.csv",
                Title = "Экспорт тегов",
                FileName = $"Tags_Export_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await _viewModel.ExportTagsAsync(saveFileDialog.FileName);
            }
        }

        /// <summary>
        /// Обработчик кнопки "Сохранить лог"
        /// </summary>
        private void BtnSaveLog_Click(object sender, RoutedEventArgs e)
        {
            SaveLogToFile();
        }

        /// <summary>
        /// Обработчик кнопки "Очистить лог"
        /// </summary>
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

        /// <summary>
        /// Обработчик закрытия окна
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _viewModel.Disconnect();
        }

        #endregion
    }
}