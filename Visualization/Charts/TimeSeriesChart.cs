using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Visualization.Charts
{
    /// <summary>
    /// Пользовательский элемент управления для отображения временного ряда
    /// </summary>
    public class TimeSeriesChart : Control
    {
        // Статические конструкторы необходимы для регистрации свойств зависимости
        static TimeSeriesChart()
        {
            // Регистрируем стиль по умолчанию
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TimeSeriesChart),
                new FrameworkPropertyMetadata(typeof(TimeSeriesChart)));
        }

        /// <summary>
        /// Свойство зависимости для заголовка
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title",
                typeof(string),
                typeof(TimeSeriesChart),
                new PropertyMetadata(""));

        /// <summary>
        /// Свойство зависимости для данных временного ряда
        /// </summary>
        public static readonly DependencyProperty TimeSeriesProperty =
            DependencyProperty.Register(
                "TimeSeries",
                typeof(IEnumerable<TagDataPoint>),
                typeof(TimeSeriesChart),
                new PropertyMetadata(null, OnTimeSeriesChanged));

        /// <summary>
        /// Свойство зависимости для видимой длительности в секундах
        /// </summary>
        public static readonly DependencyProperty VisibleDurationSecondsProperty =
            DependencyProperty.Register(
                "VisibleDurationSeconds",
                typeof(int),
                typeof(TimeSeriesChart),
                new PropertyMetadata(60));

        /// <summary>
        /// Заголовок графика
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Данные временного ряда
        /// </summary>
        public IEnumerable<TagDataPoint> TimeSeries
        {
            get => (IEnumerable<TagDataPoint>)GetValue(TimeSeriesProperty);
            set => SetValue(TimeSeriesProperty, value);
        }

        /// <summary>
        /// Видимая длительность в секундах
        /// </summary>
        public int VisibleDurationSeconds
        {
            get => (int)GetValue(VisibleDurationSecondsProperty);
            set => SetValue(VisibleDurationSecondsProperty, value);
        }

        /// <summary>
        /// Обработчик изменения данных временного ряда
        /// </summary>
        private static void OnTimeSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (TimeSeriesChart)d;
            chart.InvalidateVisual(); // Перерисовываем график
        }

        /// <summary>
        /// Событие обновления графика
        /// </summary>
        public event EventHandler ChartUpdated;

        /// <summary>
        /// Метод перерисовки графика
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // В реальном приложении здесь будет код отрисовки графика
            // Для демонстрации мы используем простую заглушку

            // Вызываем событие обновления графика
            ChartUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}