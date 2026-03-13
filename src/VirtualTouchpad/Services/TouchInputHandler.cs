using System.Windows;
using System.Windows.Media;
using VirtualTouchpad.Interop;

namespace VirtualTouchpad.Services;

public class TouchInputHandler
{
    private readonly GestureStateMachine _gestureStateMachine;
    private nint _hwnd;
    private FrameworkElement? _touchpadElement;

    public TouchInputHandler(GestureStateMachine gestureStateMachine)
    {
        _gestureStateMachine = gestureStateMachine;
    }

    public void Initialize(nint hwnd, FrameworkElement touchpadElement)
    {
        _hwnd = hwnd;
        _touchpadElement = touchpadElement;
    }

    public bool ProcessMessage(nint hwnd, int msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case NativeConstants.WM_POINTERDOWN:
            case NativeConstants.WM_POINTERUPDATE:
            case NativeConstants.WM_POINTERUP:
                return HandlePointerMessage(msg, wParam, lParam);
            default:
                return false;
        }
    }

    private bool HandlePointerMessage(int msg, nint wParam, nint lParam)
    {
        uint pointerId = (uint)(wParam.ToInt64() & 0xFFFF);

        if (!NativeMethods.GetPointerInfo(pointerId, out var pointerInfo))
            return false;

        // Only handle touch input (ignore pen, mouse, etc.)
        if (pointerInfo.pointerType != NativeConstants.PT_TOUCH)
            return false;

        // Check if the touch point is within the touchpad area
        if (!IsPointInTouchpadArea(pointerInfo.ptPixelLocation))
            return false;

        // Convert screen coordinates to touchpad-local coordinates
        var localPoint = ScreenToTouchpadLocal(pointerInfo.ptPixelLocation);

        switch (msg)
        {
            case NativeConstants.WM_POINTERDOWN:
                _gestureStateMachine.OnTouchDown(pointerId, localPoint.X, localPoint.Y);
                break;
            case NativeConstants.WM_POINTERUPDATE:
                _gestureStateMachine.OnTouchMove(pointerId, localPoint.X, localPoint.Y);
                break;
            case NativeConstants.WM_POINTERUP:
                _gestureStateMachine.OnTouchUp(pointerId, localPoint.X, localPoint.Y);
                break;
        }

        // Return true to suppress OS touch-to-mouse conversion
        return true;
    }

    private bool IsPointInTouchpadArea(POINT screenPoint)
    {
        if (_touchpadElement == null)
            return false;

        var elementPoint = _touchpadElement.PointFromScreen(
            new Point(screenPoint.X, screenPoint.Y));

        return elementPoint.X >= 0
            && elementPoint.Y >= 0
            && elementPoint.X <= _touchpadElement.ActualWidth
            && elementPoint.Y <= _touchpadElement.ActualHeight;
    }

    private Point ScreenToTouchpadLocal(POINT screenPoint)
    {
        if (_touchpadElement == null)
            return new Point(0, 0);

        return _touchpadElement.PointFromScreen(
            new Point(screenPoint.X, screenPoint.Y));
    }
}
