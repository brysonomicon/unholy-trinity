using UnholyTrinity.Hex;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Heal / harm / kill pools. OnEnter tiles fire on every hex entered, including passing straight through;
    /// OnStop tiles fire where a unit ends the tick, including a unit standing in one, so a pool keeps working
    /// every tick you stay in it. A lethal tile kills outright and the simulator ends the move there.
    /// Whether harm goes through armour is set game-wide for <see cref="EnvironmentalDamage.HarmTile"/> in <see cref="SimSettings"/>.
    /// </summary>
    public class HealthTileRule : ITileRule
    {
        public void OnTickStarted(SimState state, TickLog log)
        {
        }

        public void OnEnter(SimState state, UnitState unit, UnitMove move, TickLog log) =>
            Apply(state, unit, HealthTrigger.OnEnter, log);

        public void OnStop(SimState state, UnitState unit, TickLog log) =>
            Apply(state, unit, HealthTrigger.OnStop, log);

        static void Apply(SimState state, UnitState unit, HealthTrigger reached, TickLog log)
        {
            if (!state.Map.TryGetTile(unit.Coord, out var tile)
                || tile.Type != TileType.Health
                || tile.Health.Trigger != reached)
                return;

            var health = tile.Health;
            if (health.Lethal)
            {
                unit.Kill();
                return;
            }

            if (health.Amount > 0)
            {
                int healed = unit.Heal(health.Amount);
                if (healed > 0)
                    log.Add(new HealedEvent(unit.Id, unit.Coord, healed));
            }
            else if (health.Amount < 0)
            {
                int lost = unit.TakeDamage(-health.Amount, state.Settings.IgnoresArmour(EnvironmentalDamage.HarmTile));
                log.Add(new DamagedEvent(unit.Id, unit.Coord, lost));
            }
        }
    }
}
