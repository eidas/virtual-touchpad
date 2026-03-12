using VirtualTouchpad.Helpers;
using Xunit;

namespace VirtualTouchpad.Tests;

public class AccelerationCurveTests
{
    private readonly AccelerationCurve _curve = new();

    [Fact]
    public void ZeroInput_ReturnsZero()
    {
        var (dx, dy) = _curve.Apply(0, 0);
        Assert.Equal(0, dx);
        Assert.Equal(0, dy);
    }

    [Fact]
    public void SlowMovement_AppliesBaseSensitivity()
    {
        // Small, slow movement - should stay close to input
        var (dx, dy) = _curve.Apply(1, 0);
        Assert.True(Math.Abs(dx) > 0);
        Assert.Equal(0, dy);
    }

    [Fact]
    public void FastMovement_HasHigherMultiplier()
    {
        double slowMultiplier = _curve.CalculateMultiplier(1.0);
        double fastMultiplier = _curve.CalculateMultiplier(15.0);

        Assert.True(fastMultiplier > slowMultiplier,
            $"Fast multiplier ({fastMultiplier}) should be greater than slow multiplier ({slowMultiplier})");
    }

    [Fact]
    public void MultiplierIncreasesWithSpeed()
    {
        double m1 = _curve.CalculateMultiplier(1.0);
        double m5 = _curve.CalculateMultiplier(5.0);
        double m15 = _curve.CalculateMultiplier(15.0);
        double m30 = _curve.CalculateMultiplier(30.0);

        Assert.True(m1 <= m5);
        Assert.True(m5 <= m15);
        Assert.True(m15 <= m30);
    }

    [Fact]
    public void Sensitivity_AffectsMultiplier()
    {
        _curve.Sensitivity = 1;
        double lowSensMultiplier = _curve.CalculateMultiplier(5.0);

        _curve.Sensitivity = 10;
        double highSensMultiplier = _curve.CalculateMultiplier(5.0);

        Assert.True(highSensMultiplier > lowSensMultiplier,
            $"High sensitivity ({highSensMultiplier}) should produce larger multiplier than low ({lowSensMultiplier})");
    }

    [Fact]
    public void Sensitivity_ClampedToValidRange()
    {
        _curve.Sensitivity = 0;
        Assert.Equal(1.0, _curve.Sensitivity);

        _curve.Sensitivity = 15;
        Assert.Equal(10.0, _curve.Sensitivity);
    }

    [Fact]
    public void DiagonalMovement_AppliesCorrectly()
    {
        var (dx, dy) = _curve.Apply(3, 4); // speed = 5
        // Both components should be non-zero
        Assert.True(Math.Abs(dx) > 0);
        Assert.True(Math.Abs(dy) > 0);
        // Direction should be preserved
        Assert.True(dx > 0);
        Assert.True(dy > 0);
    }

    [Fact]
    public void NegativeMovement_PreservesDirection()
    {
        var (dx, dy) = _curve.Apply(-5, -3);
        Assert.True(dx < 0);
        Assert.True(dy < 0);
    }
}
