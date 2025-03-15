using System.Windows;
using SiemensTrend.Core.Logging;
using SiemensTrend.ViewModels;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly Logger _logger;

        public MainWindow()
        {
            InitializeComponent();

            // Создаем логер
            _logger = new Logger();

            // Создаем и устанавливаем модель представления
            _viewModel = new MainViewModel(_logger);
            DataContext = _viewModel;
        }
    }
}