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

        /// <summary>
        /// Обработчик нажатия кнопки "Получить ПЛК"
        /// </summary>
        //private async void BtnGetPlcs_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        _logger.Info("Запрос списка доступных ПЛК");

        //        // Здесь будет вызов соответствующего метода ViewModel
        //        // Например: await _viewModel.GetAvailablePlcsAsync();

        //        // Пока просто выводим сообщение
        //        _viewModel.StatusMessage = "Получение списка ПЛК...";
        //        await Task.Delay(500); // Имитация работы
        //        _viewModel.StatusMessage = "Список ПЛК получен";

        //        _logger.Info("Список ПЛК получен");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"Ошибка при получении списка ПЛК: {ex.Message}");
        //        MessageBox.Show($"Ошибка при получении списка ПЛК: {ex.Message}",
        //            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        /// <summary>
        /// Обработчик нажатия кнопки "Получить теги ПЛК"
        /// </summary>
        //private async void BtnGetPlcTags_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        _logger.Info("Запрос тегов ПЛК");

        //        // Проверяем соединение перед запросом тегов
        //        if (!_viewModel.IsConnected)
        //        {
        //            _logger.Warn("Попытка получить теги без установленного соединения");
        //            MessageBox.Show("Необходимо сначала подключиться к TIA Portal.",
        //                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }

        //        // Отключаем кнопку на время загрузки
        //        btnGetPlcTags.IsEnabled = false;
        //        _viewModel.IsLoading = true;
        //        _viewModel.StatusMessage = "Получение тегов ПЛК...";
        //        _viewModel.ProgressValue = 10;

        //        await _viewModel.GetPlcTagsAsync();

        //        _logger.Info($"Получено {_viewModel.PlcTags.Count} тегов ПЛК");
        //        _viewModel.StatusMessage = $"Получено {_viewModel.PlcTags.Count} тегов ПЛК";
        //        _viewModel.ProgressValue = 100;

        //        // Проверяем состояние соединения после получения тегов
        //        if (!_viewModel.IsConnected)
        //        {
        //            _logger.Warn("Соединение было потеряно после получения тегов");
        //            MessageBox.Show("Соединение с TIA Portal было потеряно. Пожалуйста, подключитесь снова.",
        //                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"Ошибка при получении тегов ПЛК: {ex.Message}");
        //        MessageBox.Show($"Ошибка при получении тегов ПЛК: {ex.Message}",
        //            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //        _viewModel.StatusMessage = "Ошибка при получении тегов ПЛК";
        //        _viewModel.ProgressValue = 0;
        //    }
        //    finally
        //    {
        //        btnGetPlcTags.IsEnabled = _viewModel.IsConnected;
        //        _viewModel.IsLoading = false;
        //        UpdateConnectionState();
        //    }
        //}

        /// <summary>
        /// Обработчик нажатия кнопки "Получить DB"
        /// </summary>
        //private async void BtnGetDbs_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        _logger.Info("Запрос списка блоков данных");

        //        // Отключаем кнопку на время загрузки
        //        btnGetDbs.IsEnabled = false;
        //        // Показываем индикатор загрузки
        //        _viewModel.IsLoading = true;
        //        _viewModel.StatusMessage = "Получение блоков данных...";
        //        _viewModel.ProgressValue = 10;

        //        // Здесь в реальном проекте будет вызов к модели представления
        //        await Task.Delay(800); // Имитация работы

        //        _viewModel.StatusMessage = "Блоки данных получены";
        //        _viewModel.ProgressValue = 100;

        //        _logger.Info("Блоки данных получены");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"Ошибка при получении блоков данных: {ex.Message}");
        //        MessageBox.Show($"Ошибка при получении блоков данных: {ex.Message}",
        //            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //        _viewModel.StatusMessage = "Ошибка при получении блоков данных";
        //        _viewModel.ProgressValue = 0;
        //    }
        //    finally
        //    {
        //        // Включаем кнопку обратно
        //        btnGetDbs.IsEnabled = _viewModel.IsConnected;
        //        // Скрываем индикатор загрузки
        //        _viewModel.IsLoading = false;
        //    }
        //}

        /// <summary>
        /// Обработчик нажатия кнопки "Получить теги DB"
        /// </summary>
        //private async void BtnGetDbTags_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        _logger.Info("Запрос тегов DB");

        //        // Отключаем кнопку на время загрузки
        //        //btnGetDbTags.IsEnabled = false;
        //        // Показываем индикатор загрузки
        //        _viewModel.IsLoading = true;
        //        _viewModel.StatusMessage = "Получение тегов DB...";
        //        _viewModel.ProgressValue = 10;

        //        //await _viewModel.GetDbTagsAsync();

        //        _logger.Info($"Получено {_viewModel.DbTags.Count} тегов DB");
        //        _viewModel.StatusMessage = $"Получено {_viewModel.DbTags.Count} тегов DB";
        //        _viewModel.ProgressValue = 100;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error($"Ошибка при получении тегов DB: {ex.Message}");
        //        MessageBox.Show($"Ошибка при получении тегов DB: {ex.Message}",
        //            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        //        _viewModel.StatusMessage = "Ошибка при получении тегов DB";
        //        _viewModel.ProgressValue = 0;
        //    }
        //    finally
        //    {
        //        // Включаем кнопку обратно
        //        //btnGetDbTags.IsEnabled = _viewModel.IsConnected;
        //        // Скрываем индикатор загрузки
        //        _viewModel.IsLoading = false;
        //    }
        //}
    }
}