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

        // the game manager calls this when a hex entered during the move is a portal entrance
        // (TileEffects.OnEnter returns its target). ResolveMove steps from the real Coord, so moving
        // it here makes the rest of the (already turned) steps carry on from the target
        public void MoveThroughPortal(HexCoord exit)
        {
            Debug.Log($"{name} went through the portal at {Coord} to {exit}");

            Coord = exit;
            tickMovePath.Add(Coord);
            SnapToCoord(Coord);
        }

        #endregion
    }
}
