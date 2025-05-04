using System;
using System.Collections.Generic;
using Siemens.Engineering;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;


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


        /// <summary>
        /// Текущий проект TIA Portal
        /// </summary>
        public Project CurrentProject => _project;

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
            //_xmlManager = new TiaPortalXmlManager(_logger, this);
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

                // Освобождаем ресурсы
                //_tagReader = null;
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
        /// Установка текущего проекта для работы с XML
        /// </summary>
        public void SetCurrentProjectInXmlManager()
        {
            if (_project != null)
            {
                try
                {
                    string projectName = _project.Name;
                    //_xmlManager.SetCurrentProject(projectName);
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
        /// Метод для поиска тегов по их именам
        /// </summary>
        public List<TagDefinition> FindTagsByNames(List<string> tagNames)
        {
            try
            {
                _logger.Info($"Поиск тегов по именам ({tagNames.Count} тегов)");

                if (!_isConnected || _tiaPortal == null)
                {
                    _logger.Error("Попытка поиска тегов без подключения к TIA Portal");
                    return new List<TagDefinition>();
                }

                // Создаем объект для поиска тегов
                var tagFinder = new TiaPortalTagFinder(_logger, _tiaPortal);

                // Выполняем поиск
                var foundTags = tagFinder.FindTags(tagNames);

                _logger.Info($"Найдено {foundTags.Count} тегов");

                return foundTags;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске тегов: {ex.Message}");
                return new List<TagDefinition>();
            }
        }
    }
}