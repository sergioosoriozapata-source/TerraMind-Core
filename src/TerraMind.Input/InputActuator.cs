using System.Runtime.InteropServices;
using TerraMind.Brain;

namespace TerraMind.Input;

// Actuador Vanilla via SendInput. v0.5 invocador.
// Añade: teclas 1-9/0 para hotbar + click izquierdo para invocar/atacar con latigo.
// Sin esto el invocador no puede ni seleccionar el baculo ni lanzar el minion.
public sealed class InputActuator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT { public uint uMsg; public ushort wParamL; public ushort wParamH; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion u; }

    private const uint INPUT_KEYBOARD = 1;
    private const uint INPUT_MOUSE = 0;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const ushort VK_A = 0x41, VK_D = 0x44, VK_SPACE = 0x20, VK_H = 0x48;
    private static ushort VkDigit(int slot0to9) => (ushort)(slot0to9 == 9 ? 0x30 : 0x31 + slot0to9);

    private readonly HashSet<ushort> _down = new();
    public bool DryRun { get; set; } = true;
    public bool Verbose { get; set; } = true;

    public static uint SendKeyRaw(ushort vk, bool down)
    {
        var inp = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = down ? 0u : KEYEVENTF_KEYUP, time = 0, dwExtraInfo = IntPtr.Zero } }
        };
        return SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
    }

    public static uint SendMouseClick()
    {
        var down = new INPUT { type = INPUT_MOUSE, u = new InputUnion { mi = new MOUSEINPUT { dx = 0, dy = 0, mouseData = 0, dwFlags = MOUSEEVENTF_LEFTDOWN, time = 0, dwExtraInfo = IntPtr.Zero } } };
        var up = new INPUT { type = INPUT_MOUSE, u = new InputUnion { mi = new MOUSEINPUT { dx = 0, dy = 0, mouseData = 0, dwFlags = MOUSEEVENTF_LEFTUP, time = 0, dwExtraInfo = IntPtr.Zero } } };
        uint r1 = SendInput(1, new[] { down }, Marshal.SizeOf<INPUT>());
        Thread.Sleep(80);
        uint r2 = SendInput(1, new[] { up }, Marshal.SizeOf<INPUT>());
        return r1 + r2 == 2 ? 2u : 0u;
    }

    public static int InputSize() => Marshal.SizeOf<INPUT>();

    private void SetKey(ushort vk, bool wantDown)
    {
        bool isDown = _down.Contains(vk);
        if (wantDown == isDown) return;
        if (DryRun) { if (wantDown) _down.Add(vk); else _down.Remove(vk); return; }

        uint r = SendKeyRaw(vk, wantDown);
        int err = Marshal.GetLastWin32Error();
        if (Verbose) Console.WriteLine($"[SendInput] size={Marshal.SizeOf<INPUT>()} vk=0x{vk:X2} {(wantDown ? "DOWN" : "UP")} ret={r} err={err}");
        if (r == 1) { if (wantDown) _down.Add(vk); else _down.Remove(vk); }
    }

    public void TestHoldD(int ms = 3000)
    {
        Console.WriteLine($"[Test] INPUT size={InputSize()} (debe ser 40 en x64). HOLD D {ms}ms...");
        SetKey(VK_D, true);
        Thread.Sleep(ms);
        SetKey(VK_D, false);
        Console.WriteLine("[Test] release D");
    }

    // Selecciona slot hotbar 0-9 (teclas 1..9,0) con pulso. Devuelve true si se envio.
    public bool SelectSlot(int slot)
    {
        if (slot < 0 || slot > 9) return false;
        ushort vk = VkDigit(slot);
        if (DryRun) { if (Verbose) Console.WriteLine($"[Slot DryRun] {slot + 1}"); return true; }
        uint r1 = SendKeyRaw(vk, true); Thread.Sleep(50);
        uint r2 = SendKeyRaw(vk, false); Thread.Sleep(120);
        if (Verbose) Console.WriteLine($"[Slot] {slot + 1} ret={r1 + r2}/2");
        return r1 + r2 == 2;
    }

    // Click para invocar minion / pegar latigazo. Requiere Terraria con foco.
    public bool LeftClick()
    {
        if (DryRun) { if (Verbose) Console.WriteLine("[Click DryRun]"); return true; }
        uint r = SendMouseClick();
        if (Verbose) Console.WriteLine($"[Click] ret={r}/2");
        return r == 2;
    }

    public void Execute(GameAction a)
    {
        SetKey(VK_A, a.MoveX < -0.2f);
        SetKey(VK_D, a.MoveX > 0.2f);
        SetKey(VK_SPACE, a.Jump || a.Fly);
        if (Verbose && DryRun)
            Console.WriteLine($"[Input] move={a.MoveX:F1} jump={a.Jump} fly={a.Fly} atk={a.Attack} heal={a.UseHealPotion}");

        if (!DryRun && a.UseHealPotion)
        {
            SetKey(VK_H, true);
            Thread.Sleep(60);
            SetKey(VK_H, false);
        }
    }

    public void ReleaseAll()
    {
        foreach (var vk in _down.ToArray()) SetKey(vk, false);
        _down.Clear();
    }
}
