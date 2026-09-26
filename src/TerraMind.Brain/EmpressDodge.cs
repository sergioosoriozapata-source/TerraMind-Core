using TerraMind.State;

namespace TerraMind.Brain;

// Esquiva dedicada a la Emperatriz de la Luz (diurna = furiosa).
// Fuente: wiki oficial (AI_120_HallowBoss): Bolts1, Dash, SunDance, Rainbow, Lance1 (fase 1);
// fase 2 (<50%): Lance2, Bolts1, Dash, (+Lance3 si furia), Rainbow, Bolts1, Sun, Lance1, Dash, Bolts2.
// ai[0] verificado: Lance1=4, Lance2=7, Lance3=11. Dash/Sun/Rainbow se detectan por geometria.
// De DIA todo es one-shot e ignora defensas Y esquivas (1.4.4+): tolerancia cero,
// sin latigazos (rango corto = muerte), DPS solo de subditos. Ventanas de ataque solo de noche.
public enum EmpressPattern
{
    Unknown, Bolts, DashWindup, DashActive, SunDance, Rainbow,
    LanceRing, LanceWaves, PhaseTransition
}

public sealed record EmpressDodgeDecision(
    float MoveX, bool Fly, bool Attack, string Reason,
    EmpressPattern Pattern, bool Enraged, bool Phase2);

public sealed class EmpressDodge
{
    private float _lastDir = 1f;

    public static bool IsEmpress(string bossName) =>
        bossName.Contains("Empress", StringComparison.OrdinalIgnoreCase);

    public EmpressDodgeDecision Decide(PlayerState p, BossState boss, IReadOnlyList<ProjectileState> projs, bool isDay)
    {
        bool enraged = isDay; // de dia siempre furiosa
        bool phase2 = boss.MaxLife > 0 && boss.Life < boss.MaxLife / 2;
        float dx = boss.PositionX - p.PositionX;
        float dy = boss.PositionY - p.PositionY;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float bvx = boss.VelocityX, bvy = boss.VelocityY;
        float bspeed = MathF.Sqrt(bvx * bvx + bvy * bvy);
        bool towards = (bvx * -dx + bvy * -dy) > 0; // velocidad hacia el jugador?

        var near = projs.Where(pr => pr.Active && pr.Hostile).ToArray();
        float ai0 = boss.Ai.Length > 0 ? boss.Ai[0] : -1;

        // 1. DASH ACTIVO: viene rapido hacia ti en horizontal -> aparta en vertical.
        if (bspeed > 15 && towards && Math.Abs(bvx) > Math.Abs(bvy) * 1.5f)
        {
            float up = dy > 0 ? -1f : 1f; // ella arriba => baja tu; ella abajo => sube
            return D(0, true, false, $"DASH encima v={bspeed:F0} -> vertical {(up > 0 ? "sube" : "baja")}", EmpressPattern.DashActive, enraged, phase2);
        }
        // 2. LANCE RING (ai 4/11 o anillo lento): >=5 espadas lentas rodeando -> UNA direccion, sin girar.
        var slow = near.Where(pr => pr.Speed < 4 && Dist(pr, p) is >= 150 and <= 700).ToArray();
        if (ai0 is 4 or 11 || (slow.Length >= 5 && AngularCover(slow, p) > 180))
        {
            float dir = Math.Abs(p.VelocityX) > 1f ? Math.Sign(p.VelocityX) : _lastDir;
            _lastDir = dir;
            return D(dir, false, AttackOk(enraged, false), $"anillo de lanzas ({slow.Length}) -> recto {(dir > 0 ? "derecha" : "izquierda")} sin girar", EmpressPattern.LanceRing, enraged, phase2);
        }
        // 4. LANCE WAVES (ai 7, fase 2): oleadas rapidas paralelas -> perpendicular sostenido.
        var fast = near.Where(pr => pr.Speed > 9).ToArray();
        if (ai0 == 7 || (fast.Length >= 6 && Aligned(fast)))
        {
            // oleadas horizontales => deriva vertical; si verticales => corre horizontal
            bool horiz = fast.Average(pr => Math.Abs(pr.VelocityX)) > fast.Average(pr => Math.Abs(pr.VelocityY));
            float dir = horiz ? 0 : (p.PositionX < boss.PositionX ? -1f : 1f);
            return D(dir, horiz, AttackOk(enraged, false), $"oleadas ({fast.Length}) -> perpendicular", EmpressPattern.LanceWaves, enraged, phase2);
        }
        // 5. Lanzas YA lanzadas (rapidas hacia ti): sigue recto y luego arquea.
        var launched = near.Where(pr => pr.Speed > 8 && Closing(pr, p)).ToArray();
        if (launched.Length >= 3)
            return D(_lastDir, true, AttackOk(enraged, false), $"lanzas encima ({launched.Length}) -> arquea", EmpressPattern.LanceRing, enraged, phase2);
        // 6. SUN DANCE: ella arriba-izquierda + rayos girando cerca de ella -> cae FUERA de rango.
        bool aboveLeft = dx is >= -260 and <= 60 && dy is >= -420 and <= -100;
        var rays = near.Where(pr => DistTo(pr, boss.PositionX, boss.PositionY) < 320 && pr.Speed is >= 3 and <= 14).ToArray();
        if (aboveLeft && rays.Length >= 3)
            return D(dx > 0 ? -0.6f : 0.6f, false, false, $"sun dance ({rays.Length} rayos) -> cae y sale (tiene 57% DR, no pegues)", EmpressPattern.SunDance, enraged, phase2);
        // 7. RAINBOW: anillo de estrellas alrededor de un centro + rastros -> distancia, JAMAS retroceder.
        var ring = near.Where(pr => pr.Speed < 6).ToArray();
        if (ring.Length >= 6)
        {
            float cx = ring.Average(pr => pr.PositionX), cy = ring.Average(pr => pr.PositionY);
            float dir = p.PositionX < cx ? -1f : 1f;
            if (Math.Abs(p.VelocityX) > 2f) dir = Math.Sign(p.VelocityX); // no retroceder nunca
            _lastDir = dir;
            return D(dir, true, AttackOk(enraged, true), $"arcoiris ({ring.Length}) -> lejos sin retroceder", EmpressPattern.Rainbow, enraged, phase2);
        }
        // 7. DASH WINDUP (solo si no hay patrones activos): quieta a un lado -> pre-sube/baja ya.
        // (Detras de lanzas/rayos/arcoiris: esos mandan. El windup real no tiene proyectiles alrededor.)
        if (bspeed < 3 && Math.Abs(dx) > 150)
        {
            _lastDir = Math.Sign(dx) != 0 ? Math.Sign(dx) : _lastDir;
            return D(0, true, false, "windup dash (quieta al lado) -> gana altura", EmpressPattern.DashWindup, enraged, phase2);
        }
        // 8. BOLTS: convergen hacia ti -> circulo amplio + quiebro al acercarse.
        var homing = near.Where(pr => Closing(pr, p)).OrderBy(pr => Dist(pr, p)).ToArray();
        if (homing.Length > 0 && Dist(homing[0], p) < 220)
            return D(-_lastDir, true, AttackOk(enraged, true), $"rayos encima ({Dist(homing[0], p):F0}px) -> QUIEBRO", EmpressPattern.Bolts, enraged, phase2);
        if (homing.Length > 0)
        {
            _lastDir = Math.Abs(p.VelocityX) > 1f ? Math.Sign(p.VelocityX) : _lastDir;
            return D(_lastDir, true, AttackOk(enraged, true), $"rayos ({homing.Length}) -> circulo amplio", EmpressPattern.Bolts, enraged, phase2);
        }
        // 9. TRANSICION FASE 2: desaparecio/teleport lejos de golpe -> busca espacio (lejos de ella).
        if (dist > 900)
            return D(p.PositionX < boss.PositionX ? -1f : 1f, true, false, "reposicion/teleport -> espacio abierto", EmpressPattern.PhaseTransition, enraged, phase2);
        // 10. Por defecto: orbita a ~350px.
        float orbit = dx > 0 ? -0.7f : 0.7f;
        _lastDir = orbit;
        return D(orbit, dy > 0, AttackOk(enraged, true), $"orbita d={dist:F0}", EmpressPattern.Unknown, enraged, phase2);
    }

