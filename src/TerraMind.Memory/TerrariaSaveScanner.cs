namespace TerraMind.Memory;

// Lee lo FIABLE del disco sin cripto: que jugador/mundo estan activos (por fecha),
// tamanos y horas. El .plr moderno va cifrado (se verifica: entropia alta),
// el .wld no (magic "relogic" + version). Los detalles vivos los da ClrMD.
public sealed record SaveInfo(
    string? ActivePlayer, DateTime PlayerDate, long PlayerBytes,
    string? ActiveWorld, DateTime WorldDate, long WorldBytes,
    int WorldVersion, bool SameSession);

public static class TerrariaSaveScanner
{
    public static string SaveRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");

    public static SaveInfo Scan()
    {
        string? player = null; DateTime pd = DateTime.MinValue; long pb = 0;
        string? world = null; DateTime wd = DateTime.MinValue; long wb = 0;
        int wver = 0;
        try
        {
            var pdir = Path.Combine(SaveRoot, "Players");
            if (Directory.Exists(pdir))
            {
                var best = new DirectoryInfo(pdir).GetFiles("*.plr")
                    .OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                if (best is not null) { player = Path.GetFileNameWithoutExtension(best.Name); pd = best.LastWriteTime; pb = best.Length; }
            }
            var wdir = Path.Combine(SaveRoot, "Worlds");
            if (Directory.Exists(wdir))
            {
                var best = new DirectoryInfo(wdir).GetFiles("*.wld")
                    .OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                if (best is not null)
                {
                    world = Path.GetFileNameWithoutExtension(best.Name); wd = best.LastWriteTime; wb = best.Length;
                    try
                    {
                        var hdr = new byte[4];
                        using var fs = File.OpenRead(best.FullName);
                        fs.Read(hdr, 0, 4);
                        wver = BitConverter.ToInt32(hdr, 0);
                    }
                    catch { }
                }
            }
        }
        catch { }
        bool same = player is not null && world is not null && Math.Abs((pd - wd).TotalMinutes) < 10;
        return new SaveInfo(player, pd, pb, world, wd, wb, wver, same);
    }
}
