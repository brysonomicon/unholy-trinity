using System;

namespace UnholyTrinity.Helpers
{
    [Serializable]
    public static class TickData
    {
        public static int CurrentTick { get; private set; }

        public static int TicksPerTurn = 3;

        public static float TickDuration = 2;

        public static void SetCurrentTick(int tick)
        {
            CurrentTick = tick;
        }
    }
}
