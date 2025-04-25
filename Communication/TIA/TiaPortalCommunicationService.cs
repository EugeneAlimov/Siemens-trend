using System;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using SiemensTrend.Core.Logging;
using SiemensTrend.Helpers;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Сервис для коммуникации с TIA Portal - основной класс
    /// </summary>
    public partial class TiaPortalCommunicationService
    {
        private readonly Logger _logger;
        private TiaPortal _tiaPortal;
        private Project _project;
        private bool _isConnected;
        private TiaPortalTagReader _tagReader;
        private readonly TiaPortalXmlManager _xmlManager;

        /// <summary>
        /// Флаг подключения к TIA Portal
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Текущий проект TIA Portal
        /// </summary>
        public Project CurrentProject => _project;

        /// <summary>
        /// Доступ к XML-менеджеру
        /// </summary>
        public TiaPortalXmlManager XmlManager => _xmlManager;

        /// <summary>
        /// Типы тегов для экспорта
        /// </summary>
        public enum ExportTagType
        {
            All,
            PlcTags,
            DbTags
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public TiaPortalCommunicationService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Info("Создание экземпляра TiaPortalCommunicationService");
            _xmlManager = new TiaPortalXmlManager(_logger, this);
            _logger.Info("Экземпляр TiaPortalCommunicationService создан успешно");
        }

        /// <summary>
        /// Отключение от TIA Portal
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _logger.Info("Disconnect: Отключение от TIA Portal");

                // Останавливаем все операции перед отключением
                try
                {
                    // Если есть активные асинхронные операции, отменяем их
                    _xmlManager?.CancelAllExportOperations();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Disconnect: Ошибка при остановке операций: {ex.Message}");
                }

                // Освобождаем ресурсы
                _tagReader = null;
                _project = null;
                _tiaPortal = null;
                _isConnected = false;

                // Принудительно запускаем сборщик мусора для освобождения COM-объектов
                GC.Collect();
                GC.WaitForPendingFinalizers();

                _logger.Info("Disconnect: Отключение от TIA Portal выполнено успешно");
            }
            catch (Exception ex)
            {
                _logger.Error($"Disconnect: Ошибка при отключении от TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Disconnect: Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Получение PlcSoftware из проекта
        /// </summary>
        public PlcSoftware GetPlcSoftware()
        {
            // Проверяем, есть ли активное подключение к проекту
            if (_project == null)
            {
                _logger.Error("GetPlcSoftware: Проект TIA Portal не открыт");
                return null;
            }

            _logger.Info($"GetPlcSoftware: Поиск PLC Software в проекте {_project.Name}");

            try
            {
                // Проверяем наличие устройств в проекте
                if (_project.Devices == null || _project.Devices.Count == 0)
                {
                    _logger.Error("GetPlcSoftware: В проекте нет устройств");
                    return null;
                }

                _logger.Info($"GetPlcSoftware: Количество устройств в проекте: {_project.Devices.Count}");

                // Перебираем все устройства в проекте
                foreach (var device in _project.Devices)
                {
                    try
                    {
                        if (device == null) continue;

                        _logger.Info($"GetPlcSoftware: Проверка устройства {device.Name} (Тип: {device.TypeIdentifier})");

                        // Проверяем элементы устройства
                        if (device.DeviceItems == null || device.DeviceItems.Count == 0)
                        {
                            _logger.Info($"GetPlcSoftware: Устройство {device.Name} не содержит элементов");
                            continue;
                        }

                        _logger.Info($"GetPlcSoftware: Количество элементов устройства {device.Name}: {device.DeviceItems.Count}");

                        // Перебираем элементы устройства
                        foreach (var deviceItem in device.DeviceItems)
                        {
                            try
                            {
                                if (deviceItem == null) continue;

                                _logger.Info($"GetPlcSoftware: Проверка элемента устройства {deviceItem.Name}");

                                // Получаем SoftwareContainer
                                var softwareContainer = deviceItem.GetService<Siemens.Engineering.HW.Features.SoftwareContainer>();
                                if (softwareContainer == null)
                                {
                                    _logger.Info($"GetPlcSoftware: Элемент {deviceItem.Name} не содержит SoftwareContainer");
                                    continue;
                                }

                                _logger.Info($"GetPlcSoftware: SoftwareContainer найден для элемента {deviceItem.Name}");

                                // Проверяем, содержит ли SoftwareContainer PlcSoftware
                                if (softwareContainer.Software is PlcSoftware plcSoftware)
                                {
                                    _logger.Info($"GetPlcSoftware: PlcSoftware найден в устройстве {device.Name}, элемент {deviceItem.Name}");
                                    return plcSoftware;
                                }
                                else
                                {
                                    _logger.Info($"GetPlcSoftware: Элемент {deviceItem.Name} не содержит PlcSoftware");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"GetPlcSoftware: Ошибка при проверке элемента устройства {deviceItem.Name}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"GetPlcSoftware: Ошибка при проверке устройства {device.Name}: {ex.Message}");
                    }
                }

                _logger.Error("GetPlcSoftware: PlcSoftware не найден ни в одном устройстве проекта");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetPlcSoftware: Ошибка при поиске PlcSoftware: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"GetPlcSoftware: Внутренняя ошибка: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// Установка текущего проекта для работы с XML
        /// </summary>
        public void SetCurrentProjectInXmlManager()
        {
            if (_project != null)
            {
                try
                {
                    string projectName = _project.Name;
                    _xmlManager.SetCurrentProject(projectName);
                    _logger.Info($"SetCurrentProjectInXmlManager: Установлен проект {projectName} для работы с XML");
                }
                catch (Exception ex)
                {
                    _logger.Error($"SetCurrentProjectInXmlManager: Ошибка при установке текущего проекта: {ex.Message}");
                }
            }
            else
            {
                _logger.Warn("SetCurrentProjectInXmlManager: Нет активного проекта");
            }
        }

        /// <summary>
        /// Задание текущего проекта для XML-менеджера
        /// </summary>
        /// <param name="projectName">Имя проекта</param>
        public void SetXmlManagerProject(string projectName)
        {
            if (_xmlManager != null && !string.IsNullOrEmpty(projectName))
            {
                _xmlManager.SetCurrentProject(projectName);
                _logger.Info($"SetXmlManagerProject: Установлен проект {projectName}");
            }
            else
            {
                _logger.Warn($"SetXmlManagerProject: Невозможно установить проект {projectName} (XML-менеджер не инициализирован или имя проекта пустое)");
            }
        }
    }
}