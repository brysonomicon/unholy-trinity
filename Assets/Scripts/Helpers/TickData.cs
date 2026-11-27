using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnholyTrinity.Helpers
{
[Serializable]
public static class TickData
{
    public static event Func<SAMType, IEnumerator> Tick;
    public static event Func<SAMType, IEnumerator> Tock;

    public static int CurrentTick { get; private set; }

    public static int TicksPerTurn = 3;

    public static float TickDuration = 2;

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
    /// <param name="tickType"></param>
    /// <param name="event"></param>
    /// <returns></returns>
    private static IEnumerator AwaitCoroutine(SAMType tickType, Func<SAMType, IEnumerator> @event)
    {
        if (@event == null)
        {
            yield break;
        }

        List<Coroutine> runningRoutines = new();
        foreach (Delegate del in @event.GetInvocationList())
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

        foreach (Coroutine routine in runningRoutines)
        {
            yield return routine;
        }
    }
}
}