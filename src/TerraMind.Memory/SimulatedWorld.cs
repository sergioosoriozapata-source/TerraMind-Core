using TerraMind.State;

namespace TerraMind.Memory;

// Mundo simulado para calibrar SIN juego y SIN externos.
// El bot "hace intentos" aqui: pulsa mover/volar y mide tiempos/distancias
// con la misma fisica simplificada. Cuando haya attach real, se sustituye
// por lecturas reales pero la logica de intentos es identica.
public sealed class SimulatedWorld
{
    public PlayerState State { get; private set; } = new()
    {
        Life = 400, MaxLife = 400, Mana = 200, MaxMana = 200,
        Defense = 30, WingTime = 180, WingTimeMax = 180,
        MaxRunSpeed = 11.5f, PositionX = 0, PositionY = 0
    };

    private float _vx;

    // Avanza 16ms con la accion pedida. Devuelve el nuevo estado.
    public PlayerState Step(float moveX, bool fly, bool jump)
    {
        var s = State;
        float targetVx = moveX * s.MaxRunSpeed;
        // Aceleracion simple: tarda ~600ms en llegar al max (igual que Terraria).
        _vx += (targetVx - _vx) * 0.08f;
        float newX = s.PositionX + _vx * 0.016f * 60f * 0.18f;
        float newY = s.PositionY;
        int wing = s.WingTime;

        if ((fly || jump) && wing > 0)
        {
            newY -= 2.2f; // subir
            wing--;
        }
        else
        {
            newY += 1.2f; // gravedad
            if (wing < s.WingTimeMax) wing++; // recarga en suelo (simplificado)
        }

        State = s with { PositionX = newX, PositionY = newY, VelocityX = _vx, WingTime = wing };
        return State;
    }

    public void Reset() { _vx = 0; State = State with { PositionX = 0, PositionY = 0, WingTime = State.WingTimeMax }; }
}
