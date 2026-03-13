using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using VirtualTouchpad.Interop;
using VirtualTouchpad.Services;

namespace VirtualTouchpad.Views;

public partial class MainWindow : Window
{
    private readonly InputSimulator _inputSimulator = new();
    private readonly GestureStateMachine _gestureStateMachine;
    private readonly TouchInputHandler _touchInputHandler;

    public MainWindow()
    {
        InitializeComponent();

        _gestureStateMachine = new GestureStateMachine(_inputSimulator);
        _touchInputHandler = new TouchInputHandler(_gestureStateMachine);

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        if (hwndSource != null)
        {
            hwndSource.AddHook(WndProc);
            NativeMethods.RegisterTouchWindow(hwndSource.Handle, NativeConstants.TWF_FINETOUCH);

            _touchInputHandler.Initialize(hwndSource.Handle, TouchpadArea);
        }
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        handled = _touchInputHandler.ProcessMessage(hwnd, msg, wParam, lParam);
        return nint.Zero;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LeftClickButton_Click(object sender, RoutedEventArgs e)
    {
        _inputSimulator.LeftClick();
    }

    private void RightClickButton_Click(object sender, RoutedEventArgs e)
    {
        _inputSimulator.RightClick();
    }
}
