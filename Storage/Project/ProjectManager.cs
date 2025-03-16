using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;
using System.Linq;

namespace SiemensTrend.Storage.Project
{
    /// <summary>
    /// Расширенный класс для управления проектами
    /// </summary>
    public class ProjectManager
    {
        private readonly Logger _logger;
        private ProjectConfiguration _currentProject;
        private readonly string _projectsFolderPath;
        private readonly string _projectFileExtension = ".spt"; // Siemens Project Trend

        /// <summary>
        /// Событие изменения проекта
        /// </summary>
        public event EventHandler<ProjectConfiguration> ProjectChanged;

        /// <summary>
        /// Текущий проект
        /// </summary>
        public ProjectConfiguration CurrentProject
        {
            get => _currentProject;
            private set
            {
                _currentProject = value;
                ProjectChanged?.Invoke(this, value);
            }
        }

        /// <summary>
        /// Последний использованный путь к проекту
        /// </summary>
        public string LastProjectPath { get; private set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public ProjectManager(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Определяем путь к папке проектов
            _projectsFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SiemensTrend", "Projects");

            // Создаем папку, если её нет
            if (!Directory.Exists(_projectsFolderPath))
            {
                Directory.CreateDirectory(_projectsFolderPath);
            }

            // Создаем пустой проект по умолчанию
            CurrentProject = new ProjectConfiguration
            {
                Name = "Новый проект",
                Description = "Описание проекта",
                Created = DateTime.Now,
                LastModified = DateTime.Now
            };
        }

        /// <summary>
        /// Создание нового проекта
        /// </summary>
        public void CreateNewProject(string name, string description = null)
        {
            CurrentProject = new ProjectConfiguration
            {
                Name = name,
                Description = description ?? "Описание проекта",
                Created = DateTime.Now,
                LastModified = DateTime.Now,
                ConnectionSettings = new ConnectionSettings()
            };

            _logger.Info($"Создан новый проект: {name}");

            // Очищаем последний путь
            LastProjectPath = null;
        }

        /// <summary>
        /// Сохранение проекта в файл
        /// </summary>
        public async Task<bool> SaveProjectAsync(string filePath = null)
        {
            if (CurrentProject == null)
            {
                _logger.Error("Нет активного проекта для сохранения");
                return false;
            }

            try
            {
                // Если путь не указан, используем последний или создаем новый на основе имени проекта
                if (string.IsNullOrEmpty(filePath))
                {
                    if (!string.IsNullOrEmpty(LastProjectPath))
                    {
                        filePath = LastProjectPath;
                    }
                    else
                    {
                        // Создаем безопасное имя файла из имени проекта
                        string safeName = string.Join("_", CurrentProject.Name.Split(Path.GetInvalidFileNameChars()));
                        filePath = Path.Combine(_projectsFolderPath, safeName + _projectFileExtension);
                    }
                }

                // Обновляем дату последнего изменения
                CurrentProject.LastModified = DateTime.Now;

                // Сериализуем проект в XML
                var serializer = new XmlSerializer(typeof(ProjectConfiguration));

                // Создаем директорию, если она не существует
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    // Сериализуем проект
                    await Task.Run(() => serializer.Serialize(stream, CurrentProject));
                }

                // Сохраняем путь
                LastProjectPath = filePath;

                // Добавляем проект в список последних
                AddToRecentProjects(filePath);

                _logger.Info($"Проект успешно сохранен: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при сохранении проекта: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Загрузка проекта из файла
        /// </summary>
        public async Task<bool> LoadProjectAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.Error($"Файл проекта не найден: {filePath}");
                    return false;
                }

                // Десериализуем проект из XML
                var serializer = new XmlSerializer(typeof(ProjectConfiguration));

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Десериализуем проект
                    var project = await Task.Run(() => (ProjectConfiguration)serializer.Deserialize(stream));

                    // Устанавливаем загруженный проект как текущий
                    CurrentProject = project;

                    // Обновляем дату последнего изменения
                    CurrentProject.LastModified = DateTime.Now;
                }

                // Сохраняем путь
                LastProjectPath = filePath;

                // Добавляем проект в список последних
                AddToRecentProjects(filePath);

                _logger.Info($"Проект успешно загружен: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при загрузке проекта: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получение списка последних проектов
        /// </summary>
        public List<RecentProjectInfo> GetRecentProjects()
        {
            try
            {
                // Путь к файлу с последними проектами
                string recentProjectsFile = Path.Combine(_projectsFolderPath, "recent.txt");

                // Если файл не существует, возвращаем пустой список
                if (!File.Exists(recentProjectsFile))
                {
                    return new List<RecentProjectInfo>();
                }

                // Читаем файл
                var lines = File.ReadAllLines(recentProjectsFile);

                // Создаем список последних проектов
                var recentProjects = new List<RecentProjectInfo>();

                foreach (var line in lines)
                {
                    // Формат строки: путь|дата|имя
                    var parts = line.Split('|');
                    if (parts.Length >= 3 && File.Exists(parts[0]))
                    {
                        DateTime.TryParse(parts[1], out DateTime lastOpened);

                        recentProjects.Add(new RecentProjectInfo
                        {
                            Path = parts[0],
                            LastOpened = lastOpened,
                            Name = parts[2]
                        });
                    }
                }

                // Сортируем по дате (сначала новые)
                return recentProjects.OrderByDescending(p => p.LastOpened).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении списка последних проектов: {ex.Message}");
                return new List<RecentProjectInfo>();
            }
        }

        /// <summary>
        /// Добавление проекта в список последних
        /// </summary>
        private void AddToRecentProjects(string filePath)
        {
            try
            {
                // Путь к файлу с последними проектами
                string recentProjectsFile = Path.Combine(_projectsFolderPath, "recent.txt");

                // Список строк с последними проектами
                List<string> lines = new List<string>();

                // Если файл существует, читаем его
                if (File.Exists(recentProjectsFile))
                {
                    lines = File.ReadAllLines(recentProjectsFile).ToList();
                }

                // Удаляем старую запись об этом проекте, если есть
                lines = lines.Where(l => !l.StartsWith(filePath + "|")).ToList();

                // Добавляем новую запись в начало
                lines.Insert(0, $"{filePath}|{DateTime.Now}|{CurrentProject.Name}");

                // Ограничиваем список 10 проектами
                if (lines.Count > 10)
                {
                    lines = lines.Take(10).ToList();
                }

                // Сохраняем файл
                File.WriteAllLines(recentProjectsFile, lines);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при добавлении проекта в список последних: {ex.Message}");
            }
        }

        /// <summary>
        /// Копирование проекта
        /// </summary>
        public async Task<bool> CopyProjectAsync(string newName, string newDescription = null)
        {
            if (CurrentProject == null)
            {
                _logger.Error("Нет активного проекта для копирования");
                return false;
            }

            try
            {
                // Создаем копию текущего проекта
                var newProject = new ProjectConfiguration
                {
                    Name = newName,
                    Description = newDescription ?? $"Копия проекта {CurrentProject.Name}",
                    Created = DateTime.Now,
                    LastModified = DateTime.Now,
                    ConnectionSettings = new ConnectionSettings
                    {
                        IpAddress = CurrentProject.ConnectionSettings?.IpAddress,
                        Rack = CurrentProject.ConnectionSettings?.Rack ?? 0,
                        Slot = CurrentProject.ConnectionSettings?.Slot ?? 1,
                        ConnectionType = CurrentProject.ConnectionSettings?.ConnectionType ?? ConnectionType.S7
                    },
                    MonitoredTags = CurrentProject.MonitoredTags?.ToList() ?? new List<TagDefinition>()
                };

                // Устанавливаем новый проект как текущий
                CurrentProject = newProject;

                // Очищаем путь к файлу проекта
                LastProjectPath = null;

                // Сохраняем новый проект
                string safeName = string.Join("_", newName.Split(Path.GetInvalidFileNameChars()));
                string newFilePath = Path.Combine(_projectsFolderPath, safeName + _projectFileExtension);

                bool result = await SaveProjectAsync(newFilePath);

                _logger.Info($"Проект скопирован: {newName}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при копировании проекта: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Добавление тега в проект
        /// </summary>
        public void AddTagToProject(TagDefinition tag)
        {
            if (tag == null || CurrentProject == null)
                return;

            // Инициализируем список, если нужно
            if (CurrentProject.MonitoredTags == null)
            {
                CurrentProject.MonitoredTags = new List<TagDefinition>();
            }

            // Проверяем, есть ли уже такой тег
            if (!CurrentProject.MonitoredTags.Any(t => t.Name == tag.Name))
            {
                CurrentProject.MonitoredTags.Add(tag);
                CurrentProject.LastModified = DateTime.Now;
                _logger.Info($"Тег {tag.Name} добавлен в проект");
            }
        }

        /// <summary>
        /// Удаление тега из проекта
        /// </summary>
        public void RemoveTagFromProject(TagDefinition tag)
        {
            if (tag == null || CurrentProject == null || CurrentProject.MonitoredTags == null)
                return;

            // Находим тег по имени
            var existingTag = CurrentProject.MonitoredTags.FirstOrDefault(t => t.Name == tag.Name);

            if (existingTag != null)
            {
                CurrentProject.MonitoredTags.Remove(existingTag);
                CurrentProject.LastModified = DateTime.Now;
                _logger.Info($"Тег {tag.Name} удален из проекта");
            }
        }

        /// <summary>
        /// Обновление настроек подключения
        /// </summary>
        public void UpdateConnectionSettings(ConnectionSettings settings)
        {
            if (settings == null || CurrentProject == null)
                return;

            CurrentProject.ConnectionSettings = settings;
            CurrentProject.LastModified = DateTime.Now;
            _logger.Info("Настройки подключения обновлены");
        }
    }

    /// <summary>
    /// Информация о недавнем проекте
    /// </summary>
    public class RecentProjectInfo
    {
        /// <summary>
        /// Путь к файлу проекта
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Дата последнего открытия
        /// </summary>
        public DateTime LastOpened { get; set; }

        /// <summary>
        /// Имя проекта
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Строковое представление
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({LastOpened:dd.MM.yyyy HH:mm})";
        }
    }
}