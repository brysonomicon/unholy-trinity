using System;
using TMPro;
using UnityEngine;

public class ActionCard : MonoBehaviour
{
    [SerializeField] private Color textColor;
    [SerializeField] private TMP_Text cardNameField;
    [SerializeField] protected GameObject extraInfoPlaceholder;
    [SerializeField] private TMP_Text tickCostField;

    private static readonly Color DefaultTextColor = Color.black;

    private void Start()
    {
        cardNameField.color = textColor;
        tickCostField.color = textColor;
    }

    public string CardName
    {
        get => cardNameField.text;
        private set => cardNameField.text = value;
    }

    public int TickCost
    {
        get
        {
            if (int.TryParse(tickCostField.text, out int result))
            {
                return result;
            }

            throw new InvalidCastException(
                $"{tickCostField.text} is not a number.");
        }
        set => tickCostField.text = value.ToString();
    }

    public static ActionCard Factory(string cardName, int tickCost,
        Color? textColor)
    {
        textColor ??= DefaultTextColor;

        GameObject card = new GameObject();
        ActionCard actionCard = card.AddComponent<ActionCard>();

        GameObject cardNameContainer = new();
        actionCard.cardNameField = cardNameContainer.AddComponent<TMP_Text>();
        actionCard.CardName = cardName;
        actionCard.cardNameField.color = (Color)textColor;
        cardNameContainer.transform.SetParent(card.transform);

        actionCard.extraInfoPlaceholder = new GameObject();
        actionCard.extraInfoPlaceholder.transform.SetParent(cardNameContainer
            .transform);

        GameObject tickCostContainer = new();
        actionCard.tickCostField = tickCostContainer.AddComponent<TMP_Text>();
        actionCard.tickCostField.color = (Color)textColor;
        actionCard.TickCost= tickCost;
        tickCostContainer.transform.SetParent(card.transform);

        return actionCard;
    }
}