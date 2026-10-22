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

        ActionCard weaponCard = Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>();

        SetupWeaponCard(current.CurrentWeapon);




        return;

        void SetupWeaponCard(Weapon weapon, Action beforeSetup = null)
        {
            beforeSetup?.Invoke();
            string weaponCardTitle =
                $"Fire {weapon.Data.DisplayName}";
            int weaponCardTickCost = weapon.Data.TickCost;
            Debug.Log($"Setting ammo to {weapon.Ammo}");
            weaponCard.Setup(weaponCardTitle, weaponCardTickCost,
                () => SetupWeaponCard(weapon, () => current.QueueShot(weapon)),
                Color.black,
                $"{current.GetUnplannedAmmo(weapon)}/{weapon.Data.MaxAmmo}");

            if (current.CanQueueShot(weapon))
            {
                weaponCard.EnableClickable();
            }
            else
            {
                weaponCard.DisableClickable();
            }
        }
    }

    private void ClearCardSection()
    {
        //? [CB] I wonder if it'd be worth caching and enabling/disabling cards
        foreach (var child in cardSection.GetComponentsInChildren<ActionCard>())
        {
            Destroy(child.gameObject);
        }
    }
}