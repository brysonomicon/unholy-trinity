using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public partial class Player
    {
        #region Portals

        // the game manager calls this after each hex entered during the move. if the player stepped
        // onto a portal entrance it moves them to the portal's target and turns the rest of the turn's
        // plan by the portal's rotation, so momentum and every remaining step carry on in the new
        // direction. movement itself doesn't know about portals: ResolveMove keeps a reference to this
        // tick's PlannedMove and steps from the real Coord, so changing both here is enough.
        // arriving on the target is not a step, so it never sends the player on again
        public bool ApplyPortal(int tickIndex)
        {
            if (IsDead ||
                mapManager == null ||
                mapManager.Map == null ||
                !mapManager.Map.TryGetPortalExit(Coord, out HexCoord exit, out int rotation))
            {
                return false;
            }

            Debug.Log($"{name} went through the portal at {Coord} to {exit}, rotated {rotation}");

            Coord = exit;
            tickMovePath.Add(Coord);
            SnapToCoord(Coord);

            momentum = momentum.RotateAround(HexCoord.Zero, rotation);

            // this tick's steps already taken get turned too, but ResolveMove never reads them again
            for (int i = tickIndex; i < plannedActions.Count; i++)
            {
                UnholyAction action = plannedActions[i];
                HexDirectionExtensions.RotateAll(action.PlannedMove, rotation);
                action.PlannedMomentum = action.PlannedMomentum.RotateAround(HexCoord.Zero, rotation);
                if (action.PlannedDirection.HasValue)
                {
                    action.PlannedDirection = action.PlannedDirection.Value.Rotate(rotation);
                }
            }

            return true;
        }

        #endregion
    }
}
