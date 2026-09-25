using System.Runtime.InteropServices;

namespace TerraMind.Memory;

// Solo lectura. Nunca WRITE a Terraria.exe (seguridad local).
// Sin herramientas externas: el propio bot escanea y aprende.
public sealed class MemoryReader : IDisposable
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const int PROCESS_VM_READ = 0x0010;
    private const int PROCESS_QUERY_INFORMATION = 0x0400;

    private IntPtr _handle = IntPtr.Zero;
    public IntPtr Handle => _handle;
    public bool IsOpen => _handle != IntPtr.Zero;

    public bool Open(int pid)
    {
        Dispose();
        _handle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, pid);
        return _handle != IntPtr.Zero;
    }

    public bool TryReadBytes(IntPtr address, int size, out byte[] data)
    {
        data = new byte[size];
        if (_handle == IntPtr.Zero) return false;
        try
        {
            if (!ReadProcessMemory(_handle, address, data, size, out var read) || read != size)
                return false;
            return true;
        }
        catch { return false; }
    }

    public bool TryReadInt32(IntPtr address, out int value)
    {
        value = 0;
        if (!TryReadBytes(address, 4, out var buf)) return false;
        value = BitConverter.ToInt32(buf, 0);
        return true;
    }

    public bool TryReadFloat(IntPtr address, out float value)
    {
        value = 0;
        if (!TryReadBytes(address, 4, out var buf)) return false;
        value = BitConverter.ToSingle(buf, 0);
        return true;
    }

    public bool TryReadPointer(IntPtr address, out IntPtr value)
    {
        value = IntPtr.Zero;
        if (!TryReadBytes(address, IntPtr.Size, out var buf)) return false;
        value = IntPtr.Size == 8
            ? (IntPtr)BitConverter.ToInt64(buf, 0)
            : (IntPtr)BitConverter.ToInt32(buf, 0);
        return true;
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
    }
}
