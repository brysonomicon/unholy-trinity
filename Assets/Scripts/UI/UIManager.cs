using TMPro;
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
            .Setup("Rotate Clockwise", 
                1,
                () => current.RotatePlayer(true),
                Color.black);
        Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup("Rotate CCW", 
                1,
                () => current.RotatePlayer(false),
                Color.black);
        
        cardSection.MarkDirty();
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
