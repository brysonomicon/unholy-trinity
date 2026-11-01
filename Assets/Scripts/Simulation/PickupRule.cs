using UnholyTrinity.Pickups;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Respawns and collects pickups. Respawns happen at tick start, before anyone moves, so a pickup that comes
    /// back this tick can be collected this tick, even by a unit standing on it: with RespawnTicks = 1, a pickup
    /// collected on tick N is available again on tick N + 1.
    /// </summary>
    public class PickupRule : ITileRule
    {
        public void OnTickStarted(SimState state, TickLog log)
        {
            if (state.Pickups == null)
                return;

            foreach (var coord in state.Pickups.AdvanceTicks(1))
                log.Add(TickEventType.PickupRespawned, TickEvent.NoUnit, coord, state.Pickups.Spawns[coord].Definition);
        }

        public void OnEnter(SimState state, UnitState unit, UnitMove move, TickLog log) =>
            TryCollect(state, unit, PickupTrigger.OnEnter, log);

        public void OnStop(SimState state, UnitState unit, TickLog log) =>
            TryCollect(state, unit, PickupTrigger.OnStop, log);

        static void TryCollect(SimState state, UnitState unit, PickupTrigger reached, TickLog log)
        {
            if (state.Pickups != null && state.Pickups.TryCollect(unit.Coord, reached, unit))
                log.Add(TickEventType.PickupCollected, unit.Id, unit.Coord, state.Pickups.Spawns[unit.Coord].Definition);
        }
    }
}
