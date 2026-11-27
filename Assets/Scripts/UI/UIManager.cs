using System;
using TMPro;
using UnholyTrinity.Helpers;
using UnholyTrinity.Players;
using UnholyTrinity.Hex;
using UnholyTrinity.Managers;
using UnholyTrinity.Weapons;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject cardSection;
    [SerializeField] private LaneCard laneCardPrefab;
    [SerializeField] private SamGrid samGrid;

    [SerializeField] private Vector2 deckOffset = new Vector2(25f, 25f);

    [SerializeField] private GameObject deckControls;
    [SerializeField] private Button backButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] TextMeshProUGUI confirmLabel;
    [SerializeField] private Button endTurnButton;

    private static readonly SAMType[] SamSteps = (SAMType[])Enum.GetValues(typeof(SAMType));

    private void Awake()
    {
        GameManager.NewTurnEvent += OnNextTurn;
        TurnManager.NewPhaseEvent += OnNewPhase;

        backButton.onClick.AddListener(OnBackClicked);
        confirmButton.onClick.AddListener(OnConfirmClicked);
        endTurnButton.onClick.AddListener(() => TurnManager.Instance.EndTurn());
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
            samGrid.Hide();
            deckControls.SetActive(false);
            return;
        }
        GenerateActionCards(GameManager.Instance.CurrentPlayer);
    }

    private void OnBackClicked()
    {
        GameManager.Instance.CurrentPlayer.StepBackPlanning();
        RefreshCards();
    }

    private void OnConfirmClicked()
    {
        Player current = GameManager.Instance.CurrentPlayer;

        if (!current.CanAdvancePlanningStep)
        {
            TurnManager.Instance.EndTurn();
            return;
        }

        current.AdvancePlanningStep();
        RefreshCards();
    }

    private void GenerateActionCards(Player current)
    {
        // only the steps not yet confirmed this tick are in the deck
        for (int stepIndex = SamSteps.Length - 1; stepIndex >= (int)current.PlanningStep; stepIndex --)
        {
            LaneCard card = SamSteps[stepIndex] switch
            {
                SAMType.Shoot => CreateShootCard(),
                SAMType.Aim => CreateAimCard(),
                _ => CreateMoveCard(), 
            };

            int depthInDeck = stepIndex - (int)current.PlanningStep;
            ((RectTransform)card.transform).anchoredPosition = deckOffset * depthInDeck;
        }

        samGrid.Show(current, (tickIndex, step) =>
            PlanAndRefresh(() => current.RewindPlanningTo(tickIndex, step)));

        deckControls.SetActive(true);
        backButton.interactable = 
            current.PlanningTickOfTurn > 1 || current.PlanningStep != SAMType.Shoot;
        confirmLabel.text = current.CanAdvancePlanningStep
            ? "Next"
            : "End Turn";
        
        return;

        LaneCard CreateShootCard()
        {
            Weapon weapon = current.CurrentWeapon;
            float fireReadiness = current.PlannedFireReadiness;
            LaneCard shootCard = CreateLaneCard("Shoot")
                .SetFill(fireReadiness < 1f ? fireReadiness : 0f);

            foreach (Weapon heldWeapon in current.Weapons)
            {
                shootCard.AddOption("",
                    () => PlanAndRefresh(() => current.SelectWeapon(heldWeapon)),
                    icon: heldWeapon.Data.Icon,
                    selected: heldWeapon == current.CurrentWeapon);
            }

            return shootCard
                .AddRow()
                .AddOption($"{current.GetUnplannedAmmo(weapon)}/{weapon.Data.MaxAmmo}",
                    () => PlanAndRefresh(() => current.PlanShot(weapon)),
                    current.CanPlanShot(weapon),
                    selected: current.IsPlanningShot(weapon))
                .AddRow()
                .AddOption("Hold", () => PlanAndRefresh(current.PlanHoldFire),
                    current.PlanningStep == SAMType.Shoot,
                    selected: current.IsHoldingFire);
        }

        LaneCard CreateAimCard()
        {
            return CreateLaneCard("Aim")
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
        }

        LaneCard CreateMoveCard()
        {
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
            return moveCard;
        }

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