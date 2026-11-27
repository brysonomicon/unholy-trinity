using System;
using UnholyTrinity.Helpers;
using UnholyTrinity.Hex;
using UnholyTrinity.Managers;
using UnholyTrinity.Players;
using UnityEngine;

public class SamGrid : MonoBehaviour
{
    [SerializeField] private SamGridCell cellPrefab;
    [SerializeField] private Sprite holdFireIcon;

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
                bool confirmed = current.IsStepConfirmed(tickIndex, step);
                SamGridCell cell = Instantiate(cellPrefab, transform);

                cell.Setup(
                    () => onCellClicked(cellTickIndex, step),
                    confirmed,
                    isPlanningTick && step == current.PlanningStep
                );

                if (confirmed)
                {
                    ShowPlannedStep(cell, current.GetPlannedAction(tickIndex), step);
                }
            }
        }
    }

    public void Hide()
    {
        Clear();
        gameObject.SetActive(false);
    }

    private void ShowPlannedStep(SamGridCell cell, UnholyAction action, SAMType step)
    {
        switch (step)
        {
            case SAMType.Shoot:
                cell.ShowIcon(action.PlannedShot != null
                    ? action.PlannedShot.Data.Icon
                    : holdFireIcon);
                break;
            case SAMType.Aim:
                cell.ShowAim(action.PlannedAim);
                break;
            case SAMType.Move:
                cell.ShowText(action.PlannedDirection.HasValue
                    ? action.PlannedDirection.Value.DisplayName(HexOrientation.PointyTop)
                    : "Brake");
                break;
        }
    }

    private void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}