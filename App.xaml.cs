using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Siemens.Collaboration.Net;
using SiemensTrend.Core.Logging;
using SiemensTrend.Storage.TagManagement;
using SiemensTrend.Communication;
using SiemensTrend.ViewModels;
using SiemensTrend.Views;

namespace SiemensTrend
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Logger _logger;
        private ServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Инициализируем логер
                _logger = new Logger("application.log");
                _logger.Info("Приложение запущено");

                // Проверяем, что мы в STA потоке
                if (System.Threading.Thread.CurrentThread.GetApartmentState() != System.Threading.ApartmentState.STA)
                {
                    _logger.Error("Приложение не запущено в режиме STA!");
                    MessageBox.Show("Приложение должно быть запущено в STA режиме для работы с TIA Portal Openness.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    _logger.Info("Приложение запущено в режиме STA");
                }

                // Инициализация Assembly Resolver для Siemens.Engineering.dll
                InitializeAssemblyResolver();

                // Настраиваем сервисы
                ConfigureServices();

                // Создаем и показываем главное окно
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске приложения: {ex.Message}\n\nStack trace: {ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Регистрируем сервисы
            services.AddSingleton<Logger>(_logger);

            // Регистрируем TagManager
            var tagManager = new TagManager(_logger);
            services.AddSingleton<TagManager>(tagManager);

            // Создаем и регистрируем ICommunicationService
            var communicationService = new Communication.S7.S7CommunicationService(
                _logger,
                new Communication.S7.S7ConnectionParameters
                {
                    IpAddress = "127.0.0.1" // Значение по умолчанию
                });
            services.AddSingleton<ICommunicationService>(communicationService);

            // Создаем экземпляр TiaPortalCommunicationService
            var tiaService = new Communication.TIA.TiaPortalCommunicationService(_logger);

            // Регистрируем ChartViewModel
            services.AddSingleton<ChartViewModel>();

            // Регистрируем MainViewModel после всех зависимостей
            services.AddSingleton<MainViewModel>();

            // Регистрируем окна и представления
            services.AddSingleton<MainWindow>();

            // Создаем провайдер сервисов
            _serviceProvider = services.BuildServiceProvider();
        }

        private void InitializeAssemblyResolver()
        {
            try
            {
                _logger.Info("Инициализация Assembly Resolver");

                // Используем NuGet пакет для резолва сборок Siemens.Engineering
                Api.Global.Openness().Initialize();

                _logger.Info("Assembly Resolver инициализирован успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при инициализации Assembly Resolver: {ex.Message}");

                // Резервный вариант - регистрируем собственный resolver
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            }
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            _logger.Debug($"Запрос на загрузку сборки: {args.Name}");

            string assemblyName = new AssemblyName(args.Name).Name;

            // Обрабатываем только сборки Siemens.Engineering
            if (!assemblyName.StartsWith("Siemens.Engineering"))
                return null;

            // Пути поиска сборок Siemens.Engineering
            string[] searchPaths = new[]
            {
                @"C:\Program Files\Siemens\Automation\Portal V19\PublicAPI\V19",
                @"C:\Program Files\Siemens\Automation\Portal V18\PublicAPI\V18",
                @"C:\Program Files\Siemens\Automation\Portal V17\PublicAPI\V17",
                @"C:\Program Files\Siemens\Automation\Portal V16\PublicAPI\V16"
            };

            foreach (string path in searchPaths)
            {
                string dllPath = Path.Combine(path, $"{assemblyName}.dll");

                if (File.Exists(dllPath))
                {
                    _logger.Info($"Загружена сборка: {dllPath}");
                    return Assembly.LoadFrom(dllPath);
                }
            }

            _logger.Error($"Не удалось найти сборку: {assemblyName}");
            return null;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _logger.Info("Приложение завершено");

            // Если используете Microsoft.Extensions.DependencyInjection, 
            // нужно освободить ресурсы при выходе
            if (_serviceProvider != null)
            {
                ((IDisposable)_serviceProvider).Dispose();
            }
        }
    }
}