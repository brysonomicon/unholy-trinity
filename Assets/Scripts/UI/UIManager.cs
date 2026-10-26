using System;
using UnholyTrinity.Players;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject cardSection;
    [SerializeField] private LaneCard laneCardPrefab;

    private void Awake()
    {
        GameManager.NewTurnEvent += OnNextTurn;
        TurnManager.NewPhaseEvent += OnNewPhase;
    }

    private void OnDestroy()
    {
        GameManager.NewTurnEvent -= OnNextTurn;
        TurnManager.NewPhaseEvent -= OnNewPhase;
    }

    private void OnNextTurn(Player player)
    {
        GenerateDefaultCards(player);
    }

    private void OnNewPhase(TurnPhase newPhase)
    {
        ClearCardSection();
        if (newPhase == TurnPhase.Resolution)
        {
            return;
        }
        GenerateDefaultCards(GameManager.Instance.CurrentPlayer);
    }

    private void GenerateDefaultCards(Player current)
    {
        Weapon weapon = current.CurrentWeapon;

        CreateLaneCard("Shoot")
            .AddOption($"{current.GetUnplannedAmmo(weapon)}/{weapon.Data.MaxAmmo}",
                () => PlanAndRefresh(() => current.PlanShot(weapon)),
                current.CanPlanShot(weapon),
                weapon.Data.Icon,
                current.IsPlanningShot(weapon))
            .AddRow()
            .AddOption("Hold", () => PlanAndRefresh(current.PlanHoldFire),
                selected: current.IsHoldingFire);

        CreateLaneCard("Aim")
            .AddOption("CCW", () => PlanAndRefresh(() => current.PlanRotation(false)),
                current.CanPlanRotation(false),
                selected: current.IsPlanningRotation(false))
            .AddOption("CW", () => PlanAndRefresh(() => current.PlanRotation(true)),
                current.CanPlanRotation(true),
                selected: current.IsPlanningRotation(true))
            .AddRow()
            .AddOption("Hold", () => PlanAndRefresh(current.PlanHoldAim),
                selected: current.IsHoldingAim);

        LaneCard moveCard = CreateLaneCard("Move");
        AddMoveOption(moveCard, "NW", HexDirection.D2);
        AddMoveOption(moveCard, "NE", HexDirection.D1);
        moveCard.AddRow();
        AddMoveOption(moveCard, "W", HexDirection.D3);
        moveCard.AddOption("Hold", () => PlanAndRefresh(current.PlanHoldMove),
            selected: current.IsHoldingMove);
        AddMoveOption(moveCard, "E", HexDirection.D0);
        moveCard.AddRow();
        AddMoveOption(moveCard, "SW", HexDirection.D4);
        AddMoveOption(moveCard, "SE", HexDirection.D5);

        CreateLaneCard($"Tick {current.PlanningTickOfTurn}/{TurnManager.Instance.TicksPerTurn}")
            .AddOption("Next Tick", () => PlanAndRefresh(current.PlanNextTick), current.CanPlanNextTick)
            .AddRow()
            .AddOption("Clear Tick", () => PlanAndRefresh(current.ClearTick))
            .AddRow()
            .AddOption("End Turn", () => TurnManager.Instance.EndTurn());

        return;

        // does a planning action and then rebuilds every card so they reflect new plan
        void PlanAndRefresh(Action planAction)
        {
            planAction();
            ClearCardSection();
            GenerateDefaultCards(current);
        }

        void AddMoveOption(LaneCard card, string label, HexDirection direction)
        {
            card.AddOption(label, () => PlanAndRefresh(() => current.PlanMove(direction)),
                selected: current.IsPlanningMove(direction));
        }
    }

    private LaneCard CreateLaneCard(string title)
    {
        return Instantiate(laneCardPrefab, cardSection.transform).Setup(title);
    }

    private void ClearCardSection()
    {
        //? [CB] I wonder if it'd be worth caching and enabling/disabling cards
        foreach (Transform child in cardSection.transform)
        {
            Destroy(child.gameObject);
        }
    }
}