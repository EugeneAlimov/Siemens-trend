using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SiemensTrend.Helpers
{
    /// <summary>
    /// Методы TiaPortalXmlManager для управления кэшем проектов
    /// </summary>
    public partial class TiaPortalXmlManager
    {
        /// <summary>
        /// Проверка наличия экспортированных данных для указанного проекта
        /// </summary>
        /// <param name="projectName">Имя проекта</param>
        /// <returns>True, если данные найдены</returns>
        public bool HasExportedDataForProject(string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                _logger.Warn("HasExportedDataForProject: Имя проекта не может быть пустым");
                return false;
            }

            try
            {
                // Заменяем недопустимые символы в имени файла
                string safeName = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars()));

                // Проверяем существование папки проекта и наличие в ней файлов
                string projectPath = Path.Combine(_baseExportPath, safeName);

                if (!Directory.Exists(projectPath))
                {
                    _logger.Info($"HasExportedDataForProject: Директория проекта не найдена: {projectPath}");
                    return false;
                }

                string tagsPath = Path.Combine(projectPath, "TagTables");
                string dbPath = Path.Combine(projectPath, "DB");

                bool hasTags = Directory.Exists(tagsPath) &&
                              (Directory.GetFiles(tagsPath, "*.xml", SearchOption.AllDirectories).Length > 0);

                bool hasDBs = Directory.Exists(dbPath) &&
                             (Directory.GetFiles(dbPath, "*.xml", SearchOption.TopDirectoryOnly).Length > 0);

                _logger.Info($"HasExportedDataForProject: Проект {projectName} - наличие тегов: {hasTags}, наличие DB: {hasDBs}");
                return hasTags || hasDBs;
            }
            catch (Exception ex)
            {
                _logger.Error($"HasExportedDataForProject: Ошибка: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Удаление файлов кэша проекта
        /// </summary>
        public bool ClearProjectCache(string projectName = null)
        {
            try
            {
                // Если имя проекта не указано, используем текущий проект
                string targetProject = projectName ?? _currentProjectName;

                if (string.IsNullOrEmpty(targetProject))
                {
                    _logger.Error("ClearProjectCache: Не указано имя проекта для очистки кэша");
                    return false;
                }

                // Заменяем недопустимые символы в имени файла
                string safeName = string.Join("_", targetProject.Split(Path.GetInvalidFileNameChars()));
                string projectPath = Path.Combine(_baseExportPath, safeName);

                if (!Directory.Exists(projectPath))
                {
                    _logger.Warn($"ClearProjectCache: Директория кэша для проекта {targetProject} не найдена: {projectPath}");
                    return false;
                }

                // Отменяем все активные операции экспорта перед удалением
                CancelAllExportOperations();

                // Удаляем папку проекта со всем содержимым
                DirectoryInfo dir = new DirectoryInfo(projectPath);

                // Очищаем атрибуты ReadOnly для всех файлов и папок
                foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes &= ~FileAttributes.ReadOnly;
                }

                foreach (var subDir in dir.GetDirectories("*", SearchOption.AllDirectories))
                {
                    subDir.Attributes &= ~FileAttributes.ReadOnly;
                }

                Directory.Delete(projectPath, true);
                _logger.Info($"ClearProjectCache: Кэш проекта {targetProject} успешно удален");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"ClearProjectCache: Ошибка при очистке кэша: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получение списка проектов в кэше
        /// </summary>
        public List<string> GetCachedProjects()
        {
            var projects = new List<string>();

            try
            {
                if (!Directory.Exists(_baseExportPath))
                {
                    _logger.Info($"GetCachedProjects: Базовая директория кэша не существует: {_baseExportPath}");
                    return projects;
                }

                // Получаем все папки в директории кэша
                var directories = Directory.GetDirectories(_baseExportPath);

                foreach (var dir in directories)
                {
                    try
                    {
                        // Проверяем, содержит ли директория данные (подпапки TagTables или DB)
                        bool hasTagTables = Directory.Exists(Path.Combine(dir, "TagTables")) &&
                            Directory.GetFiles(Path.Combine(dir, "TagTables"), "*.xml", SearchOption.AllDirectories).Length > 0;

                        bool hasDBs = Directory.Exists(Path.Combine(dir, "DB")) &&
                            Directory.GetFiles(Path.Combine(dir, "DB"), "*.xml", SearchOption.TopDirectoryOnly).Length > 0;

                        if (hasTagTables || hasDBs)
                        {
                            // Добавляем имя папки (проекта) в список
                            projects.Add(Path.GetFileName(dir));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"GetCachedProjects: Ошибка при проверке директории {dir}: {ex.Message}");
                    }
                }

                _logger.Info($"GetCachedProjects: Найдено {projects.Count} проектов в кэше");
            }
            catch (Exception ex)
            {
                _logger.Error($"GetCachedProjects: Ошибка: {ex.Message}");
            }

            return projects;
        }

        /// <summary>
        /// Получение информации о состоянии кэша
        /// </summary>
        public Dictionary<string, object> GetCacheStatus(string projectName = null)
        {
            var status = new Dictionary<string, object>();

            try
            {
                // Если имя проекта не указано, используем текущий проект
                string targetProject = projectName ?? _currentProjectName;

                if (string.IsNullOrEmpty(targetProject))
                {
                    status["error"] = "Не указано имя проекта";
                    return status;
                }

                // Заменяем недопустимые символы в имени файла
                string safeName = string.Join("_", targetProject.Split(Path.GetInvalidFileNameChars()));
                string projectPath = Path.Combine(_baseExportPath, safeName);

                if (!Directory.Exists(projectPath))
                {
                    status["exists"] = false;
                    return status;
                }

                status["exists"] = true;

                // Проверяем наличие кэша тегов и блоков данных
                string tagsPath = Path.Combine(projectPath, "TagTables");
                string dbPath = Path.Combine(projectPath, "DB");

                bool hasTags = Directory.Exists(tagsPath);
                bool hasDBs = Directory.Exists(dbPath);

                status["hasTags"] = hasTags;
                status["hasDBs"] = hasDBs;

                // Если есть кэш, получаем дополнительную информацию
                if (hasTags)
                {
                    var tagFiles = Directory.GetFiles(tagsPath, "*.xml", SearchOption.AllDirectories);
                    status["tagCount"] = tagFiles.Length;

                    if (tagFiles.Length > 0)
                    {
                        var lastWriteTime = File.GetLastWriteTime(tagFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First());
                        status["tagsLastUpdated"] = lastWriteTime;
                    }
                }

                if (hasDBs)
                {
                    var dbFiles = Directory.GetFiles(dbPath, "*.xml", SearchOption.TopDirectoryOnly);
                    status["dbCount"] = dbFiles.Length;

                    if (dbFiles.Length > 0)
                    {
                        var lastWriteTime = File.GetLastWriteTime(dbFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First());
                        status["dbsLastUpdated"] = lastWriteTime;
                    }
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.Error($"GetCacheStatus: Ошибка: {ex.Message}");
                status["error"] = ex.Message;
                return status;
            }
        }
    }
}