using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TodoApp.Features.Shared.Notifications;

namespace TodoApp.Features.Tasks.Import;

public sealed partial class ImportTasksPage
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    [Inject] private PreviewTaskImportHandler PreviewHandler { get; set; } = null!;
    [Inject] private ImportTasksHandler ImportHandler { get; set; } = null!;
    [Inject] private NotificationService Toasts { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ILogger<ImportTasksPage> Logger { get; set; } = null!;

    private TaskImportPreview? _preview;
    private string? _fileName;
    private string? _fileError;
    private bool _isReading;
    private bool _isImporting;
    private const string TemplateUrl = "tasks/import/template";

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs e)
    {
        _fileError = null;
        var file = e.File;

        if (!file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            _fileError = "Seuls les fichiers .csv sont acceptés.";
            return;
        }

        if (file.Size > TaskCsvParser.MaxFileSize)
        {
            _fileError = "Le fichier dépasse 2 Mo : découpez-le en plusieurs fichiers.";
            return;
        }

        _isReading = true;
        try
        {
            // Le flux du navigateur passe par SignalR et ne se lit qu'une fois, en asynchrone :
            // on le copie en mémoire (2 Mo maximum) avant de le donner à CsvHelper.
            await using var upload = file.OpenReadStream(TaskCsvParser.MaxFileSize);
            using var buffer = new MemoryStream();
            await upload.CopyToAsync(buffer);
            buffer.Position = 0;

            _preview = await PreviewHandler.HandleAsync(buffer);
            _fileName = file.Name;
        }
        catch (IOException ex)
        {
            LogReadFailed(Logger, ex);
            _fileError = "Impossible de lire le fichier. Réessayez.";
        }
        finally
        {
            _isReading = false;
        }
    }

    private async Task ImportAsync()
    {
        if (_preview is not { CanImport: true })
        {
            return;
        }

        _isImporting = true;
        try
        {
            var result = await ImportHandler.HandleAsync(new ImportTasksCommand(_preview.Rows));

            if (result.IsFailure)
            {
                Toasts.Warning(result.Error.Message, "Import impossible");
                return;
            }

            Toasts.Success($"{result.Value} tâche(s) importée(s).", "Import terminé");
            Navigation.NavigateTo("tasks");
        }
        finally
        {
            _isImporting = false;
        }
    }

    private void Reset()
    {
        _preview = null;
        _fileName = null;
        _fileError = null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Lecture du fichier d'import impossible")]
    private static partial void LogReadFailed(ILogger logger, Exception exception);
}
