using System;
using System.Collections.Generic;
using UnholyTrinity.Players;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static event Action<Player> TurnChangedEvent;
    
    private readonly Stack<Player> _playerStack = new();
    
    public void ChangeTurn()
    {
        //Cycle the next player to the top of the stack
        Player nextPlayer = _playerStack.Pop();
        _playerStack.Push(nextPlayer);
        
        TurnChangedEvent?.Invoke(nextPlayer);
    }
    
}
