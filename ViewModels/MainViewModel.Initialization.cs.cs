using System;
using System.Collections.ObjectModel;
using System.Windows;
using SiemensTrend.Core.Models;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Часть класса MainViewModel для инициализации
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// Обновление объединенного списка тегов
        /// </summary>
        private void UpdateAllTags()
        {
            try
            {
                if (AllTags == null)
                {
                    AllTags = new ObservableCollection<TagDefinition>();
                }

                AllTags.Clear();

                // Добавляем PLC теги
                foreach (var tag in PlcTags)
                {
                    AllTags.Add(tag);
                }

                // Добавляем DB теги
                foreach (var tag in DbTags)
                {
                    AllTags.Add(tag);
                }

                // Уведомляем представление об изменении
                OnPropertyChanged(nameof(AllTags));
            }
            catch (Exception ex)
            {
                _logger.Error($"UpdateAllTags: Ошибка при обновлении списка всех тегов: {ex.Message}");
            }
        }

        /// <summary>
        /// Инициализация приложения
        /// </summary>
        public void Initialize()
        {
            try
            {
                _logger.Info("Initialize: Инициализация приложения");

                // Инициализация коллекций
                InitializeTags();

                // Загружаем теги из хранилища
                LoadTagsFromStorage();

                _logger.Info("Initialize: Инициализация завершена");
            }
            catch (Exception ex)
            {
                _logger.Error($"Initialize: Ошибка при инициализации: {ex.Message}");
            }
        }

        /// <summary>
        /// Инициализация метода в конструкторе
        /// </summary>
        private void InitializeTags()
        {
            // Инициализируем коллекции
            AvailableTags = new ObservableCollection<TagDefinition>();
            MonitoredTags = new ObservableCollection<TagDefinition>();
            PlcTags = new ObservableCollection<TagDefinition>();
            DbTags = new ObservableCollection<TagDefinition>();
            AllTags = new ObservableCollection<TagDefinition>();
        }

        /// <summary>
        /// Загружает теги из хранилища
        /// </summary>
        private void LoadTagsFromStorage()
        {
            try
            {
                _logger.Info("LoadTagsFromStorage: Загрузка тегов из хранилища");

                var tags = _tagManager.LoadTags();

                // Очищаем текущие коллекции
                PlcTags.Clear();
                DbTags.Clear();
                AvailableTags.Clear();

                // Распределяем теги по коллекциям
                foreach (var tag in tags)
                {
                    AvailableTags.Add(tag);

                    if (tag.IsDbTag)
                    {
                        DbTags.Add(tag);
                    }
                    else
                    {
                        PlcTags.Add(tag);
                    }
                }

                // Обновляем объединенный список тегов
                UpdateAllTags();

                _logger.Info($"LoadTagsFromStorage: Загружено {PlcTags.Count} тегов PLC и {DbTags.Count} тегов DB");
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadTagsFromStorage: Ошибка загрузки тегов: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохраняет теги в хранилище
        /// </summary>
        public void SaveTagsToStorage()
        {
            try
            {
                _logger.Info("SaveTagsToStorage: Сохранение тегов в хранилище");

                var allTags = new System.Collections.Generic.List<TagDefinition>();

                // Собираем все теги из объединенной коллекции
                foreach (var tag in AllTags)
                {
                    allTags.Add(tag);
                }

                // Сохраняем теги
                _tagManager.SaveTags(allTags);

                _logger.Info($"SaveTagsToStorage: Сохранено {allTags.Count} тегов");
                StatusMessage = "Теги сохранены";
            }
            catch (Exception ex)
            {
                _logger.Error($"SaveTagsToStorage: Ошибка сохранения тегов: {ex.Message}");
                StatusMessage = "Ошибка сохранения тегов";
            }
        }
    }
}