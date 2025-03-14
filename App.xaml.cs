using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace Siemens_trend
{
    public partial class App : Application
    {
        private const string LogFile = "log.txt";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Log("🚀 OnStartup вызван!");
            Log("🚀 Приложение Siemens Trend запущено");

            AppDomain.CurrentDomain.AssemblyResolve += MyResolver;

            // Принудительно загружаем DLL (если получится)
            LoadSiemensDLL();

            // Открываем главное окно
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private static void LoadSiemensDLL()
        {
            string tiaPath = @"C:\Program Files\Siemens\Automation\Portal V19\PublicAPI\V19";
            string dllPath = Path.Combine(tiaPath, "Siemens.Engineering.dll");

            Log($"[LoadSiemensDLL] Проверяем: {dllPath}");

            if (File.Exists(dllPath))
            {
                try
                {
                    Assembly.LoadFrom(dllPath);
                    Log("✅ [LoadSiemensDLL] Siemens.Engineering.dll загружен успешно!");
                }
                catch (Exception ex)
                {
                    Log($"❌ [LoadSiemensDLL] Ошибка загрузки DLL: {ex.Message}");
                }
            }
            else
            {
                Log($"❌ [LoadSiemensDLL] Файл НЕ НАЙДЕН: {dllPath}");
            }
        }

        private static Assembly? MyResolver(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name;
            Log($"[AssemblyResolve] Запрос на загрузку: {args.Name}");

            string[] searchPaths = new[]
            {
                @"C:\Program Files\Siemens\Automation\Portal V19\PublicAPI\V19",
                @"C:\Program Files\Siemens\Automation\Portal V19\Bin\PublicAPI",
                @"C:\Program Files\Siemens\Automation\Portal V19\Bin\PublicAPI\Client"
            };

            if (assemblyName.StartsWith("Siemens trend.resources"))
            {
                string culture = new AssemblyName(args.Name).CultureName;
                string resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, culture, $"{assemblyName}.dll");

                if (File.Exists(resourcePath))
                {
                    Log($"[AssemblyResolve] Загружаем {resourcePath}");
                    return Assembly.LoadFrom(resourcePath);
                }
                else
                {
                    Log($"[AssemblyResolve] ❌ Файл ресурсов НЕ НАЙДЕН: {resourcePath}");
                }
            };

            foreach (string basePath in searchPaths)
            {
                string dllPath = Path.Combine(basePath, $"{assemblyName}.dll");
                if (File.Exists(dllPath))
                {
                    Log($"[AssemblyResolve] Загружаем {dllPath}");
                    return Assembly.LoadFrom(dllPath);
                }
            }

            Log($"[AssemblyResolve] ❌ Не удалось загрузить: {assemblyName}");
            return null;
        }

        private static void Log(string message)
        {
            string logMessage = $"{DateTime.Now:HH:mm:ss} {message}";
            File.AppendAllText(LogFile, logMessage + Environment.NewLine);
            Console.WriteLine(logMessage);
        }
    }
}