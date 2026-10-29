namespace TodoApp.Domain.Tasks;

/// <summary>
/// Type de tâche. Les valeurs numériques sont PERSISTÉES (base, et plus tard outbox, cache, index) :
/// - ne jamais modifier le nombre d'un membre existant ;
/// - un nouveau type prend le prochain nombre libre ;
/// - l'ordre de déclaration n'a aucune importance (l'affichage trie sur les libellés).
/// Contrat vérifié par TaskTypeContractTests.
/// </summary>
public enum TaskType
{
    Bug = 0,
    Extraction = 1,
    Analyse = 2,
    Task = 3,
    Story = 4,
}
