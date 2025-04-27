using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.ViewModels;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Модель представления
        /// </summary>
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Логгер для записи событий
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// Конструктор главного окна
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Создаем логер
            _logger = new Logger();

            // Создаем и устанавливаем модель представления
            _viewModel = new MainViewModel(_logger);
            DataContext = _viewModel;

            // Инициализируем TagBrowserViewModel
            _viewModel.InitializeTagBrowser();

            // Устанавливаем DataContext для TagBrowserView
            tagBrowser.DataContext = _viewModel.TagBrowserViewModel;

            // Добавьте следующую строку для инициализации улучшенных UI-элементов
            InitializeUI();

            // Инициализируем начальное состояние
            UpdateConnectionState();
        }
        private void InitializeUI()
        {
            try
            {
                // Инициализируем улучшенные элементы для работы с тегами DB
                InitializeEnhancedTagReading();

                // Переопределяем метод получения тегов DB на улучшенный
                OverrideGetDbTagsButton();

                // Добавляем кнопку для экспорта с улучшенным подходом
                AddEnhancedExportButton();

                // Добавляем тестовую кнопку для отладки
                AddTestButton();

                _logger.Info("MainWindow: UI инициализирован с улучшенными элементами для работы с DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"InitializeUI: Ошибка при инициализации улучшенных элементов UI: {ex.Message}");
            }
        }
        /// <summary>
        /// Обработчик нажатия кнопки "Сохранить лог"
        /// </summary>
        private void BtnSaveLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем диалог сохранения файла
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt",
                    Title = "Сохранение лога",
                    DefaultExt = ".txt",
                    AddExtension = true
                };

                // Если пользователь выбрал файл
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    _logger.Info($"Сохранение лога в файл: {filePath}");

                    // Сохраняем содержимое лога в файл
                    System.IO.File.WriteAllText(filePath, txtLog.Text);

                    _logger.Info("Лог успешно сохранен");
                    MessageBox.Show("Лог успешно сохранен",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при сохранении лога: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении лога: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Очистить лог"
        /// </summary>
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Очистка лога");

                // Очищаем содержимое лога
                txtLog.Clear();

                _logger.Info("Лог очищен");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при очистке лога: {ex.Message}");
                MessageBox.Show($"Ошибка при очистке лога: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTestButton()
        {
            try
            {
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
                    var toolbar = toolbarTray.ToolBars[0];

                    // Создаем тестовую кнопку
                    var btnTest = new Button
                    {
                        Content = "Тест DB (отладка)",
                        Margin = new Thickness(3),
                        Padding = new Thickness(5, 3, 5, 3),
                        Background = Brushes.LightYellow
                    };

                    btnTest.Click += (s, e) => TestDbTagsLoading();

                    // Добавляем разделитель и кнопку
                    toolbar.Items.Add(new Separator());
                    toolbar.Items.Add(btnTest);

                    _logger.Info("AddTestButton: Тестовая кнопка добавлена");
                }
                else
                {
                    _logger.Warn("AddTestButton: Панель инструментов не найдена");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"AddTestButton: Ошибка при добавлении тестовой кнопки: {ex.Message}");
            }
        }

        /// <summary>
        /// Создание тестовых тегов DB для отладки
        /// </summary>
        private List<TagDefinition> CreateTestDbTags()
        {
            var tags = new List<TagDefinition>();

            // Создаем несколько тестовых тегов
            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB1",
                Address = "Standard",
                DataType = TagDataType.UDT,
                Comment = "Тестовый блок данных",
                GroupName = "DB",
                IsOptimized = false,
                IsUDT = true
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB2",
                Address = "Optimized",
                DataType = TagDataType.UDT,
                Comment = "Тестовый оптимизированный блок данных",
                GroupName = "DB",
                IsOptimized = true,
                IsUDT = true
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor",
                Address = "Standard",
                DataType = TagDataType.UDT,
                Comment = "Тестовый блок данных для управления двигателем",
                GroupName = "DB",
                IsOptimized = false,
                IsUDT = true
            });

            // Добавляем переменные для блоков данных
            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB1.Value",
                Address = "DB1.DBD0",
                DataType = TagDataType.Real,
                Comment = "Значение",
                GroupName = "DB1",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB1.Status",
                Address = "DB1.DBW4",
                DataType = TagDataType.Int,
                Comment = "Статус",
                GroupName = "DB1",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB2.Value",
                Address = "DB2.Value",
                DataType = TagDataType.Real,
                Comment = "Значение (оптимизированный блок)",
                GroupName = "DB2",
                IsOptimized = true
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor.Speed",
                Address = "DB_Motor.DBD0",
                DataType = TagDataType.Real,
                Comment = "Скорость двигателя",
                GroupName = "DB_Motor",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor.Start",
                Address = "DB_Motor.DBX4.0",
                DataType = TagDataType.Bool,
                Comment = "Запуск двигателя",
                GroupName = "DB_Motor",
                IsOptimized = false
            });

            tags.Add(new TagDefinition
            {
                Id = Guid.NewGuid(),
                Name = "DB_Motor.Stop",
                Address = "DB_Motor.DBX4.1",
                DataType = TagDataType.Bool,
                Comment = "Останов двигателя",
                GroupName = "DB_Motor",
                IsOptimized = false
            });

            return tags;
        }

        /// <summary>
        /// Отладочный метод для тестирования чтения тегов DB
        /// </summary>
        private async void TestDbTagsLoading()
        {
            try
            {
                _logger.Info("TestDbTagsLoading: Запуск тестирования загрузки тегов DB");
                _viewModel.StatusMessage = "Тестирование загрузки тегов DB...";
                _viewModel.IsLoading = true;

                // Проверяем подключение к TIA Portal
                if (!_viewModel.IsConnected)
                {
                    _logger.Warn("TestDbTagsLoading: Нет подключения к TIA Portal");
                    MessageBox.Show("Необходимо сначала подключиться к TIA Portal.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _viewModel.IsLoading = false;
                    return;
                }

                // Создаем тестовые теги DB для отображения
                var testTags = CreateTestDbTags();
                _logger.Info($"TestDbTagsLoading: Создано {testTags.Count} тестовых тегов DB");

                // Очищаем и заполняем коллекцию
                _viewModel.DbTags.Clear();
                foreach (var tag in testTags)
                {
                    _viewModel.DbTags.Add(tag);
                }

                _logger.Info("TestDbTagsLoading: Тестовые теги DB загружены успешно");
                _viewModel.StatusMessage = $"Загружено {testTags.Count} тестовых тегов DB";
            }
            catch (Exception ex)
            {
                _logger.Error($"TestDbTagsLoading: Ошибка: {ex.Message}");
                _viewModel.StatusMessage = "Ошибка при тестировании загрузки тегов DB";
            }
            finally
            {
                _viewModel.IsLoading = false;
            }
        }
    }
}