using System;
using UnholyTrinity.Helpers;
using UnholyTrinity.Players;
using UnholyTrinity.Hex;
using UnholyTrinity.Managers;
using UnholyTrinity.Weapons;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject cardSection;
    [SerializeField] private LaneCard laneCardPrefab;
    [SerializeField] private RectTransform weaponColumnPrefab;
    [SerializeField] private WeaponButton weaponButtonPrefab;

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
        GenerateActionCards(player);
    }

    private void OnNewPhase(TurnPhase newPhase)
    {
        ClearCardSection();
        if (newPhase == TurnPhase.Resolution)
        {
            return;
        }
        GenerateActionCards(GameManager.Instance.CurrentPlayer);
    }

    private void GenerateActionCards(Player current)
    {
        Weapon weapon = current.CurrentWeapon;

        RectTransform weaponColumn = Instantiate(weaponColumnPrefab, cardSection.transform);

        foreach (Weapon heldWeapon in current.Weapons)
        {
            Instantiate(weaponButtonPrefab, weaponColumn).Setup(
                () => PlanAndRefresh(() => current.SelectWeapon(heldWeapon)),
                heldWeapon.Data.Icon,
                heldWeapon == current.CurrentWeapon
            );
        }

        CreateLaneCard("Shoot")
            .AddOption($"{current.GetUnplannedAmmo(weapon)}/{weapon.Data.MaxAmmo}",
                () => PlanAndRefresh(() => current.PlanShot(weapon)),
                current.CanPlanShot(weapon),
                weapon.Data.Icon,
                current.IsPlanningShot(weapon))
            .AddRow()
            .AddOption("Hold", () => PlanAndRefresh(current.PlanHoldFire),
                current.PlanningStep == SAMType.Shoot,
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
                current.PlanningStep == SAMType.Aim,
                selected: current.IsHoldingAim);

        LaneCard moveCard = CreateLaneCard("Move");
        AddMoveOption(moveCard, "NW", HexDirection.D2);
        AddMoveOption(moveCard, "NE", HexDirection.D1);
        moveCard.AddRow();
        AddMoveOption(moveCard, "W", HexDirection.D3);
        AddMoveOption(moveCard, "Brake", null);
        AddMoveOption(moveCard, "E", HexDirection.D0);
        moveCard.AddRow();
        AddMoveOption(moveCard, "SW", HexDirection.D4);
        AddMoveOption(moveCard, "SE", HexDirection.D5);

        CreateLaneCard($"Tick {current.PlanningTickOfTurn}/{TurnManager.Instance.TicksPerTurn}")
            .AddOption("Next Tick", () => PlanAndRefresh(current.AdvancePlanningStep), current.CanAdvancePlanningStep)
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
            GenerateActionCards(current);
        }

        void AddMoveOption(LaneCard card, string label, HexDirection? direction)
        {
            card.AddOption(
                label, 
                () => PlanAndRefresh(() => current.PlanMove(direction)),
                current.PlanningStep == SAMType.Move,
                selected: current.IsPlanningMove(direction));
        }
    }

    // lets tile clicks rebuild cards
    public void RefreshCards()
    {
        ClearCardSection();
        GenerateActionCards(GameManager.Instance.CurrentPlayer);
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