using System;
using System.Collections.Generic;

namespace UnholyTrinity.Helpers
{
    public static class TurnManager
    {
        public static event Action<TurnPhase> NewPhaseEvent;

        public static TurnPhase CurrentPhase => _turnPhase.Peek();

        private static readonly Queue<TurnPhase> _turnPhase = new(new List<TurnPhase>
        {
            TurnPhase.PlayerOne,
            TurnPhase.PlayerTwo,
            TurnPhase.Resolution,
        });

        public static void EndTurn()
        {
            TurnPhase previous = _turnPhase.Dequeue();
            _turnPhase.Enqueue(previous);
            NewPhaseEvent?.Invoke(CurrentPhase);
        }

        public static void Reset()
        {
            _turnPhase.Clear();
            _turnPhase.Enqueue(TurnPhase.PlayerOne);
            _turnPhase.Enqueue(TurnPhase.PlayerTwo);
            _turnPhase.Enqueue(TurnPhase.Resolution);
        }

        public static void StartGame()
        {
            Reset();
            NewPhaseEvent?.Invoke(CurrentPhase);
        }
    }
}
