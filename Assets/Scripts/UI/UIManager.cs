using UnholyTrinity.Players;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Player player;

    [SerializeField] private GameObject cardSection;

    [SerializeField] private GameObject cardPrefab;

    private void Start()
    {
        GenerateDefaultCards(player);
        GameManager.TurnChangedEvent += OnNextTurn;
    }

    private void OnDestroy()
    {
        GameManager.TurnChangedEvent -= OnNextTurn;
    }

    private void OnNextTurn(Player currentPlayer)
    {
        ClearCardSection();
        GenerateDefaultCards(currentPlayer);
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

        // [CB] TODO: Add logic for enabling and disabling cards based on if weapons can fire.
        ActionCard weaponCard = Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup($"Fire {player.CurrentWeapon.Data.DisplayName}",
                player.CurrentWeapon.Data.TickCost,
                () => player.Fire(player.CurrentWeapon),
                Color.black,
                $"{player.CurrentWeapon.Ammo}/{player.CurrentWeapon.Data.MaxAmmo}");
        if (!player.CurrentWeapon.CanFire)
        {
            weaponCard.DisableClickable();
        }

        // cardSection.MarkDirty();
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