using TerraMind.State;

namespace TerraMind.Brain;

// FSM adaptativa (Fase 4). Tick 16ms.
// Prioridad: Curacion > Esquiva > Defensivo(Mago sin mana) > Ofensivo.
public sealed class AdaptiveBrain
{
    public BrainState Current { get; private set; } = BrainState.Ofensivo;

    public BrainDecision Tick(PlayerState p, IReadOnlyList<BossState> bosses, IReadOnlyList<ProjectileState> projs, ClassProfile cls)
    {
        var (dashIncoming, closeProjs, nearestDist) = ThreatEvaluator.Evaluate(p, bosses, projs);

        // 1. Curacion baja vida.
        if (p.LifePct < 0.35f && !p.PotionSickness)
        {
            Current = BrainState.Curacion;
            return new BrainDecision(Current, $"Life {p.LifePct:P0} <35% sin sickness",
                MoveX: 0, Jump: true, Fly: true, UseHealPotion: true, SwitchToSustainedWeapon: false);
        }

        // 2. Esquiva con alas coordinadas. Reserva 25% alas.
        bool highThreat = dashIncoming || closeProjs >= 2;
        if (highThreat)
        {
            Current = BrainState.EsquivaAerea;
            bool canFly = p.WingPct > 0.25f;
            // Vuelo diagonal 45°: Fly=true + MoveX segun jefe mas cercano.
            float dir = 1f;
            if (bosses.Count > 0)
            {
                var b = bosses[0];
                dir = p.PositionX < b.PositionX ? -1f : 1f;
            }
            return new BrainDecision(Current, $"Dash={dashIncoming} Close={closeProjs}",
                MoveX: dir, Jump: true, Fly: canFly, UseHealPotion: false, SwitchToSustainedWeapon: false);
        }

        // 3. Mago sin mana -> defensivo + arma sostenida.
        if (cls.Archetype == Archetype.Mage && p.ManaPct < 0.20f)
        {
            Current = BrainState.Defensivo;
            return new BrainDecision(Current, $"Mage mana {p.ManaPct:P0} <20%",
                MoveX: 0.5f, Jump: false, Fly: false, UseHealPotion: false, SwitchToSustainedWeapon: true);
        }

        // 4. Kite defensivo si vida media y jefe cerca.
        if (p.LifePct < 0.55f && nearestDist < 250)
        {
            Current = BrainState.Defensivo;
            return new BrainDecision(Current, $"Kite Life {p.LifePct:P0} dist {nearestDist:F0}",
                MoveX: -0.7f, Jump: false, Fly: p.WingPct > 0.3f, UseHealPotion: false, SwitchToSustainedWeapon: false);
        }

        // 5. Ofensivo por defecto: mantener rango ideal por clase.
        Current = BrainState.Ofensivo;
        float move = 0f;
        if (nearestDist < cls.IdealRangeMin) move = -0.5f;      // demasiado cerca, abrir
        else if (nearestDist > cls.IdealRangeMax) move = 0.8f;  // acercar
        return new BrainDecision(Current, $"Ofensivo rango [{cls.IdealRangeMin}-{cls.IdealRangeMax}] d={nearestDist:F0}",
            MoveX: move, Jump: false, Fly: false, UseHealPotion: false, SwitchToSustainedWeapon: false);
    }
}
