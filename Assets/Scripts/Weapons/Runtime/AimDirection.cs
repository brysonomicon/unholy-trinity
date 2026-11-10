using UnholyTrinity.Hex;
using UnholyTrinity.Simulation;

namespace UnholyTrinity.Weapons
{
    // 12 aims in 30 degree steps. even aims point down a hex direction, odd aims point
    // halfway between that direction and the next one
    public enum AimDirection
    {
        A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11
    }

    public static class AimDirectionExtensions
    {
        public static HexCoord ToOffset(this AimDirection aim)
        {
            int index = (int)aim;

            HexDirection hexDirection = (HexDirection)(index / 2);

            if (index % 2 == 0)
            {
                return hexDirection.ToOffset();
            }

            return hexDirection.ToOffset() + hexDirection.Rotate(1).ToOffset();
        }

        // the simulation's view of the aim, used to decide if a move accelerates
        public static Facing ToFacing(this AimDirection aim)
        {
            int index = (int)aim;
            return new Facing((HexDirection)(index / 2), index % 2 == 1);
        }
    }
}
