using System;
using UnholyTrinity.Players;
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
                weapon.Data.Icon)
            .AddRow()
            .AddOption("Hold", () => PlanAndRefresh(current.PlanHoldFire));

        CreateLaneCard("Aim")
            .AddOption("CCW", () => PlanAndRefresh(() => current.PlanRotation(false)))
            .AddOption("CW", () => PlanAndRefresh(() => current.PlanRotation(true)))
            .AddRow()
            .AddOption("Hold", () => PlanAndRefresh(current.PlanHoldAim));

        // placeholder until movement is in
        CreateLaneCard("Move")
            .AddOption("Move", current.MovePlayer);

        CreateLaneCard($"Tick {current.PlanningTickOfTurn}/{TurnManager.Instance.TicksPerTurn}")
            .AddOption("Next Tick", () => PlanAndRefresh(current.PlanNextTick), current.CanPlanNextTick)
            .AddRow()
            .AddOption("Clear Plan", () => PlanAndRefresh(current.ClearPlan))
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