using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScamShield.UI.Models;

public sealed record QuarantineItem(
    string Id,
    string FileName,
    string OriginalPath,
    string QuarantinePath,
    string RiskZone,
    int RiskScorePercent,
    string Reason,
    DateTime QuarantinedAt
);

public sealed record NotificationBanner(
    string Title,
    string Message,
    string Severity,
    DateTime CreatedAt
);

public sealed class ProtectionSettings : INotifyPropertyChanged
{
    private bool _realTimeProtectionEnabled = true;
    private bool _autoQuarantineDangerous = true;
    private bool _windowsNotificationsEnabled = true;
    private int _suspectThreshold = 40;
    private int _dangerousThreshold = 75;

    public bool RealTimeProtectionEnabled
    {
        get => _realTimeProtectionEnabled;
        set => SetField(ref _realTimeProtectionEnabled, value);
    }

    public bool AutoQuarantineDangerous
    {
        get => _autoQuarantineDangerous;
        set => SetField(ref _autoQuarantineDangerous, value);
    }

    public bool WindowsNotificationsEnabled
    {
        get => _windowsNotificationsEnabled;
        set => SetField(ref _windowsNotificationsEnabled, value);
    }

    public int SuspectThreshold
    {
        get => _suspectThreshold;
        set
        {
            int clamped = Math.Clamp(value, 1, 99);
            if (!SetField(ref _suspectThreshold, clamped))
            {
                return;
            }

            if (_dangerousThreshold < _suspectThreshold)
            {
                DangerousThreshold = Math.Clamp(_suspectThreshold + 1, 1, 100);
            }
        }
    }

    public int DangerousThreshold
    {
        get => _dangerousThreshold;
        set
        {
            int clamped = Math.Clamp(value, 1, 100);
            if (!SetField(ref _dangerousThreshold, clamped))
            {
                return;
            }

            if (_suspectThreshold > _dangerousThreshold)
            {
                SuspectThreshold = Math.Clamp(_dangerousThreshold - 1, 1, 99);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}