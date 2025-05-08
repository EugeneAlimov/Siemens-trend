using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using SiemensTrend.Communication;
using SiemensTrend.Communication.TIA;

namespace SiemensTrend.Views.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для диалога добавления тегов
    /// </summary>
    public partial class AddTagsDialog : Window
    {
        private readonly List<TextBox> _tagInputs = new List<TextBox>();
        private readonly Logger _logger;
        private readonly ICommunicationService _communicationService;

        /// <summary>
        /// Список найденных тегов
        /// </summary>
        public List<TagDefinition> FoundTags { get; private set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public AddTagsDialog(Logger logger, ICommunicationService communicationService)
        {
            InitializeComponent();

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
            FoundTags = new List<TagDefinition>();

            // Добавляем первое поле ввода в список
            _tagInputs.Add(txtTag1);

            // Логируем тип сервиса
            _logger.Info($"AddTagsDialog: Инициализация с сервисом типа {_communicationService.GetType().FullName}");

            // Проверка типа сервиса
            if (_communicationService is Communication.TIA.TiaPortalCommunicationService tiaService)
            {
                _logger.Info($"AddTagsDialog: Сервис является TiaPortalCommunicationService");
                _logger.Info($"AddTagsDialog: Состояние подключения - IsConnected: {tiaService.IsConnected}");
            }
            else
            {
                _logger.Warn($"AddTagsDialog: Передан сервис типа {_communicationService.GetType().Name}, " +
                              "который не является TiaPortalCommunicationService");
            }
        }

        /// <summary>
        /// Обработчик изменения текста в поле ввода
        /// </summary>
        private void TagInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Активируем кнопку "Еще один", если текущий инпут не пустой
                TextBox textBox = sender as TextBox;
                if (textBox == _tagInputs.Last())
                {
                    btnAddMore.IsEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
                }

                // Проверяем, все ли поля заполнены для активации "Добавить"
                bool allFilled = _tagInputs.All(tb => !string.IsNullOrWhiteSpace(tb.Text));
                btnAdd.IsEnabled = allFilled && _tagInputs.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обработке изменения текста: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик клика по кнопке "Еще один"
        /// </summary>
        private void BtnAddMore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем новую строку для ввода
                Grid grid = new Grid();
                grid.Margin = new Thickness(0, 0, 0, 5);

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Создаем новый TextBox
                TextBox newTextBox = new TextBox();
                newTextBox.Margin = new Thickness(0, 0, 5, 0);
                newTextBox.VerticalContentAlignment = VerticalAlignment.Center;
                newTextBox.Padding = new Thickness(5);
                newTextBox.TextChanged += TagInput_TextChanged;
                Grid.SetColumn(newTextBox, 0);

                // Создаем кнопку удаления
                Button removeButton = new Button();
                removeButton.Content = "×";
                removeButton.Width = 25;
                removeButton.Height = 25;
                removeButton.Click += RemoveInput_Click;
                Grid.SetColumn(removeButton, 1);

                // Добавляем элементы в Grid
                grid.Children.Add(newTextBox);
                grid.Children.Add(removeButton);

                // Добавляем Grid на панель
                InputsPanel.Children.Add(grid);

                // Добавляем TextBox в список
                _tagInputs.Add(newTextBox);

                // Устанавливаем фокус на новый TextBox
                newTextBox.Focus();

                // Деактивируем кнопку "Еще один" до заполнения нового поля
                btnAddMore.IsEnabled = false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при добавлении нового поля: {ex.Message}");
                MessageBox.Show($"Ошибка при добавлении нового поля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик клика по кнопке удаления
        /// </summary>
        private void RemoveInput_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                Grid grid = button.Parent as Grid;
                TextBox textBox = grid.Children.OfType<TextBox>().FirstOrDefault();

                // Удаляем TextBox из списка
                if (textBox != null)
                {
                    _tagInputs.Remove(textBox);
                }

                // Удаляем Grid с панели
                InputsPanel.Children.Remove(grid);

                // Обновляем состояние кнопок
                UpdateButtonsState();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при удалении поля: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении поля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обновление состояния кнопок
        /// </summary>
        private void UpdateButtonsState()
        {
            try
            {
                // Проверяем, все ли поля заполнены для активации "Добавить"
                bool allFilled = _tagInputs.All(tb => !string.IsNullOrWhiteSpace(tb.Text));
                btnAdd.IsEnabled = allFilled && _tagInputs.Count > 0;

                // Проверяем последнее поле для активации "Еще один"
                if (_tagInputs.Count > 0)
                {
                    btnAddMore.IsEnabled = !string.IsNullOrWhiteSpace(_tagInputs.Last().Text);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при обновлении состояния кнопок: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик клика по кнопке "Отмена"
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Обработчик клика по кнопке "Добавить"
        /// </summary>
        /// <summary>
        /// Обработчик клика по кнопке "Добавить"
        /// </summary>
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Приводим коммуникационный сервис к TiaPortalCommunicationService
            if (!(_communicationService is Communication.TIA.TiaPortalCommunicationService tiaService))
            {
                MessageBox.Show("Необходимо подключение к TIA Portal. Используется неподдерживаемый тип сервиса.",
                    "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка подключения
            if (!tiaService.IsConnected)
            {
                MessageBox.Show("Отсутствует активное подключение к TIA Portal.",
                    "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получаем все введенные имена тегов
            List<string> tagNames = _tagInputs
                .Select(tb => tb.Text.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            if (tagNames.Count == 0)
            {
                MessageBox.Show("Пожалуйста, введите хотя бы один тег",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Запускаем поиск тегов
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Показываем индикатор прогресса
                var progressWindow = new ProgressWindow("Поиск тегов", "Выполняется поиск тегов...");
                progressWindow.Owner = this;
                progressWindow.Show();

                // Запускаем поиск в отдельном потоке
                var dispatcher = Dispatcher.CurrentDispatcher;

                Task.Run(() =>
                {
                    try
                    {
                        _logger.Info($"Начало поиска тегов: {string.Join(", ", tagNames)}");

                        // Вызываем метод поиска тегов из сервиса TIA Portal
                        var foundTags = tiaService.SearchTagsByNames(tagNames);

                        _logger.Info($"Поиск завершен, найдено {foundTags.Count} тегов");

                        // Возвращаемся в UI поток
                        dispatcher.Invoke(() =>
                        {
                            // Закрываем окно прогресса
                            progressWindow.Close();

                            // Сохраняем найденные теги
                            FoundTags = foundTags;

                            // Проверяем результаты
                            if (FoundTags.Count == 0)
                            {
                                MessageBox.Show("Не удалось найти ни один из введенных тегов. Проверьте правильность имен тегов.",
                                    "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }

                            // Если найдены не все теги, показываем предупреждение
                            if (FoundTags.Count < tagNames.Count)
                            {
                                MessageBox.Show($"Найдено {FoundTags.Count} из {tagNames.Count} тегов. " +
                                    "Проверьте правильность имен ненайденных тегов.",
                                    "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                            // Закрываем диалог с успешным результатом
                            DialogResult = true;
                            Close();
                        });
                    }
                    catch (Exception ex)
                    {
                        dispatcher.Invoke(() =>
                        {
                            // Закрываем окно прогресса
                            progressWindow.Close();

                            // Показываем ошибку
                            _logger.Error($"Ошибка при поиске тегов: {ex.Message}");
                            MessageBox.Show($"Ошибка при поиске тегов: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                    finally
                    {
                        dispatcher.Invoke(() =>
                        {
                            Mouse.OverrideCursor = null;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                _logger.Error($"Ошибка при запуске поиска тегов: {ex.Message}");
                MessageBox.Show($"Ошибка при запуске поиска тегов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Окно прогресса для отображения статуса операции
    /// </summary>
    public class ProgressWindow : Window
    {
        private ProgressBar _progressBar;
        private TextBlock _statusText;

        /// <summary>
        /// Конструктор
        /// </summary>
        public ProgressWindow(string title, string initialStatus)
        {
            Title = title;
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;

            // Создаем UI
            var grid = new Grid();
            grid.Margin = new Thickness(20);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Текст статуса
            _statusText = new TextBlock
            {
                Text = initialStatus,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(_statusText, 0);
            grid.Children.Add(_statusText);

            // Прогресс бар
            _progressBar = new ProgressBar
            {
                Width = 350,
                Height = 20,
                IsIndeterminate = true
            };
            Grid.SetRow(_progressBar, 2);
            grid.Children.Add(_progressBar);

            Content = grid;
        }

        /// <summary>
        /// Обновление статуса
        /// </summary>
        public void SetStatus(string status)
        {
            Dispatcher.Invoke(() => _statusText.Text = status);
        }

        /// <summary>
        /// Обновление прогресса
        /// </summary>
        public void SetProgress(int value, int maximum = 100)
        {
            Dispatcher.Invoke(() =>
            {
                _progressBar.IsIndeterminate = false;
                _progressBar.Maximum = maximum;
                _progressBar.Value = value;
            });
        }
    }
}