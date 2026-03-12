using System.Runtime.InteropServices;
using VirtualTouchpad.Interop;

namespace VirtualTouchpad.Services;

public class InputSimulator : IInputSimulator
{
    public void MoveCursorRelative(int dx, int dy)
    {
        var input = CreateMouseInput(NativeConstants.MOUSEEVENTF_MOVE, dx, dy);
        SendSingleInput(input);
    }

    public void LeftClick()
    {
        LeftDown();
        LeftUp();
    }

    public void RightClick()
    {
        RightDown();
        RightUp();
    }

    public void DoubleClick()
    {
        LeftClick();
        LeftClick();
    }

    public void LeftDown()
    {
        var input = CreateMouseInput(NativeConstants.MOUSEEVENTF_LEFTDOWN);
        SendSingleInput(input);
    }

    public void LeftUp()
    {
        var input = CreateMouseInput(NativeConstants.MOUSEEVENTF_LEFTUP);
        SendSingleInput(input);
    }

    public void RightDown()
    {
        var input = CreateMouseInput(NativeConstants.MOUSEEVENTF_RIGHTDOWN);
        SendSingleInput(input);
    }

    public void RightUp()
    {
        var input = CreateMouseInput(NativeConstants.MOUSEEVENTF_RIGHTUP);
        SendSingleInput(input);
    }

    public void Scroll(int delta)
    {
        var input = CreateMouseInput(NativeConstants.MOUSEEVENTF_WHEEL, mouseData: delta);
        SendSingleInput(input);
    }

    public void HorizontalScroll(int delta)
    {
        var input = CreateMouseInput(NativeConstants.MOUSEEVENTF_HWHEEL, mouseData: delta);
        SendSingleInput(input);
    }

    private static INPUT CreateMouseInput(uint flags, int dx = 0, int dy = 0, int mouseData = 0)
    {
        return new INPUT
        {
            type = NativeConstants.INPUT_MOUSE,
            u = new INPUT_UNION
            {
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = mouseData,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = NativeMethods.GetMessageExtraInfo()
                }
            }
        };
    }

    private static void SendSingleInput(INPUT input)
    {
        var inputs = new[] { input };
        NativeMethods.SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }
}
