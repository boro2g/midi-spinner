using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace CircularMidiGenerator.Services;

/// <summary>
/// Avalonia implementation of file dialog service
/// </summary>
public class FileDialogService : IFileDialogService
{
    private readonly Window _parentWindow;

    public FileDialogService(Window parentWindow)
    {
        _parentWindow = parentWindow ?? throw new System.ArgumentNullException(nameof(parentWindow));
    }

    /// <inheritdoc />
    public async Task<string?> ShowSaveFileDialogAsync(string title, string? defaultFileName = null, string? filter = null)
    {
        var storageProvider = _parentWindow.StorageProvider;
        
        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultFileName
        };

        if (!string.IsNullOrEmpty(filter))
        {
            options.FileTypeChoices = ParseFileFilter(filter);
        }

        var result = await storageProvider.SaveFilePickerAsync(options);
        return result?.Path.LocalPath;
    }

    /// <inheritdoc />
    public async Task<string?> ShowOpenFileDialogAsync(string title, string? filter = null)
    {
        var storageProvider = _parentWindow.StorageProvider;
        
        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        if (!string.IsNullOrEmpty(filter))
        {
            options.FileTypeFilter = ParseFileFilter(filter);
        }

        var result = await storageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    /// <inheritdoc />
    public async Task<string?> ShowFolderBrowserDialogAsync(string title)
    {
        var storageProvider = _parentWindow.StorageProvider;
        
        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        var result = await storageProvider.OpenFolderPickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    private static FilePickerFileType[] ParseFileFilter(string filter)
    {
        // Simple filter parsing for "Description|*.ext" format
        var parts = filter.Split('|');
        if (parts.Length >= 2)
        {
            var description = parts[0];
            var extensions = parts[1].Replace("*.", "").Split(';');
            
            return new[]
            {
                new FilePickerFileType(description)
                {
                    Patterns = extensions.Select(ext => $"*.{ext}").ToArray()
                }
            };
        }

        return new[] { FilePickerFileTypes.All };
    }
}