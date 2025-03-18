using System;
using System.Collections.Generic;
using System.IO;
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

        public bool ConnectToTiaPortal()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Проверка запущенных экземпляров TIA Portal...";
                ProgressValue = 10;

                // Создаем сервис TIA Portal только если его еще нет
                if (_tiaPortalService == null)
                {
                    _logger.Info("Создание экземпляра TiaPortalCommunicationService");
                    _tiaPortalService = new Communication.TIA.TiaPortalCommunicationService(_logger);
                    _logger.Info("Экземпляр TiaPortalCommunicationService создан успешно");
                }
                else
                {
                    _logger.Info("Используется существующий экземпляр TiaPortalCommunicationService");
                }

                // Получаем список открытых проектов
                StatusMessage = "Получение списка открытых проектов...";
                _logger.Info("Запрос списка открытых проектов");
                // ВАЖНО: используем синхронные вызовы для TIA Portal API
                List<Communication.TIA.TiaProjectInfo> openProjects = _tiaPortalService.GetOpenProjects();
                _logger.Info($"Получено {openProjects.Count} открытых проектов");
                ProgressValue = 50;

                // Проверяем количество открытых проектов
                if (openProjects.Count == 0)
                {
                    // Нет открытых проектов, предлагаем выбрать файл проекта
                    StatusMessage = "Открытые проекты не найдены. Выберите файл проекта...";
                    _logger.Info("Открытые проекты не найдены. Потребуется выбрать файл.");
                    ProgressValue = 60;

                    // Возвращаем false чтобы в MainWindow вызвать диалог выбора проекта
                    IsLoading = false;
                    return false;
                }
                else if (openProjects.Count == 1)
                {
                    // Один проект - подключаемся к нему
                    StatusMessage = $"Подключение к проекту: {openProjects[0].Name}...";
                    _logger.Info($"Найден один открытый проект: {openProjects[0].Name}. Выполняем подключение.");
                    ProgressValue = 70;

                    // ВАЖНО: Выполняем ConnectToProject синхронно напрямую в текущем потоке STA
                    bool result = _tiaPortalService.ConnectToProject(openProjects[0]);

                    if (result)
                    {
                        // Успешное подключение
                        StatusMessage = $"Подключено к проекту: {openProjects[0].Name}";
                        _logger.Info($"Успешное подключение к проекту: {openProjects[0].Name}");
                        ProgressValue = 100;
                        IsConnected = true;

                        // Инициализируем обозреватель тегов после успешного подключения
                        InitializeTagBrowser();

                        return true;
                    }
                    else
                    {
                        // Ошибка подключения
                        StatusMessage = "Ошибка при подключении к TIA Portal";
                        _logger.Error("Ошибка при подключении к TIA Portal");
                        ProgressValue = 0;
                        IsConnected = false;
                        return false;
                    }
                }
                else
                {
                    // Несколько проектов - возвращаем список для выбора
                    StatusMessage = "Найдено несколько открытых проектов. Выберите один...";
                    _logger.Info($"Найдено несколько открытых проектов ({openProjects.Count}). Требуется выбор пользователя.");
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
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
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
        public bool ConnectToSpecificTiaProject(Communication.TIA.TiaProjectInfo projectInfo)
        {
            try
            {
                if (projectInfo == null)
                {
                    _logger.Error("ConnectToSpecificTiaProject: projectInfo не может быть null");
                    return false;
                }

                IsLoading = true;
                StatusMessage = $"Подключение к проекту {projectInfo.Name}...";
                _logger.Info($"ConnectToSpecificTiaProject: Подключение к проекту {projectInfo.Name}");
                ProgressValue = 60;

                // Создаем сервис TIA Portal только если его еще нет
                if (_tiaPortalService == null)
                {
                    _logger.Info("ConnectToSpecificTiaProject: Создание экземпляра TiaPortalCommunicationService");
                    _tiaPortalService = new Communication.TIA.TiaPortalCommunicationService(_logger);
                }

                // ВАЖНО: Выполняем ConnectToProject синхронно в текущем STA-потоке
                bool result = _tiaPortalService.ConnectToProject(projectInfo);

                if (result)
                {
                    // Успешное подключение
                    StatusMessage = $"Подключено к проекту: {projectInfo.Name}";
                    _logger.Info($"ConnectToSpecificTiaProject: Успешное подключение к проекту: {projectInfo.Name}");
                    ProgressValue = 100;
                    IsConnected = true;

                    // Инициализируем обозреватель тегов после успешного подключения
                    InitializeTagBrowser();

                    return true;
                }
                else
                {
                    // Ошибка подключения
                    StatusMessage = "Ошибка при подключении к TIA Portal";
                    _logger.Error("ConnectToSpecificTiaProject: Ошибка при подключении к TIA Portal");
                    ProgressValue = 0;
                    IsConnected = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"ConnectToSpecificTiaProject: Ошибка при подключении к проекту {projectInfo?.Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"ConnectToSpecificTiaProject: Внутренняя ошибка: {ex.InnerException.Message}");
                }
                StatusMessage = ($"Ошибка при подключении к проекту {projectInfo?.Name}");
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
        public bool OpenTiaProject(string projectPath)
        {
            try
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    _logger.Error("OpenTiaProject: путь к проекту не может быть пустым");
                    return false;
                }

                IsLoading = true;
                StatusMessage = $"Открытие проекта TIA Portal...";
                _logger.Info($"OpenTiaProject: Открытие проекта {projectPath}");
                ProgressValue = 20;

                // Создаем сервис TIA Portal только если его еще нет
                if (_tiaPortalService == null)
                {
                    _logger.Info("OpenTiaProject: Создание экземпляра TiaPortalCommunicationService");
                    _tiaPortalService = new Communication.TIA.TiaPortalCommunicationService(_logger);
                }

                // Предупреждаем пользователя о длительной операции
                StatusMessage = "Открытие проекта TIA Portal. Это может занять некоторое время...";
                ProgressValue = 30;

                // Показываем индикатор прогресса
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    _logger.Info("OpenTiaProject: Показываем индикатор прогресса");
                });

                // ВАЖНО: Открытие проекта выполняем синхронно
                // Это может занять длительное время, но выполняется в текущем STA-потоке
                // В MainWindow этот метод должен вызываться в обработчике события нажатия кнопки
                _logger.Info("OpenTiaProject: Начинаем открытие проекта");
                bool result = _tiaPortalService.OpenProjectSync(projectPath);
                _logger.Info($"OpenTiaProject: Результат открытия проекта: {result}");

                // Обрабатываем результат и обновляем UI
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    if (result)
                    {
                        // Успешное открытие
                        string projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath);
                        StatusMessage = $"Проект TIA Portal открыт успешно: {projectName}";
                        _logger.Info($"OpenTiaProject: Проект успешно открыт: {projectName}");
                        ProgressValue = 100;
                        IsConnected = true;  // Важно: явно устанавливаем этот флаг!

                        // Инициализируем обозреватель тегов после успешного подключения
                        InitializeTagBrowser();
                    }
                    else
                    {
                        StatusMessage = "Ошибка при открытии проекта TIA Portal";
                        _logger.Error("OpenTiaProject: Ошибка при открытии проекта TIA Portal");
                        ProgressValue = 0;
                        IsConnected = false;
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"OpenTiaProject: Ошибка при открытии проекта TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"OpenTiaProject: Внутренняя ошибка: {ex.InnerException.Message}");
                }
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