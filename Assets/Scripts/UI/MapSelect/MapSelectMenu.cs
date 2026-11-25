using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Builds the map grid from a MapCatalog and handles picking/confirming.
/// Lives on the MapSelectMenu panel next to PanelSelection. Leave PanelSelection's
/// firstSelected empty: this script focuses the last-played map's card on open instead.
public class MapSelectMenu : MonoBehaviour
{
    const string LastMapKey = "LastSelectedMap";

    [SerializeField] MapCatalog catalog;
    [SerializeField] MapCard cardPrefab;
    [Tooltip("The ScrollRect's Content (the one with the GridLayoutGroup).")]
    [SerializeField] RectTransform content;
    [SerializeField] Button selectButton;
    [Tooltip("Scene to load when a map is confirmed. Must be in Build Settings.")]
    [SerializeField] string gameScene;

    readonly List<MapCard> cards = new List<MapCard>();
    MapCard picked;

    void Awake()
    {
        BuildCards();
        if (selectButton)
        {
            selectButton.onClick.AddListener(ConfirmPicked);
        }
    }

    void OnEnable()
    {
        if (cards.Count == 0) return;

        var lastId = PlayerPrefs.GetString(LastMapKey, null);
        var start = cards.Find(c => c.Entry.Id == lastId) ?? cards[0];
        Pick(start);
        if (EventSystem.current)
        {
            EventSystem.current.SetSelectedGameObject(start.gameObject);
        }
    }

    void BuildCards()
    {
        // Clear any placeholder cards left in the prefab for layout preview.
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        if (!catalog || !cardPrefab)
        {
            return;
        }

        foreach (var entry in catalog.Maps)
        {
            if (entry.definition == null)
            {
                continue;
            }
            var card = Instantiate(cardPrefab, content);
            card.Setup(entry, Pick, Confirm);
            cards.Add(card);
        }
    }

    void Pick(MapCard card)
    {
        if (picked == card)
        {
            return;
        }
        if (picked)
        {
            picked.SetPicked(false);
        }
        picked = card;
        if (picked)
        {
            picked.SetPicked(true);
        }
        if (selectButton)
        {
            selectButton.interactable = picked != null;
        }
    }

    void Confirm(MapCard card)
    {
        Pick(card);
        ConfirmPicked();
    }

    public void ConfirmPicked()
    {
        if (!picked)
        {
            return;
        }
        if (string.IsNullOrEmpty(gameScene))
        {
            Debug.LogWarning("MapSelectMenu: no game scene set.", this);
            return;
        }

        PlayerPrefs.SetString(LastMapKey, picked.Entry.Id);
        PlayerPrefs.Save();
        SelectedMap.Set(picked.Entry.definition);
        SceneManager.LoadScene(gameScene);
    }
}
