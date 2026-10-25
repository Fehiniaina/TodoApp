using System.ComponentModel.DataAnnotations;

namespace TodoApp.Infrastructure.Storage;

internal sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Dossier racine. Relatif : résolu depuis le dossier de l'application.</summary>
    [Required]
    public string RootPath { get; set; } = string.Empty;
}
