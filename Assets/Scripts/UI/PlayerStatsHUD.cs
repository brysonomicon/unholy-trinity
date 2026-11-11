using System.Collections.Generic;
using TMPro;
using UnholyTrinity.Players;
using UnityEngine;

// shows each player's health and armour.
public class PlayerStatsHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text playerOneText;
    [SerializeField] private TMP_Text playerTwoText;

    private void Awake()
    {
        Player.HealthChangedEvent += OnHealthChanged;
        // fires once every player has registered, so it also fills in the starting values
        GameManager.NewTurnEvent += OnNewTurn;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        Player.HealthChangedEvent -= OnHealthChanged;
        GameManager.NewTurnEvent -= OnNewTurn;
    }

    private void OnHealthChanged(Player _) => Refresh();

    private void OnNewTurn(Player _) => Refresh();

    private void Refresh()
    {
        IReadOnlyList<Player> players = GameManager.Instance != null ? GameManager.Instance.Players : null;
        SetText(playerOneText, players, 0);
        SetText(playerTwoText, players, 1);
    }

    private static void SetText(TMP_Text text, IReadOnlyList<Player> players, int index)
    {
        if (text == null)
        {
            return;
        }

        if (players == null || index >= players.Count || players[index] == null)
        {
            text.text = string.Empty;
            return;
        }

        Player player = players[index];
        text.text = $"{player.name.ToUpperInvariant()}\n" +
                    $"HP {player.CurrentHealth}/{player.MaxHealth}   AR {player.CurrentArmour}/{player.MaxArmour}";
    }
}
