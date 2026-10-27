using System.Globalization;
using Microsoft.AspNetCore.Components;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Shared;
using TodoApp.Features.Users;

namespace TodoApp.Features.Tasks.ListTasks;

public sealed partial class TaskList
{
    private const string DateInputFormat = "yyyy-MM-dd";

    private static readonly TaskPriority[] PriorityOptions = Enum.GetValues<TaskPriority>();

    private static readonly (DueDateFilter Value, string Label)[] DueOptions =
    [
        (DueDateFilter.Any, "Toutes"),
        (DueDateFilter.Overdue, "En retard"),
        (DueDateFilter.Today, "Aujourd'hui"),
        (DueDateFilter.Next7Days, "7 prochains jours"),
        (DueDateFilter.NoDueDate, "Sans échéance"),
        (DueDateFilter.Range, "Plage de dates…"),
    ];

    [Inject] private GetAssignableUsersHandler AssignableUsersHandler { get; set; } = null!;

    private IReadOnlyList<UserSummary> _userOptions = [];

    /// <summary>
    /// « Plage » choisie mais aucune borne saisie : on affiche les champs sans naviguer
    /// (une plage vide serait normalisée en « Toutes » par TaskFilterUrl).
    /// </summary>
    private bool _editingRange;

    private bool ShowRangeInputs => _editingRange || CurrentFilter.DueDate == DueDateFilter.Range;

    private DateOnly? RangeFrom => CurrentFilter.DueDate == DueDateFilter.Range ? CurrentFilter.DueFrom : null;

    private DateOnly? RangeTo => CurrentFilter.DueDate == DueDateFilter.Range ? CurrentFilter.DueTo : null;

    /// <summary>Appelée depuis OnInitializedAsync.</summary>
    private async Task LoadFilterOptionsAsync()
    {
        var users = await AssignableUsersHandler.HandleAsync();

        // « Moi » en premier, puis ordre alphabétique.
        _userOptions =
        [
            .. users
                .OrderByDescending(u => string.Equals(u.Id,_createdById, StringComparison.Ordinal))
                .ThenBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase),
        ];
    }

    /// <summary>Passe par l'URL : OnParametersSetAsync détecte le changement et recharge.</summary>
    private void ApplyFilter(TaskFilter filter) =>
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameters(TaskFilterUrl.ToQueryParameters(filter)));

    private void ClearFilters()
    {
        _editingRange = false;
        ApplyFilter(TaskFilter.None);
    }

    private void TogglePriority(TaskPriority priority) =>
        ApplyFilter(CurrentFilter with { Priorities = Toggle(CurrentFilter.Priorities, priority) });

    private void ClearPriorities() =>
        ApplyFilter(CurrentFilter with { Priorities = new HashSet<TaskPriority>() });

    private void ToggleAssignee(string userId) =>
        ApplyFilter(CurrentFilter with { AssigneeIds = Toggle(CurrentFilter.AssigneeIds, userId) });

    private void ToggleUnassigned() =>
        ApplyFilter(CurrentFilter with { IncludeUnassigned = !CurrentFilter.IncludeUnassigned });

    private void ClearAssignees() =>
        ApplyFilter(CurrentFilter with
        {
            AssigneeIds = new HashSet<string>(StringComparer.Ordinal),
            IncludeUnassigned = false,
        });

    private bool IsDueSelected(DueDateFilter value) =>
        value == DueDateFilter.Range
            ? ShowRangeInputs
            : !_editingRange && CurrentFilter.DueDate == value;

    private void SetDueDate(DueDateFilter mode)
    {
        if (mode == DueDateFilter.Range)
        {
            _editingRange = true; // on attend une borne avant de naviguer
            return;
        }

        _editingRange = false;
        ApplyFilter(CurrentFilter with { DueDate = mode, DueFrom = null, DueTo = null });
    }

    private void SetDueFrom(ChangeEventArgs e) => ApplyRange(ParseDateInput(e), RangeTo);

    private void SetDueTo(ChangeEventArgs e) => ApplyRange(RangeFrom, ParseDateInput(e));

    private void ApplyRange(DateOnly? from, DateOnly? to)
    {
        // Les deux bornes effacées : on retire le filtre mais on garde les champs ouverts.
        _editingRange = from is null && to is null;

        ApplyFilter(_editingRange
            ? CurrentFilter with { DueDate = DueDateFilter.Any, DueFrom = null, DueTo = null }
            : CurrentFilter with { DueDate = DueDateFilter.Range, DueFrom = from, DueTo = to });
    }

    private static HashSet<T> Toggle<T>(IReadOnlySet<T> source, T value)
    {
        var copy = new HashSet<T>(source);
        if (!copy.Remove(value))
        {
            copy.Add(value);
        }
        return copy;
    }

    private static DateOnly? ParseDateInput(ChangeEventArgs e) =>
        DateOnly.TryParseExact(e.Value as string, DateInputFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date
            : null;

    private static string? FormatDateInput(DateOnly? date) =>
        date?.ToString(DateInputFormat, CultureInfo.InvariantCulture);
}
