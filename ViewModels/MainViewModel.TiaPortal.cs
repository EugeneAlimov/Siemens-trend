using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SiemensTrend.Communication.TIA;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Часть MainViewModel для работы с TIA Portal
    /// </summary>
    public partial class MainViewModel
    {
        // Объект для работы с TIA Portal
        private TiaPortalCommunicationService _tiaPortalService;

        /// <summary>
        /// Список проектов TIA Portal для выбора
        /// </summary>
        public List<TiaProjectInfo> TiaProjects { get; private set; }

        /// <summary>
        /// Соединение с TIA Portal
        /// </summary>
        /// <returns>True если подключение успешно</returns>
        public async Task<bool> ConnectToTiaPortalAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Проверка запущенных экземпляров TIA Portal...";
                ProgressValue = 10;

                // Создаем сервис TIA Portal если еще не создан
                if (_tiaPortalService == null)
                {
                    _tiaPortalService = new TiaPortalCommunicationService(_logger);
                }

                // Получаем список открытых проектов
                StatusMessage = "Получение списка открытых проектов...";
                List<TiaProjectInfo> openProjects = _tiaPortalService.GetOpenProjects();
                ProgressValue = 50;

                // Проверяем количество открытых проектов
                if (openProjects.Count == 0)
                {
                    // Нет открытых проектов, предлагаем выбрать файл проекта
                    StatusMessage = "Открытые проекты не найдены. Выберите файл проекта...";
                    ProgressValue = 60;

                    // Возвращаем false чтобы в MainWindow вызвать диалог выбора проекта
                    IsLoading = false;
                    return false;
                }
                else if (openProjects.Count == 1)
                {
                    // Один проект - подключаемся к нему
                    StatusMessage = $"Подключение к проекту: {openProjects[0].Name}...";
                    ProgressValue = 70;

                    // Подключаемся к проекту
                    bool result = _tiaPortalService.ConnectToProject(openProjects[0]);

                    if (result)
                    {
                        // Успешное подключение
                        StatusMessage = $"Подключено к проекту: {openProjects[0].Name}";
                        ProgressValue = 100;
                        IsConnected = true;
                        return true;
                    }
                    else
                    {
                        // Ошибка подключения
                        StatusMessage = "Ошибка при подключении к TIA Portal";
                        ProgressValue = 0;
                        IsConnected = false;
                        return false;
                    }
                }
                else
                {
                    // Несколько проектов - возвращаем список для выбора
                    StatusMessage = "Найдено несколько открытых проектов. Выберите один...";
                    ProgressValue = 60;

                    // Сохраняем список проектов для последующего выбора
                    TiaProjects = openProjects;

                    // Возвращаем false чтобы в MainWindow показать диалог выбора
                    IsLoading = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении к TIA Portal: {ex.Message}");
                StatusMessage = "Ошибка при подключении к TIA Portal";
                ProgressValue = 0;
                IsConnected = false;
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Подключение к конкретному проекту TIA Portal
        /// </summary>
        /// <param name="projectInfo">Информация о проекте</param>
        /// <returns>True если подключение успешно</returns>
        public async Task<bool> ConnectToSpecificTiaProjectAsync(TiaProjectInfo projectInfo)
        {
            try
            {
                IsLoading = true;
                StatusMessage = $"Подключение к проекту {projectInfo.Name}...";
                ProgressValue = 60;

                // Создаем сервис TIA Portal если еще не создан
                if (_tiaPortalService == null)
                {
                    _tiaPortalService = new TiaPortalCommunicationService(_logger);
                }

                // Подключаемся к проекту
                bool result = _tiaPortalService.ConnectToProject(projectInfo);

                if (result)
                {
                    // Успешное подключение
                    StatusMessage = $"Подключено к проекту: {projectInfo.Name}";
                    ProgressValue = 100;
                    IsConnected = true;
                    return true;
                }
                else
                {
                    // Ошибка подключения
                    StatusMessage = "Ошибка при подключении к TIA Portal";
                    ProgressValue = 0;
                    IsConnected = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при подключении к проекту {projectInfo.Name}: {ex.Message}");
                StatusMessage = ($"Ошибка при подключении к проекту {projectInfo.Name}");
                ProgressValue = 0;
                IsConnected = false;
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Открытие проекта TIA Portal
        /// </summary>
        /// <param name="projectPath">Путь к файлу проекта TIA Portal</param>
        /// <returns>True если проект успешно открыт</returns>
        public async Task<bool> OpenTiaProjectAsync(string projectPath)
        {
            try
            {
                IsLoading = true;
                StatusMessage = $"Открытие проекта TIA Portal...";
                ProgressValue = 20;

                // Создаем сервис TIA Portal если еще не создан
                if (_tiaPortalService == null)
                {
                    _tiaPortalService = new TiaPortalCommunicationService(_logger);
                }

                // Открываем проект
                bool result = await _tiaPortalService.OpenProjectAsync(projectPath);

                if (result)
                {
                    // Успешное открытие
                    string projectName = Path.GetFileNameWithoutExtension(projectPath);
                    StatusMessage = $"Проект TIA Portal открыт успешно: {projectName}";
                    ProgressValue = 100;
                    IsConnected = true;
                    return true;
                }
                else
                {
                    // Ошибка открытия
                    StatusMessage = "Ошибка при открытии проекта TIA Portal";
                    ProgressValue = 0;
                    IsConnected = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при открытии проекта TIA Portal: {ex.Message}");
                StatusMessage = "Ошибка при открытии проекта TIA Portal";
                ProgressValue = 0;
                IsConnected = false;
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}