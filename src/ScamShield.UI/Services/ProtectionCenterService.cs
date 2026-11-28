// Importe l'espace de noms ScamShield.UI.Models pour accéder aux classes/records QuarantineItem, NotificationBanner et ProtectionSettings.
// Pourquoi : Ce service manipule directement ces modèles de données définis dans le dossier Models.
using ScamShield.UI.Models;

// Déclare l'espace de noms du fichier selon la syntaxe "file-scoped" (sans accolades).
// Pourquoi : Range la classe dans le namespace ScamShield.UI.Services correspondant au dossier Services.
namespace ScamShield.UI.Services;

// Déclare une classe publique et scellée ("sealed") nommée ProtectionCenterService.
// Pourquoi "public" : Permet aux ViewModels, à l'UI et aux tests unitaires (xUnit) d'instancier et appeler ce service.
// Pourquoi "sealed" : Empêche d'hériter de cette classe ; évite des extensions involontaires et permet de légères optimisations au compilateur.
public sealed class ProtectionCenterService
{
    // Liste privée et lecture seule ("readonly") contenant les éléments actuellement mis en quarantaine.
    // "[]" est l'expression de collection vide en C# (équivalent à new List<QuarantineItem>()).
    // Pourquoi "private" : Protège la liste interne pour que l'extérieur ne la modifie pas n'importe comment sans passer par les méthodes du service.
    // Pourquoi "readonly" : Empêche de réassigner une nouvelle liste à _quarantineItems par erreur (la référence reste fixe, son contenu peut changer).
    private readonly List<QuarantineItem> _quarantineItems = [];

    // Liste privée et lecture seule contenant l'historique des bannières/notifications générées par le service.
    // Pourquoi : Garde l'état des notifications encapsulé dans le service.
    private readonly List<NotificationBanner> _notifications = [];

    // Propriété publique en lecture seule ("get;" sans "set;") initialisée avec une nouvelle instance de ProtectionSettings.
    // Pourquoi : Centralise les réglages de sécurité (seuils, protection temps réel, notifications Windows) au sein du service.
    public ProtectionSettings Settings { get; } = new();

    // Propriété publique exposant la quarantaine sous forme de IReadOnlyList<QuarantineItem>.
    // "=>" est une propriété calculée (expression-bodied member) qui retourne la référence de _quarantineItems.
    // Pourquoi IReadOnlyList<T> : Permet de lire et parcourir la liste à l'extérieur (UI, tests) sans exposer .Add(), .Remove() ou .Clear() directement.
    public IReadOnlyList<QuarantineItem> QuarantineItems => _quarantineItems;

    // Propriété publique exposant les notifications sous forme de IReadOnlyList<NotificationBanner>.
    // Pourquoi : Donne un accès en lecture aux notifications tout en protégeant la modification interne via PushNotification / ClearNotifications.
    public IReadOnlyList<NotificationBanner> Notifications => _notifications;

    // Fonction publique qui classe un score de risque (entre 0 et 100) en zone textuelle : "Dangereux", "Suspect" ou "Sain".
    // Pourquoi cette fonction : Centralise la règle métier de classification afin que tout le projet applique les mêmes seuils de façon cohérente.
    public string ClassifyRiskZone(int scorePercent)
    {
        // Garde d'entrée : vérifie si le score est hors de l'intervalle valide [0..100].
        // Pourquoi : Un score négatif ou supérieur à 100 n'a pas de sens pour un pourcentage de risque ; lever une exception signale clairement l'erreur.
        if (scorePercent < 0 || scorePercent > 100)
        {
            // Lève ArgumentOutOfRangeException en utilisant nameof(scorePercent) pour obtenir le nom exact du paramètre en chaîne.
            // Pourquoi nameof(...) : Évite les fautes de frappe et reste exact même si on renomme le paramètre plus tard.
            throw new ArgumentOutOfRangeException(nameof(scorePercent), "Le score doit être compris entre 0 et 100.");
        }

        // Calcule le seuil dangereux effectif en prenant le maximum entre DangerousThreshold et SuspectThreshold.
        // Pourquoi Math.Max : Sécurise le calcul même si l'utilisateur ou un test modifie les seuils de manière inversée.
        int dangerous = Math.Max(Settings.DangerousThreshold, Settings.SuspectThreshold);

        // Calcule le seuil suspect effectif en prenant le minimum entre SuspectThreshold et dangerous.
        // Pourquoi Math.Min : Garantit que le seuil suspect ne dépasse jamais le seuil dangereux lors de la classification.
        int suspect = Math.Min(Settings.SuspectThreshold, dangerous);

        // Teste d'abord si le score atteint ou dépasse le seuil "Dangereux".
        // Pourquoi cet ordre : Un score élevé (ex. 85) satisfait aussi le seuil suspect (ex. 40) ; tester "Dangereux" en premier évite un mauvais classement.
        if (scorePercent >= dangerous)
        {
            return "Dangereux";
        }

        // Teste ensuite si le score atteint ou dépasse le seuil "Suspect".
        // Pourquoi : Couvre la zone intermédiaire entre le seuil suspect et le seuil dangereux.
        if (scorePercent >= suspect)
        {
            return "Suspect";
        }

        // Retourne "Sain" si le score est inférieur aux seuils configurés.
        // Pourquoi : Fournit un résultat explicite et sûr pour les éléments sous le seuil de vigilance.
        return "Sain";
    }

