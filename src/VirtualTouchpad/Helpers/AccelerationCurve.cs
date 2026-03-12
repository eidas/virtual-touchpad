namespace VirtualTouchpad.Helpers;

public class AccelerationCurve
{
    private double _sensitivity = 5.0;

    public double Sensitivity
    {
        get => _sensitivity;
        set => _sensitivity = Math.Clamp(value, 1.0, 10.0);
    }

    /// <summary>
    /// Applies acceleration curve to raw touch delta values.
    /// Slow movements map close to 1:1 for precision,
    /// fast movements are amplified for quick traversal.
    /// </summary>
    public (double dx, double dy) Apply(double rawDx, double rawDy)
    {
        double speed = Math.Sqrt(rawDx * rawDx + rawDy * rawDy);
        double multiplier = CalculateMultiplier(speed);

        return (rawDx * multiplier, rawDy * multiplier);
    }

    internal double CalculateMultiplier(double speed)
    {
        // Base sensitivity factor (1.0 at sensitivity=5, range 0.4-2.0)
        double baseFactor = 0.2 + (_sensitivity * 0.18);

        if (speed < 2.0)
        {
            // Slow movement: near 1:1 mapping for precision
            return baseFactor;
        }
        else if (speed < 10.0)
        {
            // Medium movement: gradual acceleration
            double t = (speed - 2.0) / 8.0;
            return baseFactor * (1.0 + t * 1.5);
        }
        else
        {
            // Fast movement: strong acceleration
            double t = Math.Min((speed - 10.0) / 20.0, 1.0);
            return baseFactor * (2.5 + t * 2.0);
        }
    }
}
