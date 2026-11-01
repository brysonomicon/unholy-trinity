namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// A map rule that acts on units during the move phase: pickups now, health pools and boosts later.
    /// Rules run in a fixed order and only change the <see cref="SimState"/> they're given, which is what lets the
    /// same rules drive previews, resolution and replays.
    /// </summary>
    public interface ITileRule
    {
        /// <summary>Runs once at the start of the move phase, before anyone moves.</summary>
        void OnTickStarted(SimState state, TickLog log);

        /// <summary>Runs each time <paramref name="unit"/> enters a hex. May change <paramref name="move"/>, e.g. to turn or stop the unit.</summary>
        void OnEnter(SimState state, UnitState unit, UnitMove move, TickLog log);

        /// <summary>Runs for every living unit on the hex it ends movement on, including units that didn't move.</summary>
        void OnStop(SimState state, UnitState unit, TickLog log);
    }
}
