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
                btnStartMonitoring.IsEnabled = _viewModel.IsConnected && _viewModel.MonitoredTags.Count > 0;
                btnStopMonitoring.IsEnabled = _viewModel.IsConnected;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обновлении состояния подключения: {ex.Message}");
            }
        }
    }
}