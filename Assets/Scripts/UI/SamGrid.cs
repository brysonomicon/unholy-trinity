using System;
using UnholyTrinity.Helpers;
using UnholyTrinity.Managers;
using UnholyTrinity.Players;
using UnityEngine;

// the current players plan as a grid. each row is a tick, columns are shoot/aim/move
public class SamGrid : MonoBehaviour
{
    [SerializeField] private SamGridCell cellPrefab;

    private static readonly SAMType[] SamSteps = (SAMType[])Enum.GetValues(typeof(SAMType));

    public void Show(Player current, Action<int, SAMType> onCellClicked)
    {
        Clear();
        gameObject.SetActive(true);

        for (int tickIndex = 0; tickIndex < TurnManager.Instance.TicksPerTurn; tickIndex++)
        {
            int cellTickIndex = tickIndex;
            bool isPlanningTick = tickIndex == current.PlanningTickOfTurn - 1;

            foreach (SAMType step in SamSteps)
            {
                Instantiate(cellPrefab, transform).Setup(
                    () => onCellClicked(cellTickIndex, step),
                    current.IsStepConfirmed(tickIndex, step),
                    isPlanningTick && step == current.PlanningStep
                );
            }
        }
    }

    public void Hide()
    {
        Clear();
        gameObject.SetActive(false);
    }

    private void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
