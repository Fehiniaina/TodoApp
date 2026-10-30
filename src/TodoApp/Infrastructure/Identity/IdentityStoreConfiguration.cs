using Microsoft.AspNetCore.Identity;

namespace TodoApp.Infrastructure.Identity;

/// <summary>
/// Options qui façonnent le SCHÉMA Identity (longueur des clés, version du schéma).
/// Partagées entre l'application et les tests : un écart produirait un modèle EF différent
/// de celui des migrations (PendingModelChangesWarning).
/// </summary>
internal static class IdentityStoreConfiguration
{
    public static void Configure(StoreOptions stores)
    {
        ArgumentNullException.ThrowIfNull(stores);

        // Recopie ICI les valeurs actuellement configurées dans Program.cs.
        stores.SchemaVersion = IdentitySchemaVersions.Version3;
    }
}
