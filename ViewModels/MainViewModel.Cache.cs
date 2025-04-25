using System;
using System.Collections.Generic;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Часть MainViewModel для работы с кэшем
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// Проверка кэшированных данных проекта
        /// </summary>
        public bool CheckCachedProjectData(string projectName)
        {
            try
            {
                if (string.IsNullOrEmpty(projectName))
                {
                    _logger.Warn("CheckCachedProjectData: Имя проекта не может быть пустым");
                    return false;
                }

                // Проверяем, инициализирован ли TIA сервис
                if (_tiaPortalService == null)
                {
                    _logger.Info("CheckCachedProjectData: Создаем новый экземпляр TiaPortalCommunicationService");
                    _tiaPortalService = new Communication.TIA.TiaPortalCommunicationService(_logger);
                }

                // Получаем доступ к XML Manager через отражение или с помощью нового метода
                var xmlManager = GetXmlManager();
                if (xmlManager == null)
                {
                    _logger.Error("CheckCachedProjectData: Не удалось получить доступ к XmlManager");
                    return false;
                }

                // Проверяем наличие кэшированных данных
                var hasData = xmlManager.HasExportedDataForProject(projectName);
                _logger.Info($"CheckCachedProjectData: Проект {projectName} {(hasData ? "имеет" : "не имеет")} кэшированные данные");

                return hasData;
            }
            catch (Exception ex)
            {
                _logger.Error($"CheckCachedProjectData: Ошибка: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получение списка проектов в кэше
        /// </summary>
        public List<string> GetCachedProjects()
        {
            try
            {
                // Получаем XmlManager
                var xmlManager = GetXmlManager();
                if (xmlManager == null)
                {
                    _logger.Error("GetCachedProjects: Не удалось получить доступ к XmlManager");
                    return new List<string>();
                }

                // Получаем список проектов
                return xmlManager.GetCachedProjects();
            }
            catch (Exception ex)
            {
                _logger.Error($"GetCachedProjects: Ошибка: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Очистка кэша для указанного проекта
        /// </summary>
        public bool ClearProjectCache(string projectName)
        {
            try
            {
                if (string.IsNullOrEmpty(projectName))
                {
                    _logger.Warn("ClearProjectCache: Имя проекта не может быть пустым");
                    return false;
                }

                // Получаем XmlManager
                var xmlManager = GetXmlManager();
                if (xmlManager == null)
                {
                    _logger.Error("ClearProjectCache: Не удалось получить доступ к XmlManager");
                    return false;
                }

                // Очищаем кэш проекта
                bool result = xmlManager.ClearProjectCache(projectName);

                if (result)
                {
                    _logger.Info($"ClearProjectCache: Кэш проекта {projectName} успешно очищен");
                }
                else
                {
                    _logger.Warn($"ClearProjectCache: Не удалось очистить кэш проекта {projectName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"ClearProjectCache: Ошибка: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Очистка кэша текущего проекта
        /// </summary>
        public bool ClearCurrentProjectCache()
        {
            try
            {
                // Проверяем, есть ли подключение к проекту
                if (!IsConnected || _tiaPortalService == null || _tiaPortalService.CurrentProject == null)
                {
                    _logger.Warn("ClearCurrentProjectCache: Нет подключения к проекту");
                    return false;
                }

                // Получаем имя текущего проекта
                string projectName = _tiaPortalService.CurrentProject.Name;
                if (string.IsNullOrEmpty(projectName))
                {
                    _logger.Warn("ClearCurrentProjectCache: Не удалось получить имя текущего проекта");
                    return false;
                }

                // Очищаем кэш проекта
                return ClearProjectCache(projectName);
            }
            catch (Exception ex)
            {
                _logger.Error($"ClearCurrentProjectCache: Ошибка: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получение информации о состоянии кэша для проекта
        /// </summary>
        public Dictionary<string, object> GetCacheInfo(string projectName = null)
        {
            try
            {
                // Если имя проекта не указано, используем текущий проект
                string targetProject = projectName;
                if (string.IsNullOrEmpty(targetProject) && _tiaPortalService?.CurrentProject != null)
                {
                    targetProject = _tiaPortalService.CurrentProject.Name;
                }

                if (string.IsNullOrEmpty(targetProject))
                {
                    _logger.Warn("GetCacheInfo: Не указано имя проекта");
                    return new Dictionary<string, object> { { "error", "Не указано имя проекта" } };
                }

                // Получаем XmlManager
                var xmlManager = GetXmlManager();
                if (xmlManager == null)
                {
                    _logger.Error("GetCacheInfo: Не удалось получить доступ к XmlManager");
                    return new Dictionary<string, object> { { "error", "Ошибка доступа к XmlManager" } };
                }

                // Проверяем наличие метода GetCacheStatus в XmlManager
                var methodInfo = xmlManager.GetType().GetMethod("GetCacheStatus");
                if (methodInfo != null)
                {
                    // Если метод найден, вызываем его
                    return (Dictionary<string, object>)methodInfo.Invoke(xmlManager, new object[] { targetProject });
                }
                else
                {
                    // Если метод не найден, возвращаем базовую информацию
                    var info = new Dictionary<string, object>
                    {
                        { "projectName", targetProject },
                        { "hasData", xmlManager.HasExportedDataForProject(targetProject) }
                    };
                    return info;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"GetCacheInfo: Ошибка: {ex.Message}");
                return new Dictionary<string, object> { { "error", ex.Message } };
            }
        }
    }
}