using System;
using System.Threading.Tasks;
using System.Windows;
using SiemensTrend.Communication.TIA;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Часть класса MainWindow для взаимодействия с TIA Portal
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Обработчик нажатия кнопки "Подключиться"
        /// </summary>
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Временно отключаем кнопку, чтобы избежать повторных нажатий
                btnConnect.IsEnabled = false;

                // Получаем список проектов TIA Portal
                bool connected = _viewModel.ConnectToTiaPortal();

                // ConnectToTiaPortal всегда вернет false, чтобы показать диалог выбора
                // В список TiaProjects будут загружены найденные проекты (если есть)
                var projects = _viewModel.TiaProjects;

                if (projects != null && projects.Count > 0)
                {
                    // Есть открытые проекты, показываем диалог выбора
                    ShowProjectChoiceDialog(projects);
                }
                else
                {
                    // Нет открытых проектов, предлагаем открыть файл проекта
                    ShowOpenProjectDialog();
                }

                // Обновляем состояние интерфейса
                UpdateConnectionState();

                // ВАЖНО: НЕ запускаем автоматическую загрузку тегов!
                // _viewModel.GetPlcTagsAsync();  // Раньше здесь могло быть такое
                // _viewModel.GetDbTagsAsync();   // Или такое

                // Инициализируем модель после подключения (без загрузки тегов)
                if (_viewModel.IsConnected)
                {
                    //_viewModel.InitializeAfterConnection();

                    // Сообщаем пользователю, что он может загрузить теги вручную
                    _viewModel.StatusMessage = "Подключено успешно. Используйте кнопки 'Получить теги ПЛК' и 'Получить DB' для загрузки тегов.";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении: {ex.Message}");
                MessageBox.Show($"Ошибка при подключении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Восстанавливаем кнопку в любом случае
                btnConnect.IsEnabled = !_viewModel.IsConnected;
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Отключиться"
        /// </summary>
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Выполняем отключение
                _viewModel.Disconnect();

                // Обновляем состояние интерфейса
                UpdateConnectionState();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при отключении: {ex.Message}");
                MessageBox.Show($"Ошибка при отключении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
     }
}