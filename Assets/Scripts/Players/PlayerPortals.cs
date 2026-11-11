using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public partial class Player
    {
        #region Portals

        // portals are baked into the plan: UpdatePlannedMove turns every step after a portal and the
        // tick's momentum, so the preview, the forecast and resolution only need to jump to the target.
        // arriving on the target is not a step, so it never sends the player on again

        // where a player standing on coord ends up: the portal's target on an entrance, otherwise coord
        private HexCoord ThroughPortal(HexCoord coord)
        {
            return mapManager != null &&
                   mapManager.Map != null &&
                   mapManager.Map.TryGetPortalExit(coord, out HexCoord exit, out _)
                ? exit
                : coord;
        }

        // the game manager calls this after each hex entered during the move. ResolveMove steps from
        // the real Coord, so moving it here makes the rest of the (already turned) steps carry on
        // from the target
        public bool ApplyPortal()
        {
            if (IsDead)
            {
                return false;
            }

            HexCoord exit = ThroughPortal(Coord);
            if (exit == Coord)
            {
                return false;
            }

            Debug.Log($"{name} went through the portal at {Coord} to {exit}");

            Coord = exit;
            tickMovePath.Add(Coord);
            SnapToCoord(Coord);
            return true;
        }

        #endregion
    }
}
