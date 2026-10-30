namespace TodoApp.Features.Tasks.Tags.AddTagToTask;

/// <summary>Ajoute un tag par son NOM : le tag existant est réutilisé, sinon il est créé.</summary>
internal sealed record AddTagToTaskCommand(Guid TaskId, string TagName);
