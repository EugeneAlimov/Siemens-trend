using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SiemensTrend.Core.Logging;
using SiemensTrend.Core.Models;

namespace SiemensTrend.ViewModels
{
    /// <summary>
    /// Модель представления для графика
    /// </summary>
    public class ChartViewModel : ViewModelBase
    {
        private readonly Logger _logger;

        private string _title;
        private DateTime _startTime;
        private bool _isPaused;
        private int _visibleDurationSeconds;

        /// <summary>
        /// Заголовок графика
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Время начала записи
        /// </summary>
        public DateTime StartTime
        {
            get => _startTime;
            private set => SetProperty(ref _startTime, value);
        }

        /// <summary>
        /// Приостановлен ли график
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        /// <summary>
        /// Видимая длительность в секундах
        /// </summary>
        public int VisibleDurationSeconds
        {
            get => _visibleDurationSeconds;
            set => SetProperty(ref _visibleDurationSeconds, value);
        }

        /// <summary>
        /// Теги на графике
        /// </summary>
        public ObservableCollection<TagDefinition> Tags { get; }

        /// <summary>
        /// Коллекция серий данных
        /// </summary>
        public ObservableCollection<TagTimeSeries> TimeSeries { get; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public ChartViewModel(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Инициализируем коллекции
            Tags = new ObservableCollection<TagDefinition>();
            TimeSeries = new ObservableCollection<TagTimeSeries>();

            // Устанавливаем начальные значения
            Title = "Новый график";
            StartTime = DateTime.Now;
            IsPaused = false;
            VisibleDurationSeconds = 60; // 1 минута по умолчанию
        }

        /// <summary>
        /// Добавление тега на график
        /// </summary>
        public void AddTag(TagDefinition tag)
        {
            if (tag == null || Tags.Contains(tag))
                return;

            Tags.Add(tag);

            // Создаем новую серию данных для тега
            var series = new TagTimeSeries(tag);
            TimeSeries.Add(series);

            _logger.Info($"Тег {tag.Name} добавлен на график");
        }

        /// <summary>
        /// Удаление тега с графика
        /// </summary>
        public void RemoveTag(TagDefinition tag)
        {
            if (tag == null || !Tags.Contains(tag))
                return;

            Tags.Remove(tag);

            // Находим и удаляем серию данных для тега
            var series = TimeSeries.FirstOrDefault(s => s.Tag == tag);
            if (series != null)
            {
                TimeSeries.Remove(series);
            }

            _logger.Info($"Тег {tag.Name} удален с графика");
        }

        /// <summary>
        /// Добавление новых точек данных
        /// </summary>
        public void AddDataPoints(IEnumerable<TagDataPoint> dataPoints)
        {
            if (IsPaused)
                return;

            foreach (var dataPoint in dataPoints)
            {
                // Находим серию данных для тега
                var series = TimeSeries.FirstOrDefault(s => s.Tag == dataPoint.Tag);
                if (series != null)
                {
                    // Добавляем точку в серию
                    series.AddDataPoint(dataPoint);
                }
            }

            // Обновляем график
            OnPropertyChanged(nameof(TimeSeries));
        }

        /// <summary>
        /// Очистка данных графика
        /// </summary>
        public void ClearData()
        {
            foreach (var series in TimeSeries)
            {
                series.DataPoints.Clear();
            }

            StartTime = DateTime.Now;
            _logger.Info("Данные графика очищены");

            // Обновляем график
            OnPropertyChanged(nameof(TimeSeries));
        }
    }

    /// <summary>
    /// Серия временных данных для тега
    /// </summary>
    public class TagTimeSeries
    {
        /// <summary>
        /// Тег
        /// </summary>
        public TagDefinition Tag { get; }

        /// <summary>
        /// Точки данных
        /// </summary>
        public List<TagDataPoint> DataPoints { get; }

        /// <summary>
        /// Цвет серии
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public TagTimeSeries(TagDefinition tag)
        {
            Tag = tag;
            DataPoints = new List<TagDataPoint>();

            // Генерируем случайный цвет для серии
            Color = GetRandomColor();
        }

        /// <summary>
        /// Добавление точки данных
        /// </summary>
        public void AddDataPoint(TagDataPoint dataPoint)
        {
            if (dataPoint == null || dataPoint.Tag != Tag)
                return;

            DataPoints.Add(dataPoint);
        }

        /// <summary>
        /// Генерация случайного цвета
        /// </summary>
        private string GetRandomColor()
        {
            var random = new Random();
            return $"#{random.Next(0x1000000):X6}";
        }
    }
}