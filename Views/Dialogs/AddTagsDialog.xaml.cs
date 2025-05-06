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
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
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
                        // Группируем теги по контейнерам для оптимизации поиска
                        var groupedTags = GroupTagsByContainer(tagNames);

                        // Вызываем метод поиска тегов для каждой группы
                        var foundTags = new List<TagDefinition>();
                        foreach (var group in groupedTags)
                        {
                            string containerName = group.Key;
                            var tagsInContainer = group.Value;

                            // Обновляем статус в прогресс-окне
                            dispatcher.Invoke(() =>
                                progressWindow.SetStatus($"Поиск тегов в контейнере {containerName}..."));

                            // Определяем, PLC это или DB
                            bool isDbContainer = IsDbContainer(containerName);

                            // Выполняем поиск тегов в контейнере
                            var tagsFound = SearchTagsInContainer(containerName, tagsInContainer, isDbContainer);
                            foundTags.AddRange(tagsFound);

                            // Обновляем прогресс
                            dispatcher.Invoke(() =>
                                progressWindow.SetProgress(foundTags.Count, tagNames.Count));
                        }

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
                                MessageBox.Show("Не удалось найти ни один из введенных тегов",
                                    "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }

                            // Если найдены не все теги, показываем предупреждение
                            if (FoundTags.Count < tagNames.Count)
                            {
                                MessageBox.Show($"Найдено {FoundTags.Count} из {tagNames.Count} тегов",
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

        /// <summary>
        /// Группировка тегов по контейнерам
        /// </summary>
        private Dictionary<string, List<string>> GroupTagsByContainer(List<string> tagNames)
        {
            var result = new Dictionary<string, List<string>>();

            foreach (var fullTagName in tagNames)
            {
                var parsedTag = ParseTagName(fullTagName);

                if (!string.IsNullOrEmpty(parsedTag.Container))
                {
                    if (!result.ContainsKey(parsedTag.Container))
                    {
                        result[parsedTag.Container] = new List<string>();
                    }

                    // Для тегов DB добавляем относительный путь, для PLC - полное имя
                    result[parsedTag.Container].Add(
                        parsedTag.IsDB ? parsedTag.TagName : fullTagName);
                }
                else
                {
                    // Если не удалось определить контейнер, добавляем тег как есть
                    _logger.Warn($"Не удалось определить контейнер для тега {fullTagName}");

                    // Добавляем в дефолтный контейнер
                    if (!result.ContainsKey("Default"))
                    {
                        result["Default"] = new List<string>();
                    }

                    result["Default"].Add(fullTagName);
                }
            }

            return result;
        }

        /// <summary>
        /// Определение типа контейнера
        /// </summary>
        private bool IsDbContainer(string containerName)
        {
            // Проверяем, является ли контейнер блоком данных
            return containerName.StartsWith("DB", StringComparison.OrdinalIgnoreCase) ||
                   containerName.Contains("_DB", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Структура для разобранного имени тега
        /// </summary>
        private struct ParsedTag
        {
            public string Container;
            public string TagName;
            public bool IsDB;
        }

        /// <summary>
        /// Разбор полного имени тега на части
        /// </summary>
        private ParsedTag ParseTagName(string fullTagName)
        {
            // Результат по умолчанию
            ParsedTag result = new ParsedTag
            {
                Container = string.Empty,
                TagName = fullTagName,
                IsDB = false
            };

            try
            {
                // Проверяем наличие кавычек
                if (fullTagName.Contains("\""))
                {
                    // Ищем закрывающую кавычку
                    int endQuoteIndex = fullTagName.IndexOf("\"", 1);

                    if (endQuoteIndex > 0)
                    {
                        // Извлекаем контейнер (часть в кавычках)
                        result.Container = fullTagName.Substring(1, endQuoteIndex - 1);

                        // Проверяем, есть ли точка после закрывающей кавычки
                        if (fullTagName.Length > endQuoteIndex + 1 && fullTagName[endQuoteIndex + 1] == '.')
                        {
                            // Это тег DB, так как есть часть после точки
                            result.IsDB = true;
                            result.TagName = fullTagName.Substring(endQuoteIndex + 2); // Пропускаем кавычку и точку
                        }
                        else
                        {
                            // Это тег PLC, так как нет части после точки
                            result.IsDB = false;
                            // TagName остается полным именем, так как мы не можем определить только имя тега
                        }
                    }
                }
                else if (fullTagName.Contains("."))
                {
                    // Формат без кавычек, но с точкой
                    var parts = fullTagName.Split(new[] { '.' }, 2);

                    if (parts.Length >= 2)
                    {
                        result.Container = parts[0];
                        result.TagName = parts[1];
                        // Предполагаем, что это DB, если есть точка
                        result.IsDB = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при разборе имени тега {fullTagName}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Метод поиска тегов в контейнере
        /// </summary>
        private List<TagDefinition> SearchTagsInContainer(string containerName, List<string> tagNames, bool isDb)
        {
            List<TagDefinition> results = new List<TagDefinition>();

            try
            {
                // Проверяем, доступен ли TIA Portal сервис через коммуникационный сервис
                if (_communicationService is Communication.TIA.TiaPortalCommunicationService tiaService)
                {
                    // Используем метод поиска тегов из TIA Portal
                    // Формируем полные имена тегов в зависимости от типа (PLC или DB)
                    var fullTagNames = new List<string>();
                    foreach (var tagName in tagNames)
                    {
                        string fullName = isDb
                            ? $"\"{containerName}\".{tagName}"
                            : $"\"{containerName}\"";

                        fullTagNames.Add(fullName);
                    }

                    // Ищем теги в проекте TIA Portal
                    var foundTags = tiaService.SearchTagsByNames(fullTagNames);
                    return foundTags;
                }
                else
                {
                    // Если TIA Portal сервис недоступен, используем заглушку для демонстрации
                    _logger.Warn("TIA Portal сервис недоступен, используем заглушку");

                    foreach (var tagName in tagNames)
                    {
                        // Определяем тип тега по имени для демонстрации
                        TagDataType dataType = DetermineDataTypeByName(tagName);

                        // Создаем тег с соответствующим типом и данными
                        var tag = new TagDefinition
                        {
                            Id = Guid.NewGuid(),
                            Name = isDb ? tagName : tagName, // Для DB - относительное имя, для PLC - полное имя
                            GroupName = containerName,
                            IsDbTag = isDb,
                            DataType = dataType,
                            IsOptimized = isDb && containerName.Contains("S1"), // Демо-признак оптимизации
                            Comment = $"Тег из {(isDb ? "DB" : "PLC")} {containerName}"
                        };

                        results.Add(tag);
                        _logger.Info($"Найден тег: {tag.Name}, тип: {(isDb ? "DB" : "PLC")}, тип данных: {dataType}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов в контейнере {containerName}: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Определение типа данных тега по имени (для демонстрационных целей)
        /// </summary>
        private TagDataType DetermineDataTypeByName(string tagName)
        {
            string name = tagName.ToLower();

            // Теги с определенными окончаниями или содержащие определенные слова
            if (name.Contains("switch") ||
                name.Contains("enable") ||
                name.Contains("status") ||
                name.EndsWith("on") ||
                name.EndsWith("off") ||
                name.Contains("flag") ||
                name.EndsWith("width1"))  // Добавлено на основе примера из скриншота
            {
                return TagDataType.Bool;
            }

            if (name.Contains("count") ||
                name.Contains("index") ||
                name.Contains("number") ||
                name.EndsWith("width") ||  // Добавлено на основе примера из скриншота
                name.Contains("measurement.el_width"))  // Добавлено на основе примера из скриншота
            {
                return TagDataType.Int;
            }

            if (name.Contains("pos") ||
                name.Contains("speed") ||
                name.Contains("temp") ||
                name.Contains("pressure") ||
                name.Contains("level") ||
                name.Contains("pos.d2"))  // Добавлено на основе примера из скриншота
            {
                return TagDataType.Real;
            }

            if (name.Contains("correction") ||
                name.Contains("offset") ||
                name.Contains("counter") ||
                name.Contains("cascade"))  // Добавлено на основе примера из скриншота
            {
                return TagDataType.DInt;
            }

            // По умолчанию
            return TagDataType.DInt;
        }

        /// <summary>
        /// Метод для поиска тегов
        /// </summary>
        private List<TagDefinition> SearchTags(List<string> tagNames)
        {
            List<TagDefinition> results = new List<TagDefinition>();

            try
            {
                // Для демонстрационных целей, создаем тестовые теги
                foreach (var tagName in tagNames)
                {
                    // Определяем тип тега (PLC или DB) по формату имени
                    bool isDbTag = DetermineTagType(tagName);

                    // Получаем имя контейнера (группы/DB)
                    string groupName = GetGroupName(tagName);

                    // Создаем объект тега
                    var tag = new TagDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = tagName,
                        Address = isDbTag ? "" : $"I0.{results.Count}", // Пример адреса для PLC тега
                        DataType = GetTagDataType(tagName),
                        GroupName = groupName,
                        IsDbTag = isDbTag,
                        IsOptimized = isDbTag && tagName.Contains("S1"), // Демонстрационный признак оптимизации
                        Comment = $"Демо-тег для {tagName}"
                    };

                    results.Add(tag);
                    _logger.Info($"Найден тег: {tag.Name}, тип: {(isDbTag ? "DB" : "PLC")}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Определение типа тега по имени
        /// </summary>
        private bool DetermineTagType(string tagName)
        {
            // Если тег содержит кавычки и после кавычек есть точка, то это DB тег
            if (tagName.Contains("\""))
            {
                int endQuotePos = tagName.IndexOf("\"", 1);
                if (endQuotePos > 0 && endQuotePos < tagName.Length - 1)
                {
                    return tagName[endQuotePos + 1] == '.';
                }
            }

            // Если в имени есть точка, то, скорее всего, это DB тег
            return tagName.Contains(".");
        }

        /// <summary>
        /// Определение типа данных тега
        /// </summary>
        private TagDataType GetTagDataType(string tagName)
        {
            string name = tagName.ToLower();

            // Проверка на наличие явного указания типа в имени
            if (name.Contains("bool")) return TagDataType.Bool;
            if (name.Contains("int") && !name.Contains("dint")) return TagDataType.Int;
            if (name.Contains("dint")) return TagDataType.DInt;
            if (name.Contains("real")) return TagDataType.Real;

            // Эвристическое определение по имени тега
            return DetermineDataTypeByName(tagName);
        }
        /// <summary>
        /// Получение имени группы/блока данных из имени тега
        /// </summary>
        private string GetGroupName(string tagName)
        {
            if (tagName.Contains("\""))
            {
                int startQuote = tagName.IndexOf('\"');
                int endQuote = tagName.IndexOf('\"', startQuote + 1);
                if (startQuote >= 0 && endQuote > startQuote)
                {
                    return tagName.Substring(startQuote + 1, endQuote - startQuote - 1);
                }
            }
            else if (tagName.Contains("."))
            {
                return tagName.Split('.')[0];
            }

            return "DefaultGroup";
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