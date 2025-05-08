using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Siemens.Engineering;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Адаптер для доступа к специфическим функциям TIA Portal
    /// </summary>
    public class TiaPortalServiceAdapter
    {
        private readonly TiaPortalCommunicationService _tiaService;
        private readonly Logger _logger;

        /// <summary>
        /// Конструктор
        /// </summary>
        public TiaPortalServiceAdapter(ICommunicationService communicationService, Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (communicationService is TiaPortalCommunicationService tiaService)
            {
                _tiaService = tiaService;
                _logger.Info("Адаптер TIA Portal инициализирован с сервисом TiaPortalCommunicationService");
            }
            else
            {
                _logger.Error($"Ожидался сервис TiaPortalCommunicationService, но получен {communicationService?.GetType().Name ?? "null"}");
                throw new ArgumentException("Сервис должен быть типа TiaPortalCommunicationService", nameof(communicationService));
            }
        }

        /// <summary>
        /// Текущий проект TIA Portal
        /// </summary>
        public Project CurrentProject => _tiaService.CurrentProject;

        /// <summary>
        /// Экземпляр TIA Portal
        /// </summary>
        public TiaPortal TiaPortalInstance => _tiaService.TiaPortalInstance;

        /// <summary>
        /// Соединение с TIA Portal
        /// </summary>
        public bool ConnectToTiaPortal()
        {
            try
            {
                _logger.Info("Адаптер: Подключение к TIA Portal");
                return _tiaService.ConnectToTiaPortal();
            }
            catch (Exception ex)
            {
                _logger.Error($"Адаптер: Ошибка при подключении к TIA Portal: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получение открытых проектов TIA Portal
        /// </summary>
        public List<TiaProjectInfo> GetOpenProjects()
        {
            try
            {
                _logger.Info("Адаптер: Получение открытых проектов");
                return _tiaService.GetOpenProjects();
            }
            catch (Exception ex)
            {
                _logger.Error($"Адаптер: Ошибка при получении открытых проектов: {ex.Message}");
                return new List<TiaProjectInfo>();
            }
        }

        /// <summary>
        /// Подключение к конкретному проекту TIA Portal
        /// </summary>
        public bool ConnectToSpecificTiaProject(TiaProjectInfo projectInfo)
        {
            try
            {
                _logger.Info($"Адаптер: Подключение к проекту {projectInfo?.Name ?? "null"}");
                return _tiaService.ConnectToSpecificTiaProject(projectInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Адаптер: Ошибка при подключении к проекту: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Открытие проекта TIA Portal
        /// </summary>
        public bool OpenTiaProject(string projectPath)
        {
            try
            {
                _logger.Info($"Адаптер: Открытие проекта {projectPath}");
                return _tiaService.OpenTiaProject(projectPath);
            }
            catch (Exception ex)
            {
                _logger.Error($"Адаптер: Ошибка при открытии проекта: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Поиск тегов по их именам
        /// </summary>
        public List<TagDefinition> SearchTagsByNames(List<string> tagNames)
        {
            try
            {
                _logger.Info($"Адаптер: Поиск тегов ({tagNames?.Count ?? 0})");
                return _tiaService.SearchTagsByNames(tagNames);
            }
            catch (Exception ex)
            {
                _logger.Error($"Адаптер: Ошибка при поиске тегов: {ex.Message}");
                return new List<TagDefinition>();
            }
        }

        /// <summary>
        /// Вывод информации о состоянии подключения в лог
        /// </summary>
        public void LogConnectionStatus()
        {
            try
            {
                _logger.Info("Адаптер: Запрос статуса подключения");
                _tiaService.LogConnectionStatus();
            }
            catch (Exception ex)
            {
                _logger.Error($"Адаптер: Ошибка при запросе статуса подключения: {ex.Message}");
            }
        }
    }
}
