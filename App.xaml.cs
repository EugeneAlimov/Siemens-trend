using System;
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

            try
            {
                // Инициализируем резолвер для сборок Siemens.Engineering из пакета
                _logger.Info("Инициализация резолвера Siemens.Engineering");
                Siemens.Collaboration.Net.TiaPortal.Openness.Resolver.Api.Global.Openness().Initialize();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при инициализации резолвера: {ex.Message}");
                MessageBox.Show($"Ошибка при инициализации резолвера Siemens.Engineering: {ex.Message}\n\n" +
                                "Убедитесь, что установлен TIA Portal и переустановите приложение.",
                                "Ошибка инициализации", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _logger.Info("Приложение завершено");
        }
    }
}