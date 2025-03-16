using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using SiemensTrend.Core.Models;

namespace SiemensTrend.Visualization.Charts
{
    /// <summary>
    /// Компонент графика реального времени
    /// </summary>
    public class RealTimeChart : UserControl
    {
        private readonly ObservableCollection<ISeries> _series;
        private readonly CartesianChart _chart;
        private readonly Dictionary<string, ObservableCollection<DateTimePoint>> _dataPointsDict;
        private readonly Dictionary<string, ISeries> _seriesDict;

        // Цвета для серий
        private readonly SKColor[] _colors = new[]
        {
            SKColors.Blue,
            SKColors.Red,
            SKColors.Green,
            SKColors.Orange,
            SKColors.Purple,
            SKColors.Teal,
            SKColors.Brown,
            SKColors.DarkCyan
        };

        private int _colorIndex = 0;
        private bool _isPaused = false;
        private int _visibleDurationSeconds = 60;

        /// <summary>
        /// Конструктор
        /// </summary>
        public RealTimeChart()
        {
            _series = new ObservableCollection<ISeries>();
            _dataPointsDict = new Dictionary<string, ObservableCollection<DateTimePoint>>();
            _seriesDict = new Dictionary<string, ISeries>();

            // Инициализация осей
            var xAxis = new Axis
            {
                Name = "Время",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                Labeler = value => new DateTime((long)value).ToString("HH:mm:ss")
            };

            var yAxis = new Axis
            {
                Name = "Значение",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black)
            };

            // Создание графика
            _chart = new CartesianChart
            {
                Series = _series,
                AnimationsSpeed = TimeSpan.FromMilliseconds(300),
                EasingFunction = null,
                TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Top,
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Top,
                ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X
            };

            // Устанавливаем оси после создания для совместимости с разными версиями API
            List<ICartesianAxis> xAxes = new List<ICartesianAxis> { xAxis };
            List<ICartesianAxis> yAxes = new List<ICartesianAxis> { yAxis };

            _chart.XAxes = xAxes;
            _chart.YAxes = yAxes;

            // Установка графика как контента
            Content = _chart;
        }

        /// <summary>
        /// Добавление тега для отображения на графике
        /// </summary>
        public void AddTag(TagDefinition tag)
        {
            if (tag == null || _seriesDict.ContainsKey(tag.Name))
                return;

            // Создаем коллекцию точек
            var points = new ObservableCollection<DateTimePoint>();
            _dataPointsDict[tag.Name] = points;

            // Выбираем цвет
            var color = _colors[_colorIndex % _colors.Length];
            _colorIndex++;

            // Создаем серию
            var series = new LineSeries<DateTimePoint>
            {
                Values = points,
                Name = tag.Name,
                Fill = null,
                GeometryFill = new SolidColorPaint(SKColors.White),
                GeometryStroke = new SolidColorPaint(color, 2),
                Stroke = new SolidColorPaint(color, 2),
                GeometrySize = 5,
                LineSmoothness = 0
            };

            // Сохраняем серию
            _seriesDict[tag.Name] = series;
            _series.Add(series);
        }

        /// <summary>
        /// Удаление тега с графика
        /// </summary>
        public void RemoveTag(TagDefinition tag)
        {
            if (tag == null || !_seriesDict.ContainsKey(tag.Name))
                return;

            // Удаляем серию
            _series.Remove(_seriesDict[tag.Name]);
            _seriesDict.Remove(tag.Name);
            _dataPointsDict.Remove(tag.Name);
        }

        /// <summary>
        /// Добавление новых точек данных
        /// </summary>
        public void AddDataPoints(List<TagDataPoint> dataPoints)
        {
            if (_isPaused || dataPoints == null || dataPoints.Count == 0)
                return;

            // Группируем точки по тегам
            var groupedPoints = dataPoints
                .Where(p => p.Tag != null && p.NumericValue.HasValue)
                .GroupBy(p => p.Tag.Name);

            // Обрабатываем каждую группу
            foreach (var group in groupedPoints)
            {
                string tagName = group.Key;

                // Если серия существует
                if (_dataPointsDict.TryGetValue(tagName, out var points))
                {
                    // Добавляем новые точки
                    foreach (var dp in group)
                    {
                        if (dp.NumericValue.HasValue)
                        {
                            points.Add(new DateTimePoint(dp.Timestamp, dp.NumericValue.Value));
                        }
                    }

                    // Очищаем старые точки
                    CleanupOldPoints(points);

                    // Обновляем видимый диапазон
                    UpdateVisibleRange();
                }
            }
        }

