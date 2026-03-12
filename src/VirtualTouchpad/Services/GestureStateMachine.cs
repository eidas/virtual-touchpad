using VirtualTouchpad.Helpers;

namespace VirtualTouchpad.Services;

public enum GestureState
{
    Idle,
    Tracking,
    CursorMove,
    HoldReady,
    Dragging
}

public class GestureStateMachine
{
    private readonly IInputSimulator _inputSimulator;
    private readonly AccelerationCurve _accelerationCurve = new();

    // State
    private GestureState _state = GestureState.Idle;
    private uint _activePointerId;

    // Touch tracking
    private double _startX, _startY;
    private double _lastX, _lastY;
    private long _touchDownTimestamp;
    private long _lastTapTimestamp;
    private int _activeTouchCount;

    // Configuration
    private const double TapMaxMovement = 10.0;
    private const long TapMaxDurationMs = 200;
    private const long DoubleTapIntervalMs = 300;
    private const long HoldThresholdMs = 500;

    // Hold detection timer
    private System.Threading.Timer? _holdTimer;

    public GestureState CurrentState => _state;

    public GestureStateMachine(IInputSimulator inputSimulator)
    {
        _inputSimulator = inputSimulator;
    }

    public void OnTouchDown(uint pointerId, double x, double y)
    {
        _activeTouchCount++;

        if (_state != GestureState.Idle)
            return;

        _activePointerId = pointerId;
        _startX = x;
        _startY = y;
        _lastX = x;
        _lastY = y;
        _touchDownTimestamp = GetTimestampMs();
        _state = GestureState.Tracking;

        // Start hold detection timer
        _holdTimer?.Dispose();
        _holdTimer = new System.Threading.Timer(OnHoldTimerElapsed, null, HoldThresholdMs, System.Threading.Timeout.Infinite);
    }

    public void OnTouchMove(uint pointerId, double x, double y)
    {
        if (pointerId != _activePointerId)
            return;

        double dx = x - _lastX;
        double dy = y - _lastY;

        switch (_state)
        {
            case GestureState.Tracking:
                double totalDx = x - _startX;
                double totalDy = y - _startY;
                double totalDistance = Math.Sqrt(totalDx * totalDx + totalDy * totalDy);

                if (totalDistance > TapMaxMovement)
                {
                    _state = GestureState.CursorMove;
                    CancelHoldTimer();
                    ApplyMovement(dx, dy);
                }
                break;

            case GestureState.CursorMove:
                ApplyMovement(dx, dy);
                break;

            case GestureState.HoldReady:
                // Start dragging
                _state = GestureState.Dragging;
                _inputSimulator.LeftDown();
                ApplyMovement(dx, dy);
                break;

            case GestureState.Dragging:
                ApplyMovement(dx, dy);
                break;
        }

        _lastX = x;
        _lastY = y;
    }

    public void OnTouchUp(uint pointerId, double x, double y)
    {
        _activeTouchCount = Math.Max(0, _activeTouchCount - 1);

        if (pointerId != _activePointerId)
            return;

        CancelHoldTimer();

        switch (_state)
        {
            case GestureState.Tracking:
                long elapsed = GetTimestampMs() - _touchDownTimestamp;
                double totalDx = x - _startX;
                double totalDy = y - _startY;
                double totalDistance = Math.Sqrt(totalDx * totalDx + totalDy * totalDy);

                if (totalDistance <= TapMaxMovement && elapsed <= TapMaxDurationMs)
                {
                    HandleTap();
                }
                break;

            case GestureState.CursorMove:
                // Just stop moving
                break;

            case GestureState.HoldReady:
                // Hold without drag - treat as no-op
                break;

            case GestureState.Dragging:
                _inputSimulator.LeftUp();
                break;
        }

        _state = GestureState.Idle;
    }

    private void HandleTap()
    {
        long now = GetTimestampMs();
        long sinceLastTap = now - _lastTapTimestamp;

        if (sinceLastTap <= DoubleTapIntervalMs)
        {
            _inputSimulator.DoubleClick();
            _lastTapTimestamp = 0; // Reset to prevent triple-tap
        }
        else
        {
            _inputSimulator.LeftClick();
            _lastTapTimestamp = now;
        }
    }

    private void ApplyMovement(double dx, double dy)
    {
        var (acceleratedDx, acceleratedDy) = _accelerationCurve.Apply(dx, dy);
        _inputSimulator.MoveCursorRelative((int)Math.Round(acceleratedDx), (int)Math.Round(acceleratedDy));
    }

    private void OnHoldTimerElapsed(object? state)
    {
        if (_state == GestureState.Tracking)
        {
            _state = GestureState.HoldReady;
        }
    }

    private void CancelHoldTimer()
    {
        _holdTimer?.Dispose();
        _holdTimer = null;
    }

    private static long GetTimestampMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    // For testing
    internal void Reset()
    {
        _state = GestureState.Idle;
        _activeTouchCount = 0;
        _lastTapTimestamp = 0;
        CancelHoldTimer();
    }
}
