using System;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public partial class Player
    {
        public static event Action<Player> ScoreChangedEvent;

        public int Score { get; private set; }

        public void AddScore(int amount)
        {
            Score += amount;
            Debug.Log($"{name} score {amount:+0;-0}, now {Score}");
            ScoreChangedEvent?.Invoke(this);
        }
    }
}

