using TodoApp.Domain.Tasks;

namespace TodoApp.Infrastructure.Seed;

internal static class SeedCatalog
{
    public static readonly TaskTemplate[] Tasks =
    [
        new("Corriger l'export CSV des factures",
            "Les montants supérieurs à 1 000 € sont exportés avec un séparateur de milliers qui casse l'import dans Excel.",
            TaskPriority.High,
            ["Reproduire le bug en local", "Écrire un test qui échoue", "Utiliser la culture invariante pour l'export", "Vérifier l'import dans Excel"]),

        new("Migrer le projet API vers .NET 10",
            "Mettre à jour le SDK, les packages NuGet et corriger les avertissements de compilation.",
            TaskPriority.Medium,
            ["Mettre à jour global.json", "Mettre à jour les packages NuGet", "Corriger les avertissements", "Lancer la suite de tests"]),

        new("Revue de code de la PR #{n}",
            "Ajout de la pagination sur l'écran de suivi des commandes.",
            TaskPriority.Medium,
            ["Lire la description de la PR", "Vérifier les tests", "Tester en local", "Laisser les commentaires"]),

        new("Préparer la démo de fin de sprint",
            "Démo prévue avec {client} : présenter les nouveaux écrans de suivi des livraisons.",
            TaskPriority.High,
            ["Préparer le jeu de données", "Rédiger le déroulé", "Faire une répétition"]),

        new("Ajouter un index sur la table des commandes",
            "La recherche par client dépasse 2 secondes en production.",
            TaskPriority.Medium,
            ["Analyser le plan d'exécution", "Créer la migration", "Mesurer le gain en recette"]),

        new("Renouveler le certificat SSL de la recette",
            "Le certificat expire dans 5 jours, l'environnement deviendra inaccessible.",
            TaskPriority.Urgent,
            ["Générer la demande de certificat", "Installer le nouveau certificat", "Vérifier l'accès HTTPS"]),

        new("Rédiger la documentation de l'API de facturation",
            "Compléter les descriptions OpenAPI et ajouter des exemples de requêtes.",
            TaskPriority.Low,
            ["Documenter les endpoints", "Ajouter des exemples", "Relire avec l'équipe"]),

        new("Mettre en place l'authentification à deux facteurs",
            "Demande de {client} suite à leur audit de sécurité.",
            TaskPriority.High,
            ["Activer TOTP dans Identity", "Créer l'écran d'enrôlement", "Générer les codes de secours", "Tester sur mobile"]),

        new("Corriger le crash au démarrage sur Android 14",
            "Plusieurs utilisateurs signalent une fermeture immédiate depuis la version {version}.",
            TaskPriority.Urgent,
            ["Récupérer les logs du crash", "Reproduire sur émulateur", "Publier un correctif"]),

        new("Organiser la rétrospective de sprint",
            null,
            TaskPriority.Low,
            ["Réserver la salle", "Préparer le tableau", "Envoyer le compte rendu"]),

        new("Mettre à jour les dépendances npm du front",
            "Trois vulnérabilités signalées par npm audit, dont une critique.",
            TaskPriority.Medium,
            ["Lancer npm audit", "Mettre à jour les paquets", "Vérifier le build", "Tester les écrans principaux"]),

        new("Analyser les lenteurs du tableau de bord",
            "Le chargement prend plus de 6 secondes pour les comptes avec beaucoup de commandes.",
            TaskPriority.High,
            ["Profiler les requêtes SQL", "Identifier les appels N+1", "Ajouter de la mise en cache"]),

        new("Configurer les alertes de monitoring",
            "Être prévenu avant les clients en cas de hausse des erreurs 500.",
            TaskPriority.Medium,
            ["Définir les seuils", "Créer les alertes", "Brancher les notifications Teams"]),

        new("Écrire les tests d'intégration du module paiement",
            null,
            TaskPriority.Medium,
            ["Paiement accepté", "Paiement refusé", "Remboursement partiel", "Délai d'attente dépassé"]),

        new("Préparer l'estimation pour {client}",
            "Chiffrage de la refonte du portail client, à envoyer avant vendredi.",
            TaskPriority.High,
            ["Lister les fonctionnalités", "Estimer chaque lot", "Faire relire par le chef de projet"]),

        new("Nettoyer les branches Git obsolètes",
            "Plus de 80 branches non fusionnées depuis plus de six mois.",
            TaskPriority.Low,
            []),

        new("Corriger l'envoi des e-mails de confirmation",
            "Les e-mails partent en double après une inscription.",
            TaskPriority.High,
            ["Reproduire le doublon", "Rendre l'envoi idempotent", "Ajouter un test de non-régression"]),

        new("Exécuter les tests dans le pipeline CI",
            "Aujourd'hui, le pipeline compile mais ne lance aucun test.",
            TaskPriority.Medium,
            ["Ajouter l'étape dotnet test", "Publier les rapports de couverture", "Bloquer la fusion si un test échoue"]),

        new("Vérifier la sauvegarde mensuelle de la production",
            null,
            TaskPriority.High,
            ["Lancer la sauvegarde", "Restaurer sur un environnement de test", "Archiver le rapport"]),

        new("Ajouter le mode sombre",
            "Fonctionnalité souvent demandée dans les retours utilisateurs.",
            TaskPriority.Low,
            ["Définir les couleurs", "Adapter les composants", "Respecter la préférence du système"]),

        new("Traduire l'interface en anglais",
            null,
            TaskPriority.Low,
            ["Extraire les textes dans des ressources", "Faire traduire", "Ajouter le sélecteur de langue"]),

        new("Point hebdomadaire avec {client}",
            "Suivi de l'avancement et des points bloquants.",
            TaskPriority.Medium,
            ["Préparer l'ordre du jour", "Mettre à jour le planning", "Envoyer le compte rendu"]),

        new("Accueillir {person} sur le projet",
            "Nouvelle arrivée dans l'équipe lundi.",
            TaskPriority.Medium,
            ["Créer les accès Git et Azure", "Présenter l'architecture", "Assigner un premier ticket"]),

        new("Corriger la faille XSS du champ commentaire",
            "Signalée par le pentest : le contenu n'est pas encodé à l'affichage.",
            TaskPriority.Urgent,
            ["Encoder la sortie HTML", "Ajouter un test de non-régression", "Prévenir l'équipe sécurité"]),

        new("Optimiser le poids des images du site",
            "Le score Lighthouse est pénalisé par des images de plus de 2 Mo.",
            TaskPriority.Low,
            ["Convertir en WebP", "Activer le chargement différé", "Mesurer le nouveau score"]),

        new("Déployer la version {version} en recette",
            null,
            TaskPriority.Medium,
            ["Générer les notes de version", "Lancer le déploiement", "Faire les tests de fumée", "Prévenir l'équipe de recette"]),

        new("Remplir la note de frais du mois",
            null,
            TaskPriority.Low,
            []),

        new("Préparer l'entretien technique de vendredi",
            "Candidat développeur .NET confirmé.",
            TaskPriority.Medium,
            ["Relire le CV", "Préparer l'exercice pratique", "Envoyer le retour au RH"]),

        new("Supprimer les feature flags obsolètes",
            "Les fonctionnalités sont en production depuis plus de trois mois.",
            TaskPriority.Low,
            ["Lister les flags", "Retirer le code mort", "Nettoyer la configuration"]),

        new("Investiguer les erreurs 500 sur /api/orders",
            "Pic d'erreurs depuis ce matin, environ 3 % des appels échouent.",
            TaskPriority.Urgent,
            ["Analyser les logs", "Identifier le déploiement en cause", "Corriger ou revenir en arrière", "Rédiger le post-mortem"]),
    ];
}
