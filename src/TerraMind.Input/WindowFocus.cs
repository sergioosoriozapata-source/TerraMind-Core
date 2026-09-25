using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TerraMind.Input;

// Lleva la ventana de Terraria al frente para que SendInput le llegue.
// Sin esto el live no se mueve aunque el codigo este bien.
public static class WindowFocus
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_RESTORE = 9;

    public static bool FocusTerraria()
    {
        try
        {
            var proc = Process.GetProcessesByName("Terraria").FirstOrDefault();
            if (proc is null) return false;
            var h = proc.MainWindowHandle;
            if (h == IntPtr.Zero) return false;
            ShowWindow(h, SW_RESTORE);
            return SetForegroundWindow(h);
        }
        catch { return false; }
    }
}
