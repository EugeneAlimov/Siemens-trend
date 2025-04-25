using System;
using System.Windows;
using System.Windows.Media;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Часть класса MainWindow для обновления пользовательского интерфейса
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Обновление состояния подключения в интерфейсе
        /// </summary>
        private void UpdateConnectionState()
        {
            try
            {
                // Обновляем статус подключения
                statusConnectionState.Text = _viewModel.IsConnected ? "Подключено" : "Отключено";
                statusConnectionState.Foreground = _viewModel.IsConnected ?
                    Brushes.Green : Brushes.Red;

                // Обновляем название проекта
                statusProjectName.Text = _viewModel.CurrentProjectName;

                // Обновляем доступность кнопок
                btnConnect.IsEnabled = !_viewModel.IsConnected;
                btnDisconnect.IsEnabled = _viewModel.IsConnected;
                btnGetPlcs.IsEnabled = _viewModel.IsConnected;
                btnGetPlcTags.IsEnabled = _viewModel.IsConnected;
                btnGetDbs.IsEnabled = _viewModel.IsConnected;
                btnGetDbTags.IsEnabled = _viewModel.IsConnected;
                btnStartMonitoring.IsEnabled = _viewModel.IsConnected && _viewModel.MonitoredTags.Count > 0;
                btnStopMonitoring.IsEnabled = _viewModel.IsConnected;
                btnExportTags.IsEnabled = _viewModel.IsConnected &&
                    (_viewModel.PlcTags.Count > 0 || _viewModel.DbTags.Count > 0);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обновлении состояния подключения: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновление индикатора загрузки
        /// </summary>
        private void UpdateLoadingIndicator()
        {
            // Показываем или скрываем индикатор загрузки
            progressRing.Visibility = _viewModel.IsLoading ?
                Visibility.Visible : Visibility.Collapsed;

            // Показываем или скрываем прогресс-бар в статусной строке
            statusProgressBar.Visibility = _viewModel.IsLoading ?
                Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Создание и отображение простого диалогового окна
        /// </summary>
        /// <param name="title">Заголовок окна</param>
        /// <param name="message">Сообщение</param>
        /// <param name="icon">Иконка сообщения</param>
        /// <returns>Результат диалога</returns>
        private MessageBoxResult ShowMessageDialog(string title, string message, MessageBoxImage icon = MessageBoxImage.Information)
        {
            return MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        /// <summary>
        /// Создание и отображение диалогового окна с подтверждением
        /// </summary>
        /// <param name="title">Заголовок окна</param>
        /// <param name="message">Сообщение</param>
        /// <param name="icon">Иконка сообщения</param>
        /// <returns>Результат диалога (Yes/No)</returns>
        private MessageBoxResult ShowConfirmationDialog(string title, string message, MessageBoxImage icon = MessageBoxImage.Question)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, icon);
        }
    }
}