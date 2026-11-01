namespace UnholyTrinity.Simulation
{
    /// <summary>One tick of a plan preview: what happened, and the unit as it ends the tick.</summary>
    public readonly struct PreviewTick
    {
        public readonly TickLog Log;
        public readonly UnitState Unit;

        public PreviewTick(TickLog log, UnitState unit)
        {
            Log = log;
            Unit = unit;
        }
    }
}