    // De dia: NUNCA atacar con armas (one-shot a <300px con latigo). Solo subditos.
    // De noche: ataca en bolts/rainbow/desconocido; no durante dash(windup=invulnerable) ni sun(DR).
    private static bool AttackOk(bool enraged, bool window) => !enraged && window;

    private EmpressDodgeDecision D(float mx, bool fly, bool atk, string r, EmpressPattern pat, bool enr, bool p2) =>
        new(mx, fly, atk, (enr ? "[FURIA-DIA one-shot] " : "") + r, pat, enr, p2);

    private static float Dist(ProjectileState pr, PlayerState p)
    {
        float dx = pr.PositionX - p.PositionX, dy = pr.PositionY - p.PositionY;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    private static float DistTo(ProjectileState pr, float x, float y)
    {
        float dx = pr.PositionX - x, dy = pr.PositionY - y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    private static bool Closing(ProjectileState pr, PlayerState p)
    {
        float dx = p.PositionX - pr.PositionX, dy = p.PositionY - pr.PositionY;
        float d = MathF.Sqrt(dx * dx + dy * dy);
        if (d < 1) return true;
        float dot = (pr.VelocityX * dx + pr.VelocityY * dy) / (pr.Speed * d);
        return dot > 0.5f;
    }
    private static float AngularCover(ProjectileState[] arr, PlayerState p)
    {
        if (arr.Length < 2) return 0;
        var angs = arr.Select(pr => MathF.Atan2(pr.PositionY - p.PositionY, pr.PositionX - p.PositionX)).OrderBy(a => a).ToArray();
        float maxGap = 0;
        for (int i = 0; i < angs.Length; i++)
        {
            float a = angs[i], b = angs[(i + 1) % angs.Length];
            float gap = b - a; if (gap < 0) gap += MathF.PI * 2;
            if (gap > maxGap) maxGap = gap;
        }
        return 360 - maxGap * 180 / MathF.PI;
    }
    private static bool Aligned(ProjectileState[] arr)
    {
        if (arr.Length < 4) return false;
        float mx = arr.Average(pr => Math.Abs(pr.VelocityX)), my = arr.Average(pr => Math.Abs(pr.VelocityY));
        if (mx < 1 && my < 1) return false;
        // paralelos si una componente domina 3:1 en promedio
        return mx > my * 3 || my > mx * 3;
    }
}
