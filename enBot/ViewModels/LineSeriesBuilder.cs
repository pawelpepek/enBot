using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.Generic;

namespace enBot.ViewModels;

public class LineSeriesBuilder(string Name)
{
    private string _strokeColor = "#4CAF50";
    private string _geometryFillColor = "#4CAF50";
    private List<double> _values = [];
    private bool _showsAtY;

    public LineSeriesBuilder SetValues(List<double> values)
    {
        _values = values;

        return this;
    }

    public LineSeriesBuilder SetColors(string stroke, string geometryFill)
    {
        _strokeColor = stroke;
        _geometryFillColor = geometryFill;

        return this;
    }

    public LineSeriesBuilder SetShowsYAt(bool showsAtY)
    {
        _showsAtY = showsAtY;

        return this;
    }

    public LineSeries<double> Build()
    {
        return new LineSeries<double>
        {
            Name = Name,
            Values = _values,
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse(_strokeColor)) { StrokeThickness = 2 },
            GeometrySize = 6,
            GeometryFill = new SolidColorPaint(SKColor.Parse(_geometryFillColor)),
            GeometryStroke = null,
            ScalesYAt = _showsAtY ? 1 : 0
        };
    }
}
