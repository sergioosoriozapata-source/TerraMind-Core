# Diagramas

## Arquitectura
```
Terraria.exe -> MemoryReader(ReadOnly) -> VanillaMemoryProvider -> PlayerState/BossState/ProjectileState -> AdaptiveBrain -> GameAction -> InputActuator(SendInput)
                                                                         ^ mismo contrato ^                                                          |
mods/TerraMind.TModPort (Main.LocalPlayer directo) -------------------------+--------------------------------------------------------------------------> Player.control* (Fase 5)
```

## FSM
```
        +----------------+
        |    OFENSIVO    |<---------------------+
        +-------+--------+                      |
  Life<40% |   | dash/proyectil                 |
          v   v                                 |
    +-----+---+ +-------------+  mana ok, vida ok
    | CURACION | | ESQUIVA_AEREA(alas, reserva 25%) |
    +-----+---+ +-------------+                     ^
          |             ^                           |
  potion ok|           | ai==dash                    |
           v           |                             |
        +--+-----------+-+                           |
        |   DEFENSIVO(kite)  +----------------------+
        +------------------+
```
Tick 16ms: Curacion > Esquiva > Mago sin mana > Kite > Ofensivo por rango de clase.
