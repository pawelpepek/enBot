using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using enBot.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace enBot.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly PromptStorageService _storageService;

    [ObservableProperty] private int _totalPrompts;
    [ObservableProperty] private double _avgWeightedScore;
    [ObservableProperty] private double _avgWeightedComplexity;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _showPromptCounts = false;

    [ObservableProperty] private ISeries[] _chartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _chartXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _chartYAxes = Array.Empty<Axis>();

    public DashboardViewModel(PromptStorageService storageService)
    {
        _storageService = storageService;
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private Task Refresh() => LoadDataAsync();

    partial void OnShowPromptCountsChanged(bool value) => _ = LoadDataAsync();

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            TotalPrompts = await _storageService.GetTotalPromptsAsync().ConfigureAwait(false);
            AvgWeightedScore = Math.Round(await _storageService.GetAverageWeightedScoreAsync().ConfigureAwait(false), 1);
            AvgWeightedComplexity = Math.Round(await _storageService.GetAverageWeightedComplexityAsync().ConfigureAwait(false), 1);

            await BuildChartAsync().ConfigureAwait(false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BuildChartAsync()
    {
        var stats = await _storageService.GetAllDailyStatsAsync().ConfigureAwait(false);

        var counts = new List<double>();
        var scores = new List<double>();
        var complexities = new List<double>();
        var labels = new List<string>();

        foreach (var s in stats)
        {
            counts.Add(s.Count);
            scores.Add(Math.Round(s.AvgScore, 1));
            complexities.Add(Math.Round(s.AvgComplexity, 1));
            labels.Add(s.Date.ToString("MM/dd", CultureInfo.InvariantCulture));
        }

        var series = new List<ISeries>();

        if (ShowPromptCounts)
            series.Add(new ColumnSeries<double>
            {
                Name = "Prompts",
                Values = counts,
                Fill = new SolidColorPaint(SKColor.Parse("#5C6BC0"))
            });

        series.Add(new LineSeries<double>
        {
            Name = "Avg Score",
            Values = scores,
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse("#4CAF50")) { StrokeThickness = 2 },
            GeometrySize = 6,
            GeometryFill = new SolidColorPaint(SKColor.Parse("#4CAF50")),
            GeometryStroke = null,
            ScalesYAt = ShowPromptCounts ? 1 : 0
        });

        series.Add(new LineSeries<double>
        {
            Name = "Avg Complexity",
            Values = complexities,
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse("#FF9800")) { StrokeThickness = 2 },
            GeometrySize = 6,
            GeometryFill = new SolidColorPaint(SKColor.Parse("#FF9800")),
            GeometryStroke = null,
            ScalesYAt = ShowPromptCounts ? 1 : 0
        });

        ChartSeries = series.ToArray();

        ChartXAxes = new[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = labels.Count > 10 ? -45 : 0
            }
        };

        ChartYAxes = ShowPromptCounts
            ? new[]
              {
                  new Axis { Name = "Prompts", MinLimit = 0 },
                  new Axis { Name = "Score / Complexity", MinLimit = 0, MaxLimit = 10 }
              }
            : new[]
              {
                  new Axis { Name = "Score / Complexity", MinLimit = 0, MaxLimit = 10 }
              };
    }
}
