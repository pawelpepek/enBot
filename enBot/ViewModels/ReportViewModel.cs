using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using enBot.Models;
using enBot.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace enBot.ViewModels;

public partial class ReportViewModel : ViewModelBase
{
    private readonly ReportService _service;

    [ObservableProperty] private ObservableCollection<ProgressReport> _reports = [];
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private bool _isLoading = true;

    public bool IsEmpty => !IsLoading && Reports.Count == 0;

    public ReportViewModel(ReportService service)
    {
        _service = service;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _service.GetRecentReportsAsync(20).ConfigureAwait(false);
            Reports = new ObservableCollection<ProgressReport>(result);
        }
        catch (Exception ex)
        {
            LogService.Log("[Report] LoadAsync failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnReportsChanged(ObservableCollection<ProgressReport> value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

    [RelayCommand]
    private async Task GenerateReport()
    {
        IsGenerating = true;
        try
        {
            await _service.GenerateReportAsync().ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogService.Log("[Report] GenerateReport failed", ex);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}
