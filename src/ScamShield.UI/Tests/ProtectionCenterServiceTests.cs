using ScamShield.UI.Services;
using Xunit;

namespace ScamShield.UI.Tests;

public sealed class ProtectionCenterServiceTests
{
    [Fact]
    public void QuarantineFile_ThenRestoreFile_RemovesFromQuarantineAndAddsNotification()
    {
        var service = new ProtectionCenterService();

        var item = service.QuarantineFile(
            id: "Q-100",
            fileName: "eicar.com",
            originalPath: @"C:\Users\Joseph\Downloads\eicar.com",
            scorePercent: 95,
            reason: "Signature de test détectée"
        );

        Assert.Single(service.QuarantineItems);
        Assert.Equal("Dangereux", item.RiskZone);
        Assert.NotEmpty(service.Notifications);

        bool restored = service.RestoreFile("Q-100");

        Assert.True(restored);
        Assert.Empty(service.QuarantineItems);
    }

    [Fact]
    public void DeletePermanently_ExistingItem_DeletesItem()
    {
        var service = new ProtectionCenterService();
        service.QuarantineFile("Q-101", "suspect.exe", @"D:\suspect.exe", 55, "Non signé");

        bool deleted = service.DeletePermanently("Q-101");

        Assert.True(deleted);
        Assert.Empty(service.QuarantineItems);
    }

    [Fact]
    public void ClassifyRiskZone_CustomThresholds_RespectsSettings()
    {
        var service = new ProtectionCenterService();
        service.Settings.SuspectThreshold = 30;
        service.Settings.DangerousThreshold = 70;

        Assert.Equal("Sain", service.ClassifyRiskZone(25));
        Assert.Equal("Suspect", service.ClassifyRiskZone(45));
        Assert.Equal("Dangereux", service.ClassifyRiskZone(80));
    }
}