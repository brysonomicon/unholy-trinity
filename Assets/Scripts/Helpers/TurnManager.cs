using System;
using System.Collections.Generic;

namespace UnholyTrinity.Helpers
{
public static class TurnManager
{
    public static event Action<TurnPhase> NewPhaseEvent;

    public static TurnPhase CurrentPhase => Phase.Peek();

    private static readonly Queue<TurnPhase> Phase = new(new List<TurnPhase>
    {
        TurnPhase.PlayerOne,
        TurnPhase.PlayerTwo,
        TurnPhase.Resolution,
    });

    public static void EndTurn()
    {
        TurnPhase previous = Phase.Dequeue();
        Phase.Enqueue(previous);
        NewPhaseEvent?.Invoke(CurrentPhase);
    }

    public static void Reset()
    {
        Phase.Clear();
        Phase.Enqueue(TurnPhase.PlayerOne);
        Phase.Enqueue(TurnPhase.PlayerTwo);
        Phase.Enqueue(TurnPhase.Resolution);
    }

    public static void StartGame()
    {
        Reset();
        NewPhaseEvent?.Invoke(CurrentPhase);
    }
}
}