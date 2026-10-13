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
        GenerateDefaultCards();
    }

    private void TestFunction(string text)
    {
        Debug.Log("text: " + text);
    }
    private void GenerateDefaultCards()
    {
        Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup("Move", 1, () => TestFunction("Move card"), Color.black);
        
        Instantiate(cardPrefab, cardSection.transform)
            .GetComponent<ActionCard>()
            .Setup("Rotate", 1, () => TestFunction("Rotate card"), Color.black);
        cardSection.MarkDirty();
    }
}
