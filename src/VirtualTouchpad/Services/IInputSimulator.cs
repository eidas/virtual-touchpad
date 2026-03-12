namespace VirtualTouchpad.Services;

public interface IInputSimulator
{
    void MoveCursorRelative(int dx, int dy);
    void LeftClick();
    void RightClick();
    void DoubleClick();
    void LeftDown();
    void LeftUp();
    void RightDown();
    void RightUp();
    void Scroll(int delta);
    void HorizontalScroll(int delta);
}