    // Fonction publique qui met un fichier en quarantaine, enregistre l'élément et déclenche une notification.
    // Pourquoi cette fonction : Encapsule toutes les étapes d'isolement d'un fichier en une seule opération cohérente.
    public QuarantineItem QuarantineFile(string id, string fileName, string originalPath, int scorePercent, string reason)
    {
        // Valide que "id" n'est ni null, ni vide, ni uniquement composé d'espaces blancs.
        // Pourquoi : Un identifiant vide empêcherait de gérer ou retrouver proprement l'élément en quarantaine.
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        // Valide que "fileName" est renseigné.
        // Pourquoi : Évite de créer un élément de quarantaine sans nom de fichier.
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        // Valide que "originalPath" est renseigné.
        // Pourquoi : Sans chemin d'origine, il serait impossible de savoir où restaurer le fichier si l'utilisateur le demande.
        ArgumentException.ThrowIfNullOrWhiteSpace(originalPath);

        // Détermine la zone de risque ("Dangereux", "Suspect" ou "Sain") selon le score et les paramètres.
        // Pourquoi : Réutilise ClassifyRiskZone pour ne pas dupliquer la logique de seuils.
        string zone = ClassifyRiskZone(scorePercent);

        // Construit le chemin cible où le fichier isolé est censé être stocké.
        // "$@" combine une chaîne verbatim ("@", pas besoin de doubler les '\') et l'interpolation ("$", injection de variables).
        // Pourquoi : Standardise l'emplacement et le nom du fichier mis en quarantaine.
        string quarantinePath = $@"C:\ProgramData\ScamShield\Quarantine\{id}_{fileName}.quar";

        // Crée une instance du record QuarantineItem en utilisant des arguments nommés (Id:, FileName:, etc.).
        // Pourquoi les arguments nommés : Rend la lecture plus claire et évite d'inverser par erreur deux paramètres de type string.
        // Pourquoi DateTime.Now : Enregistre l'horodatage local de la mise en quarantaine.
        var item = new QuarantineItem(
            Id: id,
            FileName: fileName,
            OriginalPath: originalPath,
            QuarantinePath: quarantinePath,
            RiskZone: zone,
            RiskScorePercent: scorePercent,
            Reason: reason,
            QuarantinedAt: DateTime.Now
        );

        // Supprime tout élément existant ayant le même Id avant d'insérer le nouveau.
        // Pourquoi : Évite les doublons si le même identifiant est mis en quarantaine plusieurs fois.
        _quarantineItems.RemoveAll(q => q.Id == id);

        // Insère l'élément à l'index 0 (début de la liste).
        // Pourquoi l'index 0 : Place les éléments les plus récents en haut de la liste pour un affichage chronologique inverse.
        _quarantineItems.Insert(0, item);

        // Appelle PushNotification pour informer l'utilisateur de l'isolement du fichier.
        // La sévérité vaut "Danger" si la zone est "Dangereux", sinon "Warning".
        // Pourquoi : Donne un retour immédiat et adapté à la gravité de la menace détectée.
        PushNotification(
            title: "Fichier isolé en quarantaine",
            message: $"{fileName} a été mis en quarantaine ({zone}, {scorePercent}/100).",
            severity: zone == "Dangereux" ? "Danger" : "Warning"
        );

        // Retourne l'objet QuarantineItem créé.
        // Pourquoi : Permet à l'appelant (ViewModel ou test) d'utiliser ou vérifier l'élément créé.
        return item;
    }

    // Fonction publique qui restaure un fichier de la quarantaine à partir de son Id.
    // Retourne "true" si l'élément existait et a été retiré de la quarantaine, "false" sinon.
    // Pourquoi cette fonction : Permet à l'utilisateur de récupérer un fichier (par exemple un faux positif).
    public bool RestoreFile(string id)
    {
        // Cherche le premier élément correspondant à l'Id ; le type est nullable ("QuarantineItem?") car il peut être introuvable.
        QuarantineItem? item = _quarantineItems.FirstOrDefault(q => q.Id == id);

        // Si aucun élément ne correspond à l'Id, quitte immédiatement en retournant false.
        // Pourquoi : Évite une erreur et n'envoie pas de fausse notification de restauration pour un fichier absent.
        if (item is null)
        {
            return false;
        }

        // Retire l'élément de la liste de quarantaine puisqu'il est restauré.
        // Pourquoi : L'élément ne doit plus figurer comme isolé une fois restauré.
        _quarantineItems.Remove(item);

        // Ajoute une notification informative indiquant la restauration et le chemin d'origine.
        // Pourquoi : Informe l'utilisateur que l'action a bien été prise en compte.
        PushNotification(
            title: "Fichier restauré",
            message: $"{item.FileName} a été restauré vers {item.OriginalPath}.",
            severity: "Info"
        );

        // Indique que la restauration a réussi.
        return true;
    }

    // Fonction publique qui supprime définitivement un élément de la quarantaine à partir de son Id.
    // Retourne "true" si l'élément a été trouvé et supprimé, "false" sinon.
    // Pourquoi cette fonction : Permet d'éliminer définitivement une menace confirmée plutôt que de la restaurer.
    public bool DeletePermanently(string id)
    {
        // Recherche l'élément ciblé dans _quarantineItems.
        QuarantineItem? item = _quarantineItems.FirstOrDefault(q => q.Id == id);

        // Retourne false si l'élément n'est pas présent dans la quarantaine.
        // Pourquoi : Gère proprement le cas où l'Id est inexistant ou déjà supprimé.
        if (item is null)
        {
            return false;
        }

        // Supprime l'élément de la collection de quarantaine.
        // Pourquoi : Retire définitivement l'entrée de la liste active de quarantaine.
        _quarantineItems.Remove(item);

        // Pousse une notification confirmant la suppression définitive.
        // Pourquoi : Donne une trace visible de l'action effectuée.
        PushNotification(
            title: "Menace supprimée",
            message: $"{item.FileName} a été supprimé définitivement de la quarantaine.",
            severity: "Info"
        );

        // Confirme que la suppression a bien eu lieu.
        return true;
    }

    // Fonction publique qui ajoute une notification en tête de la liste si l'option est activée.
    // Pourquoi cette fonction : Centralise la création des bannières de notification et respecte les préférences de l'utilisateur.
    public void PushNotification(string title, string message, string severity)
    {
        // Vérifie si les notifications sont désactivées dans Settings.
        // Pourquoi : Si l'utilisateur a désactivé WindowsNotificationsEnabled, aucune notification ne doit être ajoutée.
        if (!Settings.WindowsNotificationsEnabled)
        {
            return;
        }

        // Insère une nouvelle NotificationBanner en position 0 pour afficher la plus récente en premier.
        // Pourquoi : Dans un flux d'alertes ou d'événements, l'utilisateur consulte généralement les dernières actions en haut.
        _notifications.Insert(0, new NotificationBanner(
            Title: title,
            Message: message,
            Severity: severity,
            CreatedAt: DateTime.Now
        ));
    }

    // Fonction publique (expression-bodied member "=>") qui vide toutes les notifications.
    // Pourquoi cette fonction : Permet d'effacer d'un clic l'ensemble des notifications affichées dans l'interface.
    public void ClearNotifications() => _notifications.Clear();
}