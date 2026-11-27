using System;
using System.Collections.Generic;

namespace UnholyTrinity.Helpers
{
public static class TurnManager
{
    public static int Round { get; private set; }
    public static event Action<TurnPhase> NewPhaseEvent;
    public static event Action<int> NewRoundEvent;

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
        UpdateRound();
        NewPhaseEvent?.Invoke(CurrentPhase);
    }

    public static void Reset()
    {
        Phase.Clear();
        Phase.Enqueue(TurnPhase.PlayerOne);
        Phase.Enqueue(TurnPhase.PlayerTwo);
        Phase.Enqueue(TurnPhase.Resolution);
        Round = 0;
    }

    public static void StartGame()
    {
        Reset();
        UpdateRound();
        NewPhaseEvent?.Invoke(CurrentPhase);
    }

    private static void UpdateRound()
    {
        if (CurrentPhase != TurnPhase.PlayerOne)
        {
            return;
        }
        Round++;
        NewRoundEvent?.Invoke(Round);
    }
}
}