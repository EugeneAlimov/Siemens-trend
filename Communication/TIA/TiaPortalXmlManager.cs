using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiemensTrend.Core.Logging;
using SiemensTrend.Communication.TIA;
using static SiemensTrend.Communication.TIA.TiaPortalCommunicationService;
using Siemens.Engineering;

namespace SiemensTrend.Helpers
{
    /// <summary>
    /// Менеджер для работы с XML экспортами/импортами тегов и блоков данных из TIA Portal
    /// </summary>
    public partial class TiaPortalXmlManager
    {
        private readonly Logger _logger;
        private readonly string _baseExportPath;
        private string _currentProjectName = string.Empty;

        // Пути к директориям проекта - вычисляются на основе _currentProjectName
        private string _currentProjectPath => Path.Combine(_baseExportPath, _currentProjectName);
        private string _plcTagsPath => Path.Combine(_currentProjectPath, "TagTables");
        private string _dbExportsPath => Path.Combine(_currentProjectPath, "DB");

        // Управление отменой операций
        private CancellationTokenSource _cancellationTokenSource;

        // Ссылка на TiaPortalCommunicationService
        private readonly TiaPortalCommunicationService _tiaService;

        /// <summary>
        /// Конструктор с возможностью указать текущий проект и TiaPortalCommunicationService
        /// </summary>
        public TiaPortalXmlManager(Logger logger, TiaPortalCommunicationService tiaService = null, string currentProjectName = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tiaService = tiaService; // Сохраняем ссылку на TiaPortalCommunicationService
            _baseExportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TiaExports");
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Создаем базовую директорию, если она не существует
                Directory.CreateDirectory(_baseExportPath);
                _logger.Info($"TiaPortalXmlManager: Базовая директория для экспорта создана: {_baseExportPath}");

                // Если указано имя проекта, устанавливаем его
                if (!string.IsNullOrEmpty(currentProjectName))
                {
                    SetCurrentProject(currentProjectName);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"TiaPortalXmlManager: Ошибка при инициализации: {ex.Message}");
            }
        }

        /// <summary>
        /// Установка текущего проекта для работы с XML
        /// </summary>
        /// <param name="projectName">Название проекта TIA Portal</param>
        public void SetCurrentProject(string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                _logger.Warn("TiaPortalXmlManager: Попытка установить пустое имя проекта");
                return;
            }

            try
            {
                // Заменяем недопустимые символы в имени файла
                string safeName = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars()));
                _currentProjectName = safeName;

                // Создаем директории для текущего проекта
                Directory.CreateDirectory(_currentProjectPath);
                Directory.CreateDirectory(_plcTagsPath);
                Directory.CreateDirectory(_dbExportsPath);

                _logger.Info($"TiaPortalXmlManager: Установлен текущий проект: {projectName}");
                _logger.Info($"TiaPortalXmlManager: Пути экспорта: {_plcTagsPath}, {_dbExportsPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"TiaPortalXmlManager: Ошибка при установке текущего проекта: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт тегов в XML с возможностью выбора типа тегов
        /// </summary>
        public async Task ExportTagsToXml(ExportTagType tagType = ExportTagType.All)
        {
            if (!IsConnected || _project == null)
            {
                _logger.Error("ExportTagsToXml: Нет подключения к TIA Portal");
                return;
            }

            var plcSoftware = GetPlcSoftware();
            if (plcSoftware == null)
            {
                _logger.Error("ExportTagsToXml: Не удалось получить PlcSoftware");
                return;
            }

            // Сбрасываем токен отмены для новой операции
            _xmlManager.ResetCancellationToken();

            // Экспортируем в зависимости от типа
            try
            {
                _logger.Info($"ExportTagsToXml: Экспорт {tagType} тегов начат");

                switch (tagType)
                {
                    case ExportTagType.All:
                        // Экспортируем все типы тегов с использованием улучшенного подхода
                        await ExportTagsToXmlEnhanced(tagType);
                        break;
                    case ExportTagType.PlcTags:
                        // Только теги ПЛК - стандартный метод
                        _xmlManager.ExportTagTablesToXml(plcSoftware.TagTableGroup);
                        break;
                    case ExportTagType.DbTags:
                        // Только теги DB - улучшенный метод
                        _xmlManager.ExportEnhancedDataBlocksToXml(plcSoftware.BlockGroup);
                        break;
                    default:
                        _logger.Warn($"ExportTagsToXml: Неизвестный тип тегов: {tagType}");
                        break;
                }

                _logger.Info($"ExportTagsToXml: Экспорт {tagType} тегов завершен");
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXml: Ошибка при экспорте {tagType} тегов: {ex.Message}");
            }
        }
        /// <summary>
        /// Внутренний метод для экспорта всех тегов
        /// </summary>
        private async Task ExportTagsToXmlInternal(Siemens.Engineering.SW.PlcSoftware plcSoftware)
        {
            _logger.Info("ExportTagsToXmlInternal: Начало экспорта тегов в XML");

            try
            {
                // Экспорт таблиц тегов
                ExportTagTablesToXml(plcSoftware.TagTableGroup);

                // Проверяем отмену операции
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                // Экспорт блоков данных
                ExportDataBlocksToXml(plcSoftware.BlockGroup);

                // Ждем небольшую паузу для завершения операций
                await Task.Delay(100, _cancellationTokenSource.Token);

                _logger.Info("ExportTagsToXmlInternal: Экспорт завершен успешно");
            }
            catch (OperationCanceledException)
            {
                _logger.Info("ExportTagsToXmlInternal: Операция была отменена");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error($"ExportTagsToXmlInternal: Ошибка при экспорте: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Сброс токена отмены для новой операции
        /// </summary>
        public void ResetCancellationToken()
        {
            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                }
                _cancellationTokenSource = new CancellationTokenSource();
            }
            catch (Exception ex)
            {
                _logger.Error($"ResetCancellationToken: Ошибка: {ex.Message}");
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }
        /// <summary>
        /// Аварийное прерывание всех активных операций экспорта
        /// </summary>
        public void CancelAllExportOperations()
        {
            try
            {
                _logger.Info("CancelAllExportOperations: Запрошена остановка всех операций экспорта");

                // Отменяем все операции через токен отмены
                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                    _logger.Info("CancelAllExportOperations: Сигнал отмены отправлен");
                }

                // Создаем новый токен для будущих операций
                ResetCancellationToken();

                _logger.Info("CancelAllExportOperations: Операции экспорта остановлены");
            }
            catch (Exception ex)
            {
                _logger.Error($"CancelAllExportOperations: Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Сброс токена отмены для новой операции
        /// </summary>
        public void ResetCancellationToken()
        {
            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                }
                _cancellationTokenSource = new CancellationTokenSource();
            }
            catch (Exception ex)
            {
                _logger.Error($"ResetCancellationToken: Ошибка: {ex.Message}");
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }
    }
}