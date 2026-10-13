using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ActionCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Color textColor;
    [SerializeField] private TextMeshProUGUI cardNameField;
    [SerializeField] protected TextMeshProUGUI extraInfoPlaceholder;
    [SerializeField] private TextMeshProUGUI tickCostField;
    private UnityAction _buttonClick;
    
    private static readonly Color DefaultTextColor = Color.black;


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

    public void Setup(string cardName, int tickCost,  UnityAction callback,
        Color? newTextColor = null, string extraInfoText = "")
    {
        CardName = cardName;
        TickCost = tickCost;
        extraInfoPlaceholder.text = extraInfoText;
        
        //If the passed in color is null, use default
        newTextColor ??= DefaultTextColor;
        
        cardNameField.color = newTextColor.Value;
        tickCostField.color = newTextColor.Value;
        extraInfoPlaceholder.color = newTextColor.Value;
        
        if (_buttonClick != null)
        {
            button.onClick.RemoveListener(_buttonClick);
        }
        _buttonClick = callback;
        button.onClick.AddListener(_buttonClick);
    }
}