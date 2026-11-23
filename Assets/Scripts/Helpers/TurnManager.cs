using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnholyTrinity.Helpers
{
    [CreateAssetMenu(fileName = "NewTurnManager",
        menuName = "Managers/TurnManager")]
    public class TurnManager : ScriptableObject
    {
        public static TurnManager Instance { get; private set; }

        public static event Action<TurnPhase> NewPhaseEvent;

        public TurnPhase CurrentPhase => _turnPhase.Peek();

        private readonly Queue<TurnPhase> _turnPhase = new(new List<TurnPhase>
        {
            TurnPhase.PlayerOne,
            TurnPhase.PlayerTwo,
            TurnPhase.Resolution,
        });

        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EndTurn()
        {
            TurnPhase previous = _turnPhase.Dequeue();
            _turnPhase.Enqueue(previous);
            NewPhaseEvent?.Invoke(CurrentPhase);
        }

        public void ResetPhases()
        {
            _turnPhase.Clear();
            _turnPhase.Enqueue(TurnPhase.PlayerOne);
            _turnPhase.Enqueue(TurnPhase.PlayerTwo);
            _turnPhase.Enqueue(TurnPhase.Resolution);
        }
    }
}
