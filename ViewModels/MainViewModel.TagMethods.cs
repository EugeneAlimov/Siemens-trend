using System;
using System.Collections.Generic;
using System.Linq;
using SiemensTrend.Core.Models;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Часть MainViewModel для работы с тегами
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// Добавляет новый тег
        /// </summary>
        public void AddNewTag(TagDefinition tag)
        {
            if (tag == null)
                return;

            try
            {
                _logger.Info($"AddNewTag: Добавление нового тега: {tag.Name}");

                // Проверяем, существует ли уже тег с таким именем
                if (TagExists(tag.Name))
                {
                    _logger.Warn($"AddNewTag: Тег с именем {tag.Name} уже существует");
                    StatusMessage = $"Тег с именем {tag.Name} уже существует";
                    return;
                }

                // Добавляем тег в соответствующие коллекции
                AvailableTags.Add(tag);

                // Используем явно установленное свойство IsDbTag, а не вычисленное
                if (tag.IsDbTag)
                {
                    DbTags.Add(tag);
                    _logger.Info($"AddNewTag: Добавлен DB тег: {tag.Name}");
                }
                else
                {
                    PlcTags.Add(tag);
                    _logger.Info($"AddNewTag: Добавлен PLC тег: {tag.Name}");
                }

                // Обновляем объединенный список тегов
                UpdateAllTags();

                // Сохраняем изменения
                SaveTagsToStorage();

                StatusMessage = $"Тег {tag.Name} добавлен";
            }
            catch (Exception ex)
            {
                _logger.Error($"AddNewTag: Ошибка при добавлении тега: {ex.Message}");
                StatusMessage = "Ошибка при добавлении тега";
            }
        }

        /// <summary>
        /// Редактирует тег
        /// </summary>
        public void EditTag(TagDefinition originalTag, TagDefinition updatedTag)
        {
            if (originalTag == null || updatedTag == null)
                return;

            try
            {
                _logger.Info($"EditTag: Редактирование тега: {originalTag.Name} -> {updatedTag.Name}");

                // Проверяем, существует ли тег с новым именем, если оно отличается
                if (!originalTag.Name.Equals(updatedTag.Name, StringComparison.OrdinalIgnoreCase) &&
                    TagExists(updatedTag.Name))
                {
                    _logger.Warn($"EditTag: Тег с именем {updatedTag.Name} уже существует");
                    StatusMessage = $"Тег с именем {updatedTag.Name} уже существует";
                    return;
                }

                // Удаляем старый тег
                RemoveTag(originalTag);

                // Добавляем обновленный тег
                AddNewTag(updatedTag);

                _logger.Info($"EditTag: Тег отредактирован успешно");
                StatusMessage = $"Тег {updatedTag.Name} отредактирован";
            }
            catch (Exception ex)
            {
                _logger.Error($"EditTag: Ошибка при редактировании тега: {ex.Message}");
                StatusMessage = "Ошибка при редактировании тега";
            }
        }

        /// <summary>
        /// Удаляет тег
        /// </summary>
        public void RemoveTag(TagDefinition tag)
        {
            if (tag == null)
                return;

            try
            {
                _logger.Info($"RemoveTag: Удаление тега: {tag.Name}");

                // Удаляем тег из всех коллекций
                AvailableTags.Remove(tag);

                if (tag.IsDbTag)
                {
                    DbTags.Remove(tag);
                }
                else
                {
                    PlcTags.Remove(tag);
                }

                // Удаляем из мониторинга, если присутствует
                if (MonitoredTags.Contains(tag))
                {
                    RemoveTagFromMonitoring(tag);
                }

                // Обновляем объединенный список тегов
                UpdateAllTags();

                // Сохраняем изменения
                SaveTagsToStorage();

                _logger.Info($"RemoveTag: Тег {tag.Name} удален");
                StatusMessage = $"Тег {tag.Name} удален";
            }
            catch (Exception ex)
            {
                _logger.Error($"RemoveTag: Ошибка при удалении тега: {ex.Message}");
                StatusMessage = "Ошибка при удалении тега";
            }
        }

        /// <summary>
        /// Проверяет, существует ли тег с указанным именем
        /// </summary>
        private bool TagExists(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return false;

            // Проверяем в PLC тегах
            if (PlcTags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                return true;

            // Проверяем в DB тегах
            if (DbTags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        /// <summary>
        /// Поиск тега по ID
        /// </summary>
        public TagDefinition FindTagById(Guid id)
        {
            // Ищем в объединенном списке
            return AllTags.FirstOrDefault(t => t.Id == id);
        }

        /// <summary>
        /// Поиск тега по имени
        /// </summary>
        public TagDefinition FindTagByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // Ищем в объединенном списке
            return AllTags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Добавляет теги в список мониторинга
        /// </summary>
        public void AddTagsToMonitoring(IEnumerable<TagDefinition> tags)
        {
            if (tags == null)
                return;

            foreach (var tag in tags)
            {
                AddTagToMonitoring(tag);
            }
        }

        /// <summary>
        /// Добавляет тег в список мониторинга
        /// </summary>
        private void AddTagToMonitoring(TagDefinition tag)
        {
            if (tag == null)
                return;

            try
            {
                // Проверяем лимит тегов для мониторинга
                if (MonitoredTags.Count >= MaxMonitoredTags)
                {
                    _logger.Warn($"AddTagToMonitoring: Достигнут лимит тегов для мониторинга ({MaxMonitoredTags})");
                    StatusMessage = $"Достигнут лимит тегов для мониторинга ({MaxMonitoredTags})";
                    return;
                }

                // Проверяем, не добавлен ли тег уже
                if (MonitoredTags.Any(t => t.Id == tag.Id))
                {
                    _logger.Warn($"AddTagToMonitoring: Тег {tag.Name} уже добавлен в мониторинг");
                    StatusMessage = $"Тег {tag.Name} уже добавлен в мониторинг";
                    return;
                }

                // Добавляем тег в мониторинг
                MonitoredTags.Add(tag);
                _logger.Info($"AddTagToMonitoring: Тег {tag.Name} добавлен в мониторинг");
                StatusMessage = $"Тег {tag.Name} добавлен в мониторинг";

                // Вызываем событие для обновления графика
                TagAddedToMonitoring?.Invoke(this, tag);
            }
            catch (Exception ex)
            {
                _logger.Error($"AddTagToMonitoring: Ошибка при добавлении тега в мониторинг: {ex.Message}");
                StatusMessage = "Ошибка при добавлении тега в мониторинг";
            }
        }

        /// <summary>
        /// Удаляет тег из списка мониторинга
        /// </summary>
        private void RemoveTagFromMonitoring(TagDefinition tag)
        {
            if (tag == null)
                return;

            try
            {
                if (MonitoredTags.Remove(tag))
                {
                    _logger.Info($"RemoveTagFromMonitoring: Тег {tag.Name} удален из мониторинга");
                    StatusMessage = $"Тег {tag.Name} удален из мониторинга";

                    // Вызываем событие для обновления графика
                    TagRemovedFromMonitoring?.Invoke(this, tag);
                }
                else
                {
                    _logger.Warn($"RemoveTagFromMonitoring: Тег {tag.Name} не найден в списке мониторинга");
                    StatusMessage = $"Тег {tag.Name} не найден в списке мониторинга";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"RemoveTagFromMonitoring: Ошибка при удалении тега из мониторинга: {ex.Message}");
                StatusMessage = "Ошибка при удалении тега из мониторинга";
            }
        }
    }
}