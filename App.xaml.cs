using System;
using System.IO;
using System.Reflection;
using System.Windows;
using SiemensTrend.Core.Logging;
using Siemens.Collaboration.Net.TiaPortal.Openness.Resolver;
using Siemens.Collaboration.Net;

namespace SiemensTrend
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Logger _logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Инициализируем логер
                _logger = new Logger("application.log");
                _logger.Info("Приложение запущено");

                // Инициализация Assembly Resolver для Siemens.Engineering.dll
                InitializeAssemblyResolver();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске приложения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        }
    }
}