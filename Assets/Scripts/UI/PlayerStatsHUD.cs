using System.Collections.Generic;
using TMPro;
using UnholyTrinity.Managers;
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
        Player.PlanForecastChangedEvent += OnHealthChanged;
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
        Player.PlanForecastChangedEvent -= OnHealthChanged;
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
        Player.PlanForecast forecast = player.CurrentForecast;

        // while planning, the change the plan is expected to make by its last tick, e.g. "(+25)"
        string healthChange = forecast != null ? FormatChange(forecast.Health - player.CurrentHealth) : string.Empty;
        string armourChange = forecast != null ? FormatChange(forecast.Armour - player.CurrentArmour) : string.Empty;

        text.text = $"{player.name.ToUpperInvariant()}{System.Environment.NewLine}" +
                    $"HP {player.CurrentHealth}/{player.MaxHealth}{healthChange}{System.Environment.NewLine}" +
                    $"AR {player.CurrentArmour}/{player.MaxArmour}{armourChange}";
    }

    // " (+25)" or " (-10)", and nothing when there's no change
    private static string FormatChange(int change)
    {
        return change == 0 ? string.Empty : $" ({change:+0;-0})";
    }
}
