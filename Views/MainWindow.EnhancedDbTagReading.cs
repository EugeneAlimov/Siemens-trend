using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SiemensTrend.Communication.TIA;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Класс расширений для MainWindow с поддержкой улучшенного чтения тегов DB
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Инициализация элементов управления для улучшенного чтения тегов
        /// </summary>
        private void InitializeEnhancedTagReading()
        {
            try
            {
                // Создаем выпадающий список режимов чтения тегов
                var readerModeCombo = new ComboBox
                {
                    Width = 120,
                    Margin = new Thickness(3),
                    ToolTip = "Выберите режим чтения тегов"
                };

                // Заполняем список доступными режимами
                var modes = _viewModel.GetAvailableReaderModes();
                if (modes != null)
                {
                    foreach (var mode in modes)
                    {
                        readerModeCombo.Items.Add(mode);
                    }

                    // Устанавливаем текущий режим, если доступен метод
                    if (_viewModel.SelectedReaderMode != null)
                    {
                        readerModeCombo.SelectedItem = _viewModel.SelectedReaderMode;
                    }
                    else
                    {
                        // Установка режима по умолчанию
                        if (readerModeCombo.Items.Count > 0)
                            readerModeCombo.SelectedIndex = 0;
                    }
                }

                // Находим нужную панель инструментов с кнопками получения тегов
                ToolBarTray toolbarTray = null;

                // Поиск по имени
                toolbarTray = FindName("toolBarTray") as ToolBarTray;

                // Если не нашли по имени, то поищем по типу в визуальном дереве
                if (toolbarTray == null)
                {
                    var toolbarTrays = FindVisualChildren<ToolBarTray>(this);
                    if (toolbarTrays.Count > 0)
                    {
                        toolbarTray = toolbarTrays[0];
                    }
                }

                if (toolbarTray != null && toolbarTray.ToolBars.Count > 0)
                {
                    var tagToolbar = toolbarTray.ToolBars[0]; // Предполагаем, что нужная панель инструментов первая

                    // Добавляем разделитель
                    tagToolbar.Items.Add(new Separator());

                    // Добавляем текстовый блок
                    tagToolbar.Items.Add(new TextBlock
                    {
                        Text = "Режим чтения:",
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(3)
                    });

                    // Добавляем выпадающий список
                    tagToolbar.Items.Add(readerModeCombo);

                    // Добавляем улучшенные кнопки
                    var btnGetDbTagsSafe = CreateToolbarButton("Получить теги DB (безопасно)", BtnGetDbTagsSafe_Click);
                    tagToolbar.Items.Add(btnGetDbTagsSafe);

                    _logger.Info("InitializeEnhancedTagReading: Элементы управления для улучшенного чтения тегов инициализированы");
                }
                else
                {
                    _logger.Warn("InitializeEnhancedTagReading: Не удалось найти панель инструментов");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"InitializeEnhancedTagReading: Ошибка при инициализации элементов управления: {ex.Message}");
            }
        }
        /// <summary>
        /// Создание кнопки для панели инструментов
        /// </summary>
        private Button CreateToolbarButton(string content, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Content = content,
                Margin = new Thickness(3),
                Padding = new Thickness(5, 3, 5, 3)
            };

            if (clickHandler != null)
            {
                button.Click += clickHandler;
            }

            return button;
        }

        /// <summary>
        /// Обновление описания режима чтения тегов
        /// </summary>
        private void UpdateReaderModeDescription()
        {
            var txtReaderModeDescription = FindName("txtReaderModeDescription") as TextBlock;
            if (txtReaderModeDescription != null)
            {
                txtReaderModeDescription.Text = _viewModel.CurrentReaderModeDescription;
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки безопасного получения тегов DB
        /// </summary>
        private async void BtnGetDbTagsSafe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnGetDbTagsSafe_Click: Запрос безопасного получения тегов DB");

                // Проверяем соединение перед запросом тегов
                if (!_viewModel.IsConnected)
                {
                    _logger.Warn("BtnGetDbTagsSafe_Click: Попытка получить теги без установленного соединения");
                    MessageBox.Show("Необходимо сначала подключиться к TIA Portal.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Отключаем кнопку на время загрузки
                var button = sender as Button;
                if (button != null) button.IsEnabled = false;

                _viewModel.IsLoading = true;
                _viewModel.StatusMessage = "Получение тегов DB в безопасном режиме...";
                _viewModel.ProgressValue = 10;

                // Вызываем безопасный метод получения тегов DB
                await _viewModel.GetDbTagsSafeAsync();

                _logger.Info($"BtnGetDbTagsSafe_Click: Получено {_viewModel.DbTags.Count} тегов DB");
                _viewModel.StatusMessage = $"Получено {_viewModel.DbTags.Count} тегов DB";
                _viewModel.ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnGetDbTagsSafe_Click: Ошибка при получении тегов DB: {ex.Message}");
                MessageBox.Show($"Ошибка при получении тегов DB: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.StatusMessage = "Ошибка при получении тегов DB";
                _viewModel.ProgressValue = 0;
            }
            finally
            {
                // Включаем кнопку обратно
                var button = sender as Button;
                if (button != null) button.IsEnabled = true;

                _viewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// Переопределение обработчика для кнопки "Получить теги DB" с использованием улучшенного подхода
        /// </summary>
        protected void OverrideGetDbTagsButton()
        {
            try
            {
                // Найти существующую кнопку
                var btnGetDbTags = FindName("btnGetDbTags") as Button;
                if (btnGetDbTags != null)
                {
                    // Заменить обработчик события
                    btnGetDbTags.Click -= BtnGetDbTags_Click;
                    btnGetDbTags.Click += BtnGetDbTagsSafe_Click;

                    // Обновить текст кнопки
                    btnGetDbTags.Content = "Получить теги DB (безопасно)";

                    _logger.Info("OverrideGetDbTagsButton: Кнопка получения тегов DB переопределена с использованием улучшенного подхода");
                }
                else
                {
                    _logger.Warn("OverrideGetDbTagsButton: Кнопка btnGetDbTags не найдена");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"OverrideGetDbTagsButton: Ошибка при переопределении кнопки: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик кнопки экспорта тегов с улучшенным подходом
        /// </summary>
        private async void BtnExportTagsEnhanced_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("BtnExportTagsEnhanced_Click: Запрос экспорта тегов с улучшенным подходом");

                // Проверяем соединение перед экспортом
                if (!_viewModel.IsConnected)
                {
                    _logger.Warn("BtnExportTagsEnhanced_Click: Попытка экспорта тегов без установленного соединения");
                    MessageBox.Show("Необходимо сначала подключиться к TIA Portal.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Отключаем кнопку на время экспорта
                var button = sender as Button;
                if (button != null) button.IsEnabled = false;

                _viewModel.IsLoading = true;
                _viewModel.StatusMessage = "Экспорт тегов с улучшенным подходом...";
                _viewModel.ProgressValue = 10;

                // Вызываем улучшенный метод экспорта
                await _viewModel.ExportTagsToXmlEnhanced();

                _logger.Info("BtnExportTagsEnhanced_Click: Экспорт тегов завершен");
                _viewModel.StatusMessage = "Экспорт тегов завершен";
                _viewModel.ProgressValue = 100;

                MessageBox.Show("Теги успешно экспортированы с использованием улучшенного подхода",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"BtnExportTagsEnhanced_Click: Ошибка при экспорте тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при экспорте тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.StatusMessage = "Ошибка при экспорте тегов";
                _viewModel.ProgressValue = 0;
            }
            finally
            {
                // Включаем кнопку обратно
                var button = sender as Button;
                if (button != null) button.IsEnabled = true;

                _viewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// Добавление кнопки экспорта тегов с улучшенным подходом
        /// </summary>
        private void AddEnhancedExportButton()
        {
            try
            {
                // Найти существующую панель инструментов
                // Проблема: используется имя toolBarTray, но в XAML может быть другое название
                // Попробуем более надежный способ поиска:

                ToolBarTray toolbarTray = null;

                // Поиск по имени
                toolbarTray = FindName("toolBarTray") as ToolBarTray;

                // Если не нашли по имени, то поищем по типу в визуальном дереве
                if (toolbarTray == null)
                {
                    var toolbarTrays = FindVisualChildren<ToolBarTray>(this);
                    if (toolbarTrays.Count > 0)
                    {
                        toolbarTray = toolbarTrays[0];
                    }
                }

                if (toolbarTray != null && toolbarTray.ToolBars.Count > 0)
                {
                    var toolbar = toolbarTray.ToolBars[0]; // Предполагаем, что нужная панель инструментов первая

                    // Создаем кнопку экспорта с улучшенным подходом
                    var btnExportTagsEnhanced = CreateToolbarButton("Экспорт тегов (улучшенный)", BtnExportTagsEnhanced_Click);

                    // Добавляем разделитель и кнопку
                    toolbar.Items.Add(new Separator());
                    toolbar.Items.Add(btnExportTagsEnhanced);

                    _logger.Info("AddEnhancedExportButton: Кнопка экспорта тегов с улучшенным подходом добавлена");
                }
                else
                {
                    _logger.Warn("AddEnhancedExportButton: Панель инструментов не найдена");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"AddEnhancedExportButton: Ошибка при добавлении кнопки: {ex.Message}");
            }
        }

        // Добавьте вспомогательный метод для поиска элементов в визуальном дереве
        private List<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            List<T> result = new List<T>();
            if (depObj == null) return result;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                if (child is T)
                    result.Add((T)child);

                // Рекурсивно ищем в дочерних элементах
                result.AddRange(FindVisualChildren<T>(child));
            }

            return result;
        }
    }
}