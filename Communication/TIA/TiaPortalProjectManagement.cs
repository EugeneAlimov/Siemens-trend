using System;
using System.Collections.Generic;
using System.IO;
using Siemens.Engineering;
using SiemensTrend.Core.Logging;

namespace SiemensTrend.Communication.TIA
{
    /// <summary>
    /// Часть сервиса TiaPortalCommunicationService для управления проектами
    /// </summary>
    public partial class TiaPortalCommunicationService
    {
        /// <summary>
        /// Получение списка открытых проектов TIA Portal
        /// </summary>
        public List<TiaProjectInfo> GetOpenProjects()
        {
            List<TiaProjectInfo> projects = new List<TiaProjectInfo>();

            try
            {
                _logger.Info("Получение списка открытых проектов TIA Portal");
                var tiaProcesses = TiaPortal.GetProcesses();
                _logger.Info($"Найдено {tiaProcesses.Count} процессов TIA Portal");

                foreach (var process in tiaProcesses)
                {
                    try
                    {
                        if (process == null)
                        {
                            _logger.Warn("Пропуск null процесса TIA Portal");
                            continue;
                        }

                        // Подключаемся к процессу
                        _logger.Info($"Подключение к процессу TIA Portal {process.Id}");
                        var tiaPortal = process.Attach();

                        if (tiaPortal == null)
                        {
                            _logger.Warn($"Не удалось подключиться к процессу {process.Id}");
                            continue;
                        }

                        _logger.Info($"Успешное подключение к процессу TIA Portal {process.Id}");

                        // Проверяем, есть ли открытый проект
                        if (tiaPortal.Projects == null)
                        {
                            _logger.Warn($"Свойство Projects равно null для процесса {process.Id}");
                            continue;
                        }

                        int projectCount = tiaPortal.Projects.Count;
                        _logger.Info($"В процессе TIA Portal {process.Id} найдено {projectCount} проектов");

                        if (projectCount > 0)
                        {
                            foreach (var project in tiaPortal.Projects)
                            {
                                try
                                {
                                    if (project == null)
                                    {
                                        _logger.Warn("Пропуск null проекта");
                                        continue;
                                    }

                                    var projectInfo = new TiaProjectInfo
                                    {
                                        Name = project.Name,
                                        Path = project.Path?.ToString() ?? "Неизвестно",
                                        TiaProcess = process,
                                        TiaPortalInstance = tiaPortal,
                                        Project = project
                                    };

                                    projects.Add(projectInfo);
                                    _logger.Info($"Найден открытый проект: {project.Name} в процессе {process.Id}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.Error($"Ошибка при получении информации о проекте: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            _logger.Info($"В процессе TIA Portal {process.Id} нет открытых проектов");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при подключении к процессу TIA Portal {process.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении списка открытых проектов TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }

            return projects;
        }

        /// <summary>
        /// Подключение к выбранному проекту
        /// </summary>
        public bool ConnectToProject(TiaProjectInfo projectInfo)
        {
            try
            {
                // Проверяем входные параметры
                if (projectInfo == null)
                {
                    _logger.Error("ConnectToProject: projectInfo не может быть null");
                    return false;
                }

                if (projectInfo.Project == null)
                {
                    _logger.Error("ConnectToProject: projectInfo.Project не может быть null");
                    return false;
                }

                if (projectInfo.TiaPortalInstance == null)
                {
                    _logger.Error("ConnectToProject: projectInfo.TiaPortalInstance не может быть null");
                    return false;
                }

                _logger.Info($"ConnectToProject: Подключение к проекту: {projectInfo.Name}");

                // Если уже подключены к какому-то проекту, сначала отключаемся
                if (_isConnected || _project != null || _tiaPortal != null)
                {
                    _logger.Info("ConnectToProject: Обнаружено активное подключение, выполняем отключение");
                    Disconnect();
                }

                // Сохраняем ссылки на объекты TIA Portal
                _tiaPortal = projectInfo.TiaPortalInstance;
                _project = projectInfo.Project;

                // Немедленно проверяем, что _project не null
                if (_project == null)
                {
                    _logger.Error("ConnectToProject: После присвоения _project оказался null");
                    return false;
                }

                // Устанавливаем текущий проект для XML-менеджера
                SetCurrentProjectInXmlManager();

                // Логируем информацию о проекте
                try
                {
                    _logger.Info($"ConnectToProject: Проект: {_project.Name}, Путь: {_project.Path}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ConnectToProject: Не удалось получить информацию о проекте: {ex.Message}");
                    // Не прерываем выполнение, так как это не критично
                }

                // Проверяем доступность проекта, пытаясь обратиться к его свойствам
                try
                {
                    // Пытаемся обратиться к свойствам проекта для проверки
                    var devices = _project.Devices.Count;
                    _logger.Info($"ConnectToProject: Количество устройств в проекте: {devices}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ConnectToProject: Ошибка при проверке свойств проекта: {ex.Message}");
                    Disconnect(); // Отключаемся при ошибке
                    return false;
                }

                // Создаем читатель тегов только после успешной проверки проекта
                try
                {
                    _tagReader = new TiaPortalTagReader(_logger, this);
                    _logger.Info("ConnectToProject: TiaPortalTagReader создан успешно");
                }
                catch (Exception ex)
                {
                    _logger.Error($"ConnectToProject: Ошибка при создании TiaPortalTagReader: {ex.Message}");
                    Disconnect(); // Отключаемся при ошибке
                    return false;
                }

                // Устанавливаем флаг подключения
                _isConnected = true;
                _logger.Info($"ConnectToProject: Успешное подключение к проекту: {_project.Name}");

                // Даем немного времени для завершения операций в UI
                System.Windows.Forms.Application.DoEvents();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"ConnectToProject: Ошибка при подключении к проекту: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"ConnectToProject: Внутренняя ошибка: {ex.InnerException.Message}");
                }

                // Сбрасываем состояние в случае ошибки
                _tiaPortal = null;
                _project = null;
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Открытие проекта TIA Portal (синхронный метод)
        /// </summary>
        public bool OpenProjectSync(string projectPath)
        {
            try
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    _logger.Error("OpenProjectSync: путь к проекту не может быть пустым");
                    return false;
                }

                _logger.Info($"OpenProjectSync: Открытие проекта TIA Portal: {projectPath}");

                // Если уже подключены к какому-то проекту, сначала отключаемся
                if (_isConnected || _project != null || _tiaPortal != null)
                {
                    _logger.Info("OpenProjectSync: Обнаружено активное подключение, выполняем отключение");
                    Disconnect();
                }

                bool openResult = false;
                TiaPortal createdTiaPortal = null;

                try
                {
                    // Создаем новый экземпляр TIA Portal с пользовательским интерфейсом
                    _logger.Info("OpenProjectSync: Создание нового экземпляра TIA Portal");
                    createdTiaPortal = new TiaPortal(TiaPortalMode.WithUserInterface);
                    _logger.Info("OpenProjectSync: Экземпляр TIA Portal создан успешно");

                    // Проверяем существование файла проекта
                    if (!File.Exists(projectPath))
                    {
                        _logger.Error($"OpenProjectSync: Файл проекта не существует: {projectPath}");

                        // Освобождаем ресурсы
                        if (createdTiaPortal != null)
                        {
                            createdTiaPortal.Dispose();
                        }

                        return false;
                    }

                    // Создаем FileInfo для проекта
                    var projectFile = new FileInfo(projectPath);

                    _logger.Info($"OpenProjectSync: Попытка открыть проект {projectPath}");

                    // Открываем проект напрямую (синхронно)
                    _project = createdTiaPortal.Projects.Open(projectFile);

                    // Обработка UI-событий во время открытия проекта
                    System.Windows.Forms.Application.DoEvents();

                    // Если проект успешно открыт
                    if (_project != null)
                    {
                        _logger.Info($"OpenProjectSync: Проект успешно открыт: {_project.Name}");

                        // Устанавливаем TiaPortal и флаги состояния
                        _tiaPortal = createdTiaPortal;

                        // Устанавливаем текущий проект для XML-менеджера
                        SetCurrentProjectInXmlManager();

                        openResult = true;
                    }
                    else
                    {
                        _logger.Error("OpenProjectSync: Проект не удалось открыть, Projects.Open вернул null");

                        // Освобождаем ресурсы
                        if (createdTiaPortal != null)
                        {
                            createdTiaPortal.Dispose();
                        }

                        openResult = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"OpenProjectSync: Ошибка при открытии проекта: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error($"OpenProjectSync: Внутренняя ошибка: {ex.InnerException.Message}");
                    }
                    openResult = false;

                    // Освобождаем ресурсы при ошибке
                    try
                    {
                        if (createdTiaPortal != null)
                        {
                            createdTiaPortal.Dispose();
                        }
                    }
                    catch (Exception disposeEx)
                    {
                        _logger.Error($"OpenProjectSync: Ошибка при освобождении ресурсов: {disposeEx.Message}");
                    }

                    return false;
                }

                if (openResult)
                {
                    // Создаем читатель тегов после успешного открытия проекта
                    try
                    {
                        _tagReader = new TiaPortalTagReader(_logger, this);
                        _logger.Info("OpenProjectSync: TiaPortalTagReader создан успешно");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"OpenProjectSync: Ошибка при создании TiaPortalTagReader: {ex.Message}");
                        return false;
                    }

                    _isConnected = true;
                    _logger.Info($"OpenProjectSync: Проект успешно открыт: {_project.Name}");
                    return true;
                }
                else
                {
                    _isConnected = false;
                    _tiaPortal = null;
                    _project = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"OpenProjectSync: Ошибка при открытии проекта TIA Portal: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"OpenProjectSync: Внутренняя ошибка: {ex.InnerException.Message}");
                }

                // Сбрасываем состояние в случае ошибки
                _isConnected = false;
                _tiaPortal = null;
                _project = null;
                return false;
            }
        }
    }
}