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
using enBot.Models;
using System.Linq;

namespace enBot.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly AppRepository _storageService;

    [ObservableProperty] private int _totalPrompts;
    [ObservableProperty] private double _avgWeightedScore;
    [ObservableProperty] private double _avgWeightedComplexity;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _showPromptCounts = false;

    [ObservableProperty] private ISeries[] _chartSeries = [];
    [ObservableProperty] private Axis[] _chartXAxes = [];
    [ObservableProperty] private Axis[] _chartYAxes = [];

    public DashboardViewModel(AppRepository storageService)
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
            var promptsStatistics = await _storageService.GetAllStatsAsync().ConfigureAwait(false);

            TotalPrompts = promptsStatistics.TotalPrompts;
            AvgWeightedScore = Math.Round(promptsStatistics.AvgWeightedScore, 1);
            AvgWeightedComplexity = Math.Round(promptsStatistics.AvgWeightedComplexity, 1);

            await BuildChartAsync(promptsStatistics.DailyStatistics).ConfigureAwait(false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BuildChartAsync(List<DayPromptsStatistics> daysStatistics)
    {
        var counts = daysStatistics.Select(s => (double)s.TotalPrompts).ToList();
        var scores = daysStatistics.Select(s => Math.Round(s.AvgWeightedScore, 1)).ToList();
        var complexities = daysStatistics.Select(s => Math.Round(s.AvgWeightedComplexity, 1)).ToList();
        var labels = daysStatistics.Select(s => s.Date.ToString("MM/dd", CultureInfo.InvariantCulture)).ToList();

        var series = new List<ISeries>();

        if (ShowPromptCounts)
            series.Add(new ColumnSeries<double>
            {
                Name = "Prompts",
                Values = counts,
                Fill = new SolidColorPaint(SKColor.Parse("#5C6BC0"))
            });

        series.Add(
            new LineSeriesBuilder("Avg Score")
            .SetValues(scores)
            .SetColors("#4CAF50", "#4CAF50")
            .SetShowsYAt(ShowPromptCounts)
            .Build());

        series.Add(
            new LineSeriesBuilder("Avg Complexity")
            .SetValues(complexities)
            .SetColors("#FF9800", "#FF9800")
            .SetShowsYAt(ShowPromptCounts)
            .Build());

        ChartSeries = [.. series];

        ChartXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsRotation = labels.Count > 10 ? -45 : 0
            }
        ];

        ChartYAxes = ShowPromptCounts
            ?
              [
                  new Axis { Name = "Prompts", MinLimit = 0 },
                  new Axis { Name = "Score / Complexity", MinLimit = 0, MaxLimit = 10 }
              ]
            :
              [
                  new Axis { Name = "Score / Complexity", MinLimit = 0, MaxLimit = 10 }
              ];
    }
}
