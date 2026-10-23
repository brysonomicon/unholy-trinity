using UnholyTrinity.Hex;

namespace UnholyTrinity.Weapons
{
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
    }
}