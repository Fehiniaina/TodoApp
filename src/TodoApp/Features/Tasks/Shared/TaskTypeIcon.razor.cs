using Microsoft.AspNetCore.Components;
using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

public partial class TaskTypeIcon
{
    [Parameter, EditorRequired] public TaskType Type { get; set; }

    /// <summary>Vrai quand le libellé est déjà affiché à côté : évite une double lecture.</summary>
    [Parameter] public bool Decorative { get; set; }

    // Une forme distincte par type : le sens ne repose pas sur la seule couleur.
    // MarkupString sans risque ici : chaînes CONSTANTES, aucune donnée utilisateur.
    private MarkupString Glyph => new(Type switch
    {
        TaskType.Bug => """<circle cx="8" cy="8" r="3.5" />""",
        TaskType.Story => """<path d="M4.5 2.5h7v11L8 10.5l-3.5 3z" />""",
        TaskType.Task => """<path d="M4 8.5l2.5 2.5L12 5.5" class="stroke" />""",
        TaskType.Extraction => """<path d="M8 3v7M5 7.5L8 10.5 11 7.5M4 13h8" class="stroke" />""",
        TaskType.Analyse => """<g class="stroke"><circle cx="7" cy="7" r="3.5" /><path d="M9.5 9.5L13 13" /></g>""",
        _ => """<circle cx="8" cy="8" r="2" />""",
    });
}
