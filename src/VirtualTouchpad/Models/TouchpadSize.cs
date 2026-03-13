namespace VirtualTouchpad.Models;

public enum TouchpadSizePreset
{
    Small,
    Medium,
    Large
}

public static class TouchpadSize
{
    public static (int Width, int Height) GetSize(TouchpadSizePreset preset) => preset switch
    {
        TouchpadSizePreset.Small => (200, 150),
        TouchpadSizePreset.Medium => (300, 220),
        TouchpadSizePreset.Large => (400, 300),
        _ => (300, 220)
    };
}
