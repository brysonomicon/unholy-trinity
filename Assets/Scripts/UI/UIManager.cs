using System;
using UnholyTrinity.Players;
using UnholyTrinity.Weapons;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject cardSection;

    [SerializeField] private GameObject cardPrefab;
    private void Awake()
    {
        GameManager.NewTurnEvent += OnNextTurn;
        TurnManager.NewPhaseEvent += OnPhaseChange;
    }

    private void OnDestroy()
    {
        GameManager.NewTurnEvent -= OnNextTurn;
        TurnManager.NewPhaseEvent -= OnPhaseChange;
    }

    private void OnNextTurn(Player player)
    {
        ClearCardSection();
        
        GenerateDefaultCards(player);
    }
    
    private void OnPhaseChange(TurnPhase obj)
    {
        ClearCardSection();
    }

    private void GenerateDefaultCards(Player current)
    {
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
                () => SetupWeaponCard(weapon, () => current.Fire(weapon)), Color.black,
                $"{weapon.Ammo}/{weapon.Data.MaxAmmo}");
            
            if (weapon.CanFire)
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
        foreach (var child in cardSection.GetComponentsInChildren<Transform>())
        {
            Destroy(child.gameObject);
        }
    }
}