using System;

namespace UnholyTrinity.Helpers
{
    public enum SamType
    {
        Shoot,
        Aim,
        Move
    }
    [Serializable]
    public static class TickData
    {
        public static event Action<SamType> Tick;
        public static event Action<SamType> Tock;

        public static int CurrentTick { get; private set; }

        public static int TicksPerTurn = 3;

        public static float TickDuration = 2;

        public static void SetCurrentTick(int tick)
        {
            CurrentTick = tick;
        }

        public static void CallTick(SamType tickType)
        {
            Tick?.Invoke(tickType);
        }

        public static void CallTock(SamType tickType)
        {
            Tock?.Invoke(tickType);
        }
    }
}
