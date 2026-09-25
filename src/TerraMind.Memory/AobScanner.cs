using System.Diagnostics;

namespace TerraMind.Memory;

// Scanner interno. El bot lo ejecuta solo al iniciar, sin Cheat Engine.
// Busca firmas con wildcards "??" dentro de los modulos del proceso Terraria.
public static class AobScanner
{
    public static byte?[] Parse(string signature)
    {
        var parts = signature.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var outp = new byte?[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (p == "??" || p == "?") outp[i] = null;
            else outp[i] = Convert.ToByte(p, 16);
        }
        return outp;
    }

    public static int Find(byte[] buffer, byte?[] pattern)
    {
        if (pattern.Length == 0 || buffer.Length < pattern.Length) return -1;
        for (int i = 0; i <= buffer.Length - pattern.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (pattern[j].HasValue && buffer[i + j] != pattern[j]!.Value) { ok = false; break; }
            }
            if (ok) return i;
        }
        return -1;
    }

    // Escanea los modulos del proceso en bloques de 64KB usando solo ReadProcessMemory.
    // Devuelve true si encuentra la firma. No necesita ninguna herramienta externa.
    public static bool TryFind(Process proc, MemoryReader mem, string signature, out IntPtr found)
    {
        found = IntPtr.Zero;
        var pattern = Parse(signature);
        const int chunk = 64 * 1024;

        try
        {
            foreach (ProcessModule mod in proc.Modules)
            {
                long baseAddr = mod.BaseAddress.ToInt64();
                int size = mod.ModuleMemorySize;
                if (size <= 0 || size > 200_000_000) continue;

                for (long off = 0; off < size; off += chunk)
                {
                    int toRead = (int)Math.Min(chunk + pattern.Length, size - off);
                    var addr = new IntPtr(baseAddr + off);
                    if (!mem.TryReadBytes(addr, toRead, out var buf)) break;
                    int idx = Find(buf, pattern);
                    if (idx >= 0)
                    {
                        found = new IntPtr(baseAddr + off + idx);
                        return true;
                    }
                }
            }
        }
        catch
        {
            return false;
        }
        return false;
    }
}