        /// <summary>
        /// Очистка старых точек
        /// </summary>
        private void CleanupOldPoints(ObservableCollection<DateTimePoint> points)
        {
            if (points.Count == 0)
                return;

            // Граница для удаления старых точек
            var cutoffTime = DateTime.Now.AddSeconds(-_visibleDurationSeconds * 3);

            // Удаляем старые точки
            while (points.Count > 0 && points[0].DateTime < cutoffTime)
            {
                points.RemoveAt(0);
            }
        }

        /// <summary>
        /// Обновление видимого диапазона
        /// </summary>
        private void UpdateVisibleRange()
        {
            if (_isPaused)
                return;

            var now = DateTime.Now;
            var minTime = now.AddSeconds(-_visibleDurationSeconds);

            // Обновляем ось X
            if (_chart.XAxes != null && _chart.XAxes.Any())
            {
                var xAxis = _chart.XAxes.FirstOrDefault() as Axis;
                if (xAxis != null)
                {
                    xAxis.MinLimit = minTime.Ticks;
                    xAxis.MaxLimit = now.Ticks;
                }
            }
        }

        /// <summary>
        /// Установка видимого диапазона времени
        /// </summary>
        public void SetVisibleDuration(int seconds)
        {
            if (seconds < 1)
                seconds = 1;

            _visibleDurationSeconds = seconds;
            UpdateVisibleRange();
        }

        /// <summary>
        /// Установка паузы в обновлении графика
        /// </summary>
        public void SetPaused(bool paused)
        {
            _isPaused = paused;

            if (!_isPaused)
                UpdateVisibleRange();
        }

        /// <summary>
        /// Очистка данных графика
        /// </summary>
        public void ClearData()
        {
            foreach (var points in _dataPointsDict.Values)
            {
                points.Clear();
            }

            UpdateVisibleRange();
        }

        /// <summary>
        /// Добавление горизонтального маркера
        /// </summary>
        public void AddHorizontalLine(string name, double value, SKColor? color = null)
        {
            var lineColor = color ?? SKColors.DarkGray;

            // Создаем горизонтальную линию как ось
            var line = new Axis
            {
                Name = name,
                NamePaint = new SolidColorPaint(lineColor),
                IsVisible = true,
                Position = LiveChartsCore.Measure.AxisPosition.Start,
                ShowSeparatorLines = true,
                MinLimit = value,
                MaxLimit = value,
                ForceStepToMin = true,
                SeparatorsPaint = new SolidColorPaint
                {
                    Color = lineColor,
                    StrokeThickness = 1
                }
            };

            // Добавляем ось к графику
            List<ICartesianAxis> yAxes = new List<ICartesianAxis>();

            // Сначала добавляем существующие оси
            if (_chart.YAxes != null)
            {
                foreach (var existingAxis in _chart.YAxes)
                {
                    yAxes.Add(existingAxis);
                }
            }

            // Добавляем новую ось-линию
            yAxes.Add(line);

            // Устанавливаем обновленный список осей
            _chart.YAxes = yAxes;
        }

        /// <summary>
        /// Установка автоматического масштабирования по оси Y
        /// </summary>
        public void SetAutoScaleY(bool autoScale)
        {
            if (_chart.YAxes != null && _chart.YAxes.Any())
            {
                var yAxis = _chart.YAxes.FirstOrDefault() as Axis;
                if (yAxis != null && autoScale)
                {
                    yAxis.MinLimit = null;
                    yAxis.MaxLimit = null;
                }
            }
        }

        /// <summary>
        /// Установка фиксированного диапазона по оси Y
        /// </summary>
        public void SetYRange(double min, double max)
        {
            if (_chart.YAxes != null && _chart.YAxes.Any())
            {
                var yAxis = _chart.YAxes.FirstOrDefault() as Axis;
                if (yAxis != null)
                {
                    yAxis.MinLimit = min;
                    yAxis.MaxLimit = max;
                }
            }
        }

        /// <summary>
        /// Сброс масштаба
        /// </summary>
        public void ResetZoom()
        {
            SetAutoScaleY(true);
            UpdateVisibleRange();
        }
    }
}