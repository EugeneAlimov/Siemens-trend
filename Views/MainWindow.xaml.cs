using System;
using System.Windows;
using System.Windows.Controls;
using SiemensTrend.Core.Logging;
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

            // Инициализируем начальное состояние
            UpdateConnectionState();
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
    }
}