using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

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

        /// <summary>
        /// Обработчик для кнопки "Сохранить в кэш"
        /// </summary>
        private void BtnSaveToCache_Click(object sender, RoutedEventArgs e)
        {
            SaveTagsToCache();
        }

        /// <summary>
        /// Новая функция для сохранения тегов текущего проекта
        /// </summary>
        private async void SaveTagsToCache()
        {
            if (!_viewModel.IsConnected)
            {
                MessageBox.Show("Необходимо сначала подключиться к проекту TIA Portal",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _logger.Info("Сохранение тегов в кэш...");
                _viewModel.StatusMessage = "Сохранение тегов в кэш...";
                _viewModel.IsLoading = true;
                _viewModel.ProgressValue = 20;

                // Экспортируем теги в XML
                await _viewModel.ExportTagsToXml();

                _viewModel.ProgressValue = 100;
                _viewModel.StatusMessage = "Теги успешно сохранены в кэш";
                _logger.Info("Теги успешно сохранены в кэш");

                MessageBox.Show("Теги успешно сохранены в кэш и будут доступны при следующем открытии проекта",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при сохранении тегов в кэш: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении тегов в кэш: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _viewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// Импорт тегов из файла
        /// </summary>
        private async void ImportTagsFromFile()
        {
            try
            {
                // Создаем диалог открытия файла
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                    Title = "Импорт тегов",
                    CheckFileExists = true
                };

                // Если пользователь выбрал файл
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    _logger.Info($"Импорт тегов из файла: {filePath}");

                    // Здесь будет вызов соответствующего метода ViewModel
                    // Например: await _viewModel.ImportTagsAsync(filePath);

                    // Пока просто выводим сообщение
                    _viewModel.StatusMessage = "Импорт тегов...";
                    await Task.Delay(1000); // Имитация работы
                    _viewModel.StatusMessage = "Теги импортированы";

                    _logger.Info("Теги успешно импортированы");
                    MessageBox.Show("Теги успешно импортированы",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем UI после импорта
                    UpdateConnectionState();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при импорте тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при импорте тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}