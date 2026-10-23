namespace TodoApp.Infrastructure.Email.Resilience;

internal enum SmtpFailureKind
{
    /// <summary>Réseau, serveur occupé, 4xx, timeout : réessayer plus tard.</summary>
    Transient,

    /// <summary>5xx (boîte inexistante, adresse refusée…) : réessayer ne changera rien.</summary>
    Permanent,

    /// <summary>Identifiants ou TLS invalides : problème de configuration, pas de l'email.</summary>
    Configuration,
}
