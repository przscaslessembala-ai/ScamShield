using ScamShield.Core.Models;
using Xunit;

namespace ScamShield.Core.Tests;

public sealed class VerdictPolicyTests
{
    [Theory]
    [InlineData(0.0, Verdict.Safe)]
    [InlineData(0.49, Verdict.Safe)]
    [InlineData(0.5, Verdict.Suspicious)]
    [InlineData(0.79, Verdict.Suspicious)]
    [InlineData(0.8, Verdict.Dangerous)]
    [InlineData(1.0, Verdict.Dangerous)]
    public void EvaluateReturnsExpectedVerdict(double score, Verdict expected)
    {
        var verdict = VerdictPolicy.Default.Evaluate(score);

        Assert.Equal(expected, verdict);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void EvaluateRejectsScoreOutsideRange(double score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VerdictPolicy.Default.Evaluate(score));
    }

    [Theory]
    [InlineData(0.0, 0.8)]
    [InlineData(0.8, 0.5)]
    [InlineData(0.5, 1.5)]
    public void ConstructorRejectsInconsistentThresholds(double suspicious, double dangerous)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerdictPolicy(suspicious, dangerous));
    }

    [Fact]
    public void CustomThresholdsAreApplied()
    {
        var strict = new VerdictPolicy(0.3, 0.6);

        Assert.Equal(Verdict.Suspicious, strict.Evaluate(0.3));
        Assert.Equal(Verdict.Dangerous, strict.Evaluate(0.6));
    }
}
