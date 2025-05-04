using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SiemensTrend.Core.Models;
using SiemensTrend.Visualization.Charts;
using SkiaSharp;

namespace SiemensTrend.Views
{
    /// <summary>
    /// Логика взаимодействия для ChartView.xaml
    /// </summary>
    public partial class ChartView : UserControl
    {
        private RealTimeChart _chart;
        private DispatcherTimer _durationTimer;
        private DateTime _startTime;
        private bool _isPaused = false;

        /// <summary>
        /// Событие обновления интервала
        /// </summary>
        public event EventHandler<int> TimeRangeChanged;

        /// <summary>
        /// Конструктор
        /// </summary>
        public ChartView()
        {
            InitializeComponent();

            // Создаем и инициализируем график
            InitializeChart();

            // Инициализируем таймер для обновления длительности
            InitializeDurationTimer();

            // Обновляем состояние
            UpdateStatus();
        }

        /// <summary>
        /// Инициализация компонента графика
        /// </summary>
        private void InitializeChart()
        {
            // Создаем экземпляр RealTimeChart
            _chart = new RealTimeChart();

            // Добавляем график на форму
            chartContainer.Children.Clear();
            chartContainer.Children.Add(_chart);

            // Устанавливаем начальный интервал
            int initialInterval = GetSelectedTimeRange();
            _chart.SetVisibleDuration(initialInterval);

            // Инициализируем время начала
            _startTime = DateTime.Now;
            txtStartTime.Text = $"Время начала: {_startTime:HH:mm:ss}";
        }

        /// <summary>
        /// Инициализация таймера для обновления длительности
        /// </summary>
        private void InitializeDurationTimer()
        {
            _durationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _durationTimer.Tick += (s, e) =>
            {
                if (!_isPaused)
                {
                    UpdateDuration();
                }
            };

            _durationTimer.Start();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Запуск"
        /// </summary>
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = false;
            _chart.SetPaused(false);
            UpdateStatus();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Пауза"
        /// </summary>
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = true;
            _chart.SetPaused(true);
            UpdateStatus();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Очистить"
        /// </summary>
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _chart.ClearData();
            _startTime = DateTime.Now;
            txtStartTime.Text = $"Время начала: {_startTime:HH:mm:ss}";
            UpdateStatus();
        }

        /// <summary>
        /// Обработчик изменения временного интервала
        /// </summary>
        private void CmbTimeRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int timeRange = GetSelectedTimeRange();


            if (_chart != null)
            {
                _chart.SetVisibleDuration(timeRange);
            }

            //_chart.SetVisibleDuration(timeRange);

            // Вызываем событие для оповещения родительских компонентов
            TimeRangeChanged?.Invoke(this, timeRange);
        }

        /// <summary>
        /// Получение выбранного временного интервала
        /// </summary>
        private int GetSelectedTimeRange()
        {
            var item = cmbTimeRange.SelectedItem as ComboBoxItem;
            if (item != null && item.Tag != null)
            {
                if (int.TryParse(item.Tag.ToString(), out int seconds))
                {
                    return seconds;
                }
            }

            return 60; // По умолчанию 1 минута
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Сбросить масштаб"
        /// </summary>
        private void BtnResetZoom_Click(object sender, RoutedEventArgs e)
        {
            _chart.ResetZoom();
        }

        /// <summary>
        /// Обработчик переключения автомасштабирования
        /// </summary>
        private void ChkAutoScale_Click(object sender, RoutedEventArgs e)
        {
            bool autoScale = chkAutoScale.IsChecked ?? true;
            _chart.SetAutoScaleY(autoScale);
        }

        /// <summary>
        /// Обновление длительности
        /// </summary>
        private void UpdateDuration()
        {
            TimeSpan duration = DateTime.Now - _startTime;
            txtDuration.Text = $"Длительность: {duration:hh\\:mm\\:ss}";
        }

        /// <summary>
        /// Обновление статуса
        /// </summary>
        private void UpdateStatus()
        {
            txtStatus.Text = $"Статус: {(_isPaused ? "Пауза" : "Активен")}";
        }

        /// <summary>
        /// Добавление тега на график
        /// </summary>
        public void AddTagToChart(TagDefinition tag)
        {
            if (tag == null)
                return;

            _chart.AddTag(tag);
        }

        /// <summary>
        /// Удаление тега с графика
        /// </summary>
        public void RemoveTagFromChart(TagDefinition tag)
        {
            if (tag == null)
                return;

            _chart.RemoveTag(tag);
        }

        /// <summary>
        /// Добавление новых точек данных
        /// </summary>
        public void AddDataPoints(List<TagDataPoint> dataPoints)
        {
            if (dataPoints == null || dataPoints.Count == 0)
                return;

            _chart.AddDataPoints(dataPoints);
        }

        /// <summary>
        /// Добавление горизонтального маркера
        /// </summary>
        public void AddMarker(string name, double value, SKColor color)
        {
            _chart.AddHorizontalLine(name, value, color);
        }

        /// <summary>
        /// Установка диапазона Y
        /// </summary>
        public void SetYAxisRange(double min, double max)
        {
            _chart.SetYRange(min, max);
            chkAutoScale.IsChecked = false;
        }
    }
}