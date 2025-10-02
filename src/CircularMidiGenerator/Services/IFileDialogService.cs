using System.Threading.Tasks;

namespace CircularMidiGenerator.Services;

/// <summary>
/// Service interface for file dialog operations
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Shows a save file dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="defaultFileName">Default file name</param>
    /// <param name="filter">File filter (e.g., "Configuration Files|*.cmg")</param>
    /// <returns>Selected file path or null if cancelled</returns>
    Task<string?> ShowSaveFileDialogAsync(string title, string? defaultFileName = null, string? filter = null);

    /// <summary>
    /// Shows an open file dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="filter">File filter (e.g., "Configuration Files|*.cmg")</param>
    /// <returns>Selected file path or null if cancelled</returns>
    Task<string?> ShowOpenFileDialogAsync(string title, string? filter = null);

    /// <summary>
    /// Shows a folder browser dialog
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <returns>Selected folder path or null if cancelled</returns>
    Task<string?> ShowFolderBrowserDialogAsync(string title);
}