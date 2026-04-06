using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using enBot.Models;
using enBot.Services.Analysis;
using enBot.Services.Infrastructure;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace enBot.ViewModels;

public partial class PromptSuggestionsViewModel : ViewModelBase
{
    private readonly PromptSuggestionService _service;

    [ObservableProperty] private ObservableCollection<PromptSuggestion> _suggestions = [];
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private bool _isLoading = true;

    public bool IsEmpty => !IsLoading && Suggestions.Count == 0;

    public PromptSuggestionsViewModel(PromptSuggestionService service)
    {
        _service = service;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _service.GetRecentSuggestionsAsync(20).ConfigureAwait(false);
            Suggestions = new ObservableCollection<PromptSuggestion>(result);
        }
        catch (Exception ex)
        {
            LogService.Log("[Suggestions] LoadAsync failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSuggestionsChanged(ObservableCollection<PromptSuggestion> value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

    [RelayCommand]
    private async Task GenerateSuggestion()
    {
        IsGenerating = true;
        try
        {
            await _service.GenerateSuggestionAsync().ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogService.Log("[Suggestions] GenerateSuggestion failed", ex);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}
