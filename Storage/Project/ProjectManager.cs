using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SiemensTrend.Core.Logging;

namespace SiemensTrend.Storage.Project
{
    /// <summary>
    /// Класс для управления проектами
    /// </summary>
    public class ProjectManager
    {
        private readonly Logger _logger;
        private ProjectConfiguration _currentProject;

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
        /// Конструктор
        /// </summary>
        public ProjectManager(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                LastModified = DateTime.Now
            };

            _logger.Info($"Создан новый проект: {name}");
        }

        /// <summary>
        /// Сохранение проекта в файл
        /// </summary>
        public async Task<bool> SaveProjectAsync(string filePath)
        {
            if (CurrentProject == null)
            {
                _logger.Error("Нет активного проекта для сохранения");
                return false;
            }

            try
            {
                // Обновляем дату последнего изменения
                CurrentProject.LastModified = DateTime.Now;

                // Сериализуем проект в XML
                var serializer = new XmlSerializer(typeof(ProjectConfiguration));

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    // Сериализуем проект
                    await Task.Run(() => serializer.Serialize(stream, CurrentProject));
                }

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
                }

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
        public List<string> GetRecentProjects(string projectsFolder = null)
        {
            try
            {
                // Если папка не указана, используем папку "Projects" в каталоге приложения
                if (string.IsNullOrEmpty(projectsFolder))
                {
                    projectsFolder = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "Projects");
                }

                // Создаем директорию, если она не существует
                if (!Directory.Exists(projectsFolder))
                {
                    Directory.CreateDirectory(projectsFolder);
                    return new List<string>();
                }

                // Получаем список файлов проектов (.spt)
                var projectFiles = Directory.GetFiles(projectsFolder, "*.spt");

                // Сортируем по дате изменения (сначала новые)
                Array.Sort(projectFiles, (a, b) =>
                    File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

                return new List<string>(projectFiles);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при получении списка проектов: {ex.Message}");
                return new List<string>();
            }
        }
    }
}