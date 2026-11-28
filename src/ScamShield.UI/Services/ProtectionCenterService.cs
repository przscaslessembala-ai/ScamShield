using ScamShield.UI.Models;

namespace ScamShield.UI.Services;

public sealed class ProtectionCenterService
{
    private readonly List<QuarantineItem> _quarantineItems = [];
    private readonly List<NotificationBanner> _notifications = [];

    public ProtectionSettings Settings { get; } = new();

    public IReadOnlyList<QuarantineItem> QuarantineItems => _quarantineItems;
    public IReadOnlyList<NotificationBanner> Notifications => _notifications;

    public string ClassifyRiskZone(int scorePercent)
    {
        if (scorePercent < 0 || scorePercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(scorePercent), "Le score doit être compris entre 0 et 100.");
        }

        int dangerous = Math.Max(Settings.DangerousThreshold, Settings.SuspectThreshold);
        int suspect = Math.Min(Settings.SuspectThreshold, dangerous);

        if (scorePercent >= dangerous)
        {
            return "Dangereux";
        }

        if (scorePercent >= suspect)
        {
            return "Suspect";
        }

        return "Sain";
    }

    public QuarantineItem QuarantineFile(string id, string fileName, string originalPath, int scorePercent, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalPath);

        string zone = ClassifyRiskZone(scorePercent);
        string quarantinePath = $@"C:\ProgramData\ScamShield\Quarantine\{id}_{fileName}.quar";

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

        _quarantineItems.RemoveAll(q => q.Id == id);
        _quarantineItems.Insert(0, item);

        PushNotification(
            title: "Fichier isolé en quarantaine",
            message: $"{fileName} a été mis en quarantaine ({zone}, {scorePercent}/100).",
            severity: zone == "Dangereux" ? "Danger" : "Warning"
        );

        return item;
    }

    public bool RestoreFile(string id)
    {
        QuarantineItem? item = _quarantineItems.FirstOrDefault(q => q.Id == id);
        if (item is null)
        {
            return false;
        }

        _quarantineItems.Remove(item);
        PushNotification(
            title: "Fichier restauré",
            message: $"{item.FileName} a été restauré vers {item.OriginalPath}.",
            severity: "Info"
        );
        return true;
    }

    public bool DeletePermanently(string id)
    {
        QuarantineItem? item = _quarantineItems.FirstOrDefault(q => q.Id == id);
        if (item is null)
        {
            return false;
        }

        _quarantineItems.Remove(item);
        PushNotification(
            title: "Menace supprimée",
            message: $"{item.FileName} a été supprimé définitivement de la quarantaine.",
            severity: "Info"
        );
        return true;
    }

    public void PushNotification(string title, string message, string severity)
    {
        if (!Settings.WindowsNotificationsEnabled)
        {
            return;
        }

        _notifications.Insert(0, new NotificationBanner(
            Title: title,
            Message: message,
            Severity: severity,
            CreatedAt: DateTime.Now
        ));
    }

    public void ClearNotifications() => _notifications.Clear();
}
