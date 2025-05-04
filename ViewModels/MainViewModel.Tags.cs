using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SiemensTrend.Core.Commands;
using SiemensTrend.Core.Models;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Часть MainViewModel для работы с тегами
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// ViewModel для работы с тегами
        /// </summary>
        public TagsViewModel TagsViewModel { get; }

        public ChartViewModel ChartViewModel { get; }

        // Инициализация в конструкторе MainViewModel:
        //TagsViewModel = new TagsViewModel(_logger, _communicationService, _tagManager);

        /// <summary>
        /// Метод для подписки на события TagsViewModel
        /// </summary>
        private void SubscribeToTagsEvents()
        {
            // Подписываемся на события добавления/удаления тегов из мониторинга
            TagsViewModel.TagAddedToMonitoring += OnTagAddedToMonitoring;
            TagsViewModel.TagRemovedFromMonitoring += OnTagRemovedFromMonitoring;

            // Подписываемся на изменение списка мониторинга для обновления команд
            TagsViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TagsViewModel.MonitoredTags))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            };
        }
        /// <summary>
        /// Обработчик события добавления тега в мониторинг
        /// </summary>
        private void OnTagAddedToMonitoring(object sender, TagViewModel tag)
        {
            if (tag?.Tag != null && ChartViewModel != null) // Теперь ChartViewModel - это свойство, а не тип
            {
                // Добавляем тег на график
                ChartViewModel.AddTag(tag.Tag);
            }
        }
        /// <summary>
        /// Обработчик события удаления тега из мониторинга
        /// </summary>
        private void OnTagRemovedFromMonitoring(object sender, TagViewModel tag)
        {
            if (tag?.Tag != null && ChartViewModel != null) // Теперь ChartViewModel - это свойство, а не тип
            {
                // Удаляем тег с графика
                ChartViewModel.RemoveTag(tag.Tag);
            }
        }

        /// <summary>
        /// Метод для проверки возможности запуска мониторинга
        /// </summary>
        private bool CanStartMonitoring()
        {
            return IsConnected && TagsViewModel?.MonitoredTags?.Count > 0;
        }

        /// <summary>
        /// Метод для запуска мониторинга
        /// </summary>
        private async Task StartMonitoringAsync()
        {
            try
            {
                if (!IsConnected || TagsViewModel?.MonitoredTags?.Count == 0)
                    return;

                StatusMessage = "Запуск мониторинга...";
                ProgressValue = 50;

                // Преобразуем ViewModels в модели
                var tags = TagsViewModel.MonitoredTags
                    .Select(vm => vm.Tag)
                    .ToList();

                // Запускаем мониторинг
                await _communicationService.StartMonitoringAsync(tags);

                // Запускаем график
                ChartViewModel?.Start(); // Теперь ChartViewModel - это свойство, а не тип

                StatusMessage = "Мониторинг запущен";
                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при запуске мониторинга: {ex.Message}");
                StatusMessage = "Ошибка при запуске мониторинга";
                ProgressValue = 0;
            }
        }
    }
}