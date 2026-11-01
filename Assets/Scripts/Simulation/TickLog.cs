using System.Collections.Generic;

namespace UnholyTrinity.Simulation
{
    /// <summary>Everything that happened during one simulated tick, in order.</summary>
    public class TickLog
    {
        readonly List<TickEvent> events = new List<TickEvent>();

        public IReadOnlyList<TickEvent> Events => events;

        /// <summary>The step new events are stamped with; advanced by the simulator, so rules never set it.</summary>
        public int Step { get; internal set; }

        public void Add(TickEvent tickEvent)
        {
            tickEvent.Step = Step;
            events.Add(tickEvent);
        }
    }
}
