namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// Something that happened during a simulated tick. Views (playback, preview drawing, effects) read the log
    /// instead of re-deriving what the simulation decided. Logs are never saved: a replay stores the initial state
    /// and the UnholyActions, and gets the logs back by simulating them again.
    ///
    /// Each kind of event is its own class carrying only its own data (see TickEvents.cs); views match on the type,
    /// e.g. <c>case MovedEvent moved:</c>.
    /// </summary>
    public abstract class TickEvent
    {
        /// <summary><see cref="UnitId"/> for events not caused by a unit, like a pickup respawning.</summary>
        public const int NoUnit = -1;

        /// <summary>0 at tick start, 1..n for each movement step, n + 1 for where units end up. Set by <see cref="TickLog.Add"/>.</summary>
        public int Step { get; internal set; }
        public int UnitId { get; }

        protected TickEvent(int unitId)
        {
            UnitId = unitId;
        }

        public override string ToString() => $"[{Step}] {Describe()}";

        protected abstract string Describe();
    }
}
