using System;
using System.Windows;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Часть класса MainWindow для управления тегами
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Обработчик нажатия кнопки "Запустить мониторинг"
        /// </summary>
        private async void BtnStartMonitoring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Запуск мониторинга тегов");

                await _viewModel.StartMonitoringAsync();

                _logger.Info("Мониторинг запущен");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при запуске мониторинга: {ex.Message}");
                MessageBox.Show($"Ошибка при запуске мониторинга: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Остановить мониторинг"
        /// </summary>
        private async void BtnStopMonitoring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Остановка мониторинга тегов");

                await _viewModel.StopMonitoringAsync();

                _logger.Info("Мониторинг остановлен");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при остановке мониторинга: {ex.Message}");
                MessageBox.Show($"Ошибка при остановке мониторинга: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}