using Avalonia.Controls;
using Avalonia.Platform.Storage;
using enBot.ViewModels;

namespace enBot.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        var browseButton = this.FindControl<Button>("BrowseButton")!;
        browseButton.Click += async (_, _) =>
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select project folder",
                AllowMultiple = false
            });

            if (folders.Count > 0 && DataContext is SettingsViewModel vm)
                vm.SelectedFolder = folders[0].Path.LocalPath;
        };
    }
}
