using System;

namespace UnholyTrinity.Helpers
{
    [Serializable]
    public static class TickData
    {
        public static event Action<SAMType> Tick;
        public static event Action<SAMType> Tock;

        public static int CurrentTick { get; private set; }

        public static int TicksPerTurn = 3;

        public static float TickDuration = 2;

        public static void SetCurrentTick(int tick)
        {
            CurrentTick = tick;
        }

        public static void CallTick(SAMType tickType)
        {
            Tick?.Invoke(tickType);
        }

        public static void CallTock(SAMType tickType)
        {
            Tock?.Invoke(tickType);
        }
    }
}
