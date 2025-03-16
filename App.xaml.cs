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

            //try
            //{
            //    // Инициализируем резолвер для сборок Siemens.Engineering
            //    _logger.Info("Инициализация резолвера Siemens.Engineering");

            //    // Исправленный вызов инициализации резолвера
            //    //var resolver = new Siemens.Collaboration.Net.TiaPortal.Openness.Resolver.OpennessResolver.Initialize();

            //    // Если вышеуказанный вызов не работает, попробуйте один из следующих вариантов:
            //    // Вариант 1:
            //    // AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            //    // Вариант 2:
            //    // var resolver = new Siemens.Collaboration.Net.TiaPortal.Openness.Resolver.OpennessResolver();
            //    // resolver.Initialize();
            //}
            //catch (Exception ex)
            //{
            //    _logger.Error($"Ошибка при инициализации резолвера: {ex.Message}");
            //    MessageBox.Show($"Ошибка при инициализации резолвера Siemens.Engineering: {ex.Message}\n\n" +
            //                    "Убедитесь, что установлен TIA Portal и переустановите приложение.",
            //                    "Ошибка инициализации", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
        }



        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            string siemensEngineeringDllName = "Siemens.Engineering";
            string subKeyName = @"SOFTWARE\Siemens\Automation\Openness";
            var assemblyName = new AssemblyName(args.Name);
            if (!assemblyName.Name.StartsWith(siemensEngineeringDllName)) return null;

            using var regBaseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var opennessBaseKey = regBaseKey.OpenSubKey(subKeyName);
            using var registryKeyLatestTiaVersion = opennessBaseKey?.OpenSubKey(opennessBaseKey.GetSubKeyNames().Last());

            var requestedVersionOfAssembly = assemblyName.Version.ToString();
            using var assemblyVersionSubKey = registryKeyLatestTiaVersion
                ?.OpenSubKey("PublicAPI")
                ?.OpenSubKey(requestedVersionOfAssembly);

            var siemensEngineeringAssemblyPath = assemblyVersionSubKey?
                .GetValue(siemensEngineeringDllName).ToString();

            if (siemensEngineeringAssemblyPath == null || !File.Exists(siemensEngineeringAssemblyPath))
                return null;

            var assembly = Assembly.LoadFrom(siemensEngineeringAssemblyPath);
            return assembly;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _logger.Info("Приложение завершено");
        }

        //private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        //{
        //    _logger.Debug($"Запрос на загрузку сборки: {args.Name}");

        //    string assemblyName = new AssemblyName(args.Name).Name;

        //    // Обрабатываем только сборки Siemens.Engineering
        //    if (!assemblyName.StartsWith("Siemens.Engineering"))
        //        return null;

        //    // Пути поиска сборок Siemens.Engineering
        //    string[] searchPaths = new[]
        //    {
        //        @"C:\Program Files\Siemens\Automation\Portal V19\PublicAPI\V19",
        //        @"C:\Program Files\Siemens\Automation\Portal V18\PublicAPI\V18",
        //        @"C:\Program Files\Siemens\Automation\Portal V17\PublicAPI\V17",
        //        @"C:\Program Files\Siemens\Automation\Portal V16\PublicAPI\V16"
        //    };

        //    foreach (string path in searchPaths)
        //    {
        //        string dllPath = Path.Combine(path, $"{assemblyName}.dll");

        //        if (File.Exists(dllPath))
        //        {
        //            _logger.Info($"Загружена сборка: {dllPath}");
        //            return Assembly.LoadFrom(dllPath);
        //        }
        //    }

        //    _logger.Error($"Не удалось найти сборку: {assemblyName}");
        //    return null;
        //}

        //protected override void OnExit(ExitEventArgs e)
        //{
        //    base.OnExit(e);
        //    _logger.Info("Приложение завершено");
        //}
    }
}