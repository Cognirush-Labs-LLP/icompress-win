using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.UI;

namespace miCompressor.ui;

/// <summary>
/// A custom WinUI 3 Pie Chart Control.
/// </summary>
public sealed class PieChart : Control
{
    private Canvas _canvas;
    private StackPanel _legendPanel;

    public PieChart()
    {
        this.DefaultStyleKey = typeof(PieChart);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _canvas = GetTemplateChild("PART_Canvas") as Canvas;
        _legendPanel = GetTemplateChild("PART_LegendPanel") as StackPanel;
        DrawPieChart();
    }

    #region Dependency Properties

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(List<PieChartData>), typeof(PieChart),
        new PropertyMetadata(null, OnDataChanged));

    public List<PieChartData> Data
    {
        get => (List<PieChartData>)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PieChart)d;
        control.DrawPieChart();
    }
    #endregion

    private void DrawPieChart()
    {
        if (_canvas == null || _legendPanel == null || Data == null || Data.Count == 0)
            return;

        _canvas.Children.Clear();
        _legendPanel.Children.Clear();

        double total = Data.Sum(d => d.Value);
        if (total == 0) return; // Avoid rendering empty charts

        double startAngle = 0;
        double centerX = 70;
        double centerY = 70;
        double radius = 70;
        var nonZeroEntries = Data.Where(d => d.Value > 0).ToList();

        if (nonZeroEntries.Count == 1)
        {
            var entry = nonZeroEntries.First();
            var fullCircleSlice = CreatePieSlice(centerX, centerY, radius, 0, 360, entry.Color, 1);
            _canvas.Children.Add(fullCircleSlice);
            AddLegend(entry);
            return;
        }

        int entryIndex = 1;
        foreach (var entry in nonZeroEntries)
        {
            double sweepAngle = (entry.Value / total) * 360;
            var pieSlice = CreatePieSlice(centerX, centerY, radius, startAngle, sweepAngle, entry.Color, entryIndex);
            _canvas.Children.Add(pieSlice);
            startAngle += sweepAngle;
            AddLegend(entry);
        }
    }

    private Path CreatePieSlice(double centerX, double centerY, double radius, double startAngle, double sweepAngle, Color color, int entryIndex)
    {
        double startRadians = (Math.PI / 180) * startAngle;
        double endRadians = (Math.PI / 180) * (startAngle + sweepAngle);

        double x1 = centerX + radius * Math.Cos(startRadians);
        double y1 = centerY + radius * Math.Sin(startRadians);
        double x2 = centerX + radius * Math.Cos(endRadians);
        double y2 = centerY + radius * Math.Sin(endRadians);

        bool isLargeArc = sweepAngle > 180;

        PathFigure figure = new PathFigure
        {
            StartPoint = new Windows.Foundation.Point(centerX, centerY),
            Segments = new PathSegmentCollection
                {
                    new LineSegment { Point = new Windows.Foundation.Point(x1, y1) },
                    new ArcSegment
                    {
                        Point = new Windows.Foundation.Point(x2, y2),
                        Size = new Windows.Foundation.Size(radius, radius),
                        SweepDirection = SweepDirection.Clockwise,
                        IsLargeArc = isLargeArc
                    },
                    new LineSegment { Point = new Windows.Foundation.Point(centerX, centerY) }
                }
        };

        PathGeometry geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        Path path = new Path
        {
            Fill = new SolidColorBrush(color),
            Data = geometry,
            Shadow = new ThemeShadow()
        };

        path.Translation += new Vector3(0, 0, entryIndex*10);

        return path;
    }

    private void AddLegend(PieChartData entry)
    {
        StackPanel legendItem = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

        Rectangle colorBox = new Rectangle
        {
            Width = 16,
            Height = 16,
            Fill = new SolidColorBrush(entry.Color),
            Margin = new Thickness(5, 0, 5, 0)
        };

        TextBlock label = new TextBlock
        {
            Text = entry.Label,
            VerticalAlignment = VerticalAlignment.Center
        };

        legendItem.Children.Add(colorBox);
        legendItem.Children.Add(label);
        _legendPanel.Children.Add(legendItem);
    }
}

/// <summary>
/// Represents a data entry for the PieChart with a value and color.
/// </summary>
public class PieChartData
{
    public string Label { get; set; }
    public double Value { get; set; }
    public Color Color { get; set; }
}
