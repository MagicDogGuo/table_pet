using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace TablePet.Shell;

public static class ScreenBounds
{
    private const uint MonitorDefaultToNearest = 2;

    public static Rect GetWorkArea(Window window)
    {
        var source = PresentationSource.FromVisual(window);
        var fromDevice = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var toDevice = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;

        var dipCenter = new Point(
            window.Left + (window.ActualWidth / 2),
            window.Top + (window.ActualHeight / 2));
        var pixelCenter = toDevice.Transform(dipCenter);

        var monitor = MonitorFromPoint(
            new NativePoint { X = (int)Math.Round(pixelCenter.X), Y = (int)Math.Round(pixelCenter.Y) },
            MonitorDefaultToNearest);

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
        {
            return SystemParameters.WorkArea;
        }

        var topLeft = fromDevice.Transform(new Point(info.Work.Left, info.Work.Top));
        var bottomRight = fromDevice.Transform(new Point(info.Work.Right, info.Work.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    public static void ClampToWorkArea(Window window)
    {
        if (window.ActualWidth <= 0 || window.ActualHeight <= 0)
        {
            return;
        }

        var work = GetWorkArea(window);
        var maxLeft = work.Right - window.ActualWidth;
        var maxTop = work.Bottom - window.ActualHeight;
        window.Left = Math.Clamp(window.Left, work.Left, Math.Max(work.Left, maxLeft));
        window.Top = Math.Clamp(window.Top, work.Top, Math.Max(work.Top, maxTop));
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }
}
