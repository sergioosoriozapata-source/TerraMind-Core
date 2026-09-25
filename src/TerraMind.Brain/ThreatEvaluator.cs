using TerraMind.State;

namespace TerraMind.Brain;

// Evalua amenaza de jefes + proyectiles (Fase 3).
// Reglas Vanilla: dash si velocidad hacia player > MaxRunSpeed,
// proyectil rapido <150px = amenaza alta.
public static class ThreatEvaluator
{
    public static (bool DashIncoming, int CloseProjectiles, float NearestBossDist) Evaluate(
        PlayerState p,
        IReadOnlyList<BossState> bosses,
        IReadOnlyList<ProjectileState> projs)
    {
        bool dash = false;
        int close = 0;
        float nearest = float.MaxValue;

        foreach (var b in bosses)
        {
            if (!b.Active) continue;
            float dx = b.PositionX - p.PositionX;
            float dy = b.PositionY - p.PositionY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < nearest) nearest = dist;

            // Heuristica generica + caso EoC ai[0]==3.
            bool towardsPlayer = (b.VelocityX * dx + b.VelocityY * dy) > 0;
            if (towardsPlayer && b.Speed > MathF.Max(p.MaxRunSpeed, 6f) && dist < 400)
                dash = true;
            if (b.Ai.Length > 0 && Math.Abs(b.Ai[0] - 3f) < 0.01f && dist < 500)
                dash = true;
        }

        foreach (var pr in projs)
        {
            if (!pr.Active || !pr.Hostile) continue;
            float dx = pr.PositionX - p.PositionX;
            float dy = pr.PositionY - p.PositionY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 150) close++;
        }

        return (dash, close, nearest);
    }

    // Tiempo hasta impacto (para esquiva anticipada).
    public static float TimeToImpact(float distance, float projSpeed)
        => projSpeed <= 0.01f ? float.MaxValue : distance / projSpeed;
}
