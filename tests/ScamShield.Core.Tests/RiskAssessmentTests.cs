using ScamShield.Core.Models;
using Xunit;

namespace ScamShield.Core.Tests;

public sealed class RiskAssessmentTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.42)]
    [InlineData(1.0)]
    public void AcceptsScoreInsideRange(double score)
    {
        var assessment = new RiskAssessment(score, []);

        Assert.Equal(score, assessment.Score);
        Assert.Empty(assessment.Reasons);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void RejectsScoreOutsideRange(double score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RiskAssessment(score, []));
    }

    [Fact]
    public void RejectsNullReasons()
    {
        Assert.Throws<ArgumentNullException>(() => new RiskAssessment(0.5, null!));
    }

    [Fact]
    public void FeatureNamesAreUnique()
    {
        Assert.Equal(FeatureNames.All.Count, FeatureNames.All.Distinct().Count());
    }
}
