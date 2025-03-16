using System;
using System.IO; // Добавляем это для File.Exists
using System.Reflection; // Для Assembly и ResolveEventArgs
using Microsoft.Win32; // Для работы с Registry
using System.Windows;
using SiemensTrend.Core.Logging;


namespace SiemensTrend
{
    public partial class App : Application
    {
        private static Logger _logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Инициализируем логер
            _logger = new Logger("application.log");
            _logger.Info("Приложение запущено");

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

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