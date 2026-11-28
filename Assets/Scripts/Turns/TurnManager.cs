using System;
using System.Collections;
using System.Collections.Generic;
using UnholyTrinity.Helpers;
using UnityEngine;

namespace UnholyTrinity.Turns
{
public enum TurnPhase
{
    PlayerOne,
    PlayerTwo,
    Resolution
}

public static class TurnManager
{
    public static int Round { get; private set; }
    public static event Action TurnEndEvent;
    public static event Action<int> NewRoundEvent;

    public static event Action<TurnPhase> NewPhaseEvent;

    public static event Func<SAMType, IEnumerator> Tick;
    public static event Func<SAMType, IEnumerator> Tock;

    // counts every tick of the game and only resets with ResetGame.
    // cooldowns are stored as the tick they expire on
    public static int CurrentTick { get; private set; }

    public const int TicksPerTurn = 3;

    public const float TickDuration = 2;

    public static TurnPhase CurrentPhase => Phase.Peek();

    private static readonly Queue<TurnPhase> Phase = new(new List<TurnPhase>
    {
        TurnPhase.PlayerOne,
        TurnPhase.PlayerTwo,
        TurnPhase.Resolution,
    });

    public static void EndTurn()
    {
        if (CurrentPhase == TurnPhase.Resolution)
        {
            TurnEndEvent?.Invoke();
        }
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

    public static void SetCurrentTick(int tick)
    {
        CurrentTick = tick;
    }

    public static IEnumerator CallTick(SAMType tickType)
    {
        yield return AwaitCoroutine(tickType, Tick);
    }

    public static IEnumerator CallTock(SAMType tickType)
    {
        yield return AwaitCoroutine(tickType, Tock);
    }

    /// <summary>
    /// Helper function used to ensure all coroutines are completed before we advance.
    /// If, normally, we used Coroutines as an argument type, any function that yielded more than once would be left behind.
    /// Now, we can be certain everything is resolved before we move forward.
    /// </summary>
    /// <param name="tickType">the SAM type of the current events we're running</param>
    /// <param name="eventDelegates">The callback type of the Delegates you're passing in</param>
    /// <returns></returns>
    private static IEnumerator AwaitCoroutine(SAMType tickType, Func<SAMType, IEnumerator> eventDelegates)
    {
        if (eventDelegates == null)
        {
            yield break;
        }

        List<Coroutine> runningRoutines = new();
        //Start coroutines for each of the delegates for the event
        foreach (Delegate del in eventDelegates.GetInvocationList())
        {
            if (del is not Func<SAMType, IEnumerator> listener)
            {
                continue;
            }

            IEnumerator routine = listener.Invoke(tickType);
            if (routine != null)
            {
                runningRoutines.Add(CoroutineRunner.Start(routine));
            }
        }

        //Wait for them all to run
        foreach (Coroutine routine in runningRoutines)
        {
            yield return routine;
        }
    }
}
}