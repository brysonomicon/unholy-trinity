using System;
using UnholyTrinity.Players;
using UnholyTrinity.Weapons;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject cardSection;
    [SerializeField] private GameObject cardPrefab;

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
        Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup("End Turn", 0,
                () => TurnManager.Instance.EndTurn());
        
        Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup("Move", 1, current.MovePlayer, Color.black);

        //TODO: Pass in rotation after clicking on card somehow.
        Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup("Rotate CW",
                1,
                () => current.RotatePlayer(true),
                Color.black);
        Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup("Rotate CCW",
                1,
                () => current.RotatePlayer(false),
                Color.black);

        Weapon weapon = current.CurrentWeapon;
        ActionCard weaponCard = Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup($"Fire {weapon.Data.DisplayName}",
                weapon.Data.TickCost,
                () => PlanAndRefresh(() => current.QueueShot(weapon)),
                Color.black,
                $"{current.GetUnplannedAmmo(weapon)}/{weapon.Data.MaxAmmo}");
        SetClickable(weaponCard, current.CanQueueShot(weapon));

        ActionCard holdFireCard = Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup("Hold Fire", 1,
                () => PlanAndRefresh(current.HoldFire),
                Color.black);
        SetClickable(holdFireCard, current.HasFreeShotSlot);

        Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup("Clear Plan", 0,
                () => PlanAndRefresh(current.ClearPlan),
                Color.black);

        return;

        // does a planning action and then rebuilds every card so they reflect new plan
        void PlanAndRefresh(Action planAction)
        {
            planAction();
            ClearCardSection();
            GenerateDefaultCards(current);
        }

        // void SetupWeaponCard(Weapon weapon, Action beforeSetup = null)
        // {
        //     beforeSetup?.Invoke();
        //     string weaponCardTitle =
        //         $"Fire {weapon.Data.DisplayName}";
        //     int weaponCardTickCost = weapon.Data.TickCost;
        //     Debug.Log($"Setting ammo to {weapon.Ammo}");
        //     weaponCard.Setup(weaponCardTitle, weaponCardTickCost,
        //         () => SetupWeaponCard(weapon, () => current.QueueShot(weapon)),
        //         Color.black,
        //         $"{current.GetUnplannedAmmo(weapon)}/{weapon.Data.MaxAmmo}");

        //     if (current.CanQueueShot(weapon))
        //     {
        //         weaponCard.EnableClickable();
        //     }
        //     else
        //     {
        //         weaponCard.DisableClickable();
        //     }
        // }
    }

    private void ClearCardSection()
    {
        //? [CB] I wonder if it'd be worth caching and enabling/disabling cards
        foreach (var child in cardSection.GetComponentsInChildren<ActionCard>())
        {
            Destroy(child.gameObject);
        }
    }

    private static void SetClickable(ActionCard card, bool clickable)
    {
        if (clickable)
        {
            card.EnableClickable();
        }
        else
        {
            card.DisableClickable();
        }
    }
}