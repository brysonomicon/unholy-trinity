using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Converts between hex coordinates and world space on the XZ plane (+X east, +Z north).
    /// Axial -r points north, so <see cref="HexDirection.D1"/> is NE in both orientations.
    /// </summary>
    public readonly struct HexLayout
    {
        static readonly float Sqrt3 = Mathf.Sqrt(3f);

        public readonly HexOrientation Orientation;
        /// <summary>Distance from a hex centre to any corner.</summary>
        public readonly float Size;
        public readonly Vector3 Origin;

        public HexLayout(HexOrientation orientation, float size, Vector3 origin)
        {
            Orientation = orientation;
            Size = size;
            Origin = origin;
        }

        /// <summary>World-space centre of a hex.</summary>
        public Vector3 HexToWorld(HexCoord hex)
        {
            // x and y here are 2D layout axes with y pointing south, which is why y is negated onto Z below.
            float x, y;
            if (Orientation == HexOrientation.PointyTop)
            {
                x = Size * (Sqrt3 * hex.Q + Sqrt3 / 2f * hex.R);
                y = Size * (1.5f * hex.R);
            }
            else
            {
                x = Size * (1.5f * hex.Q);
                y = Size * (Sqrt3 / 2f * hex.Q + Sqrt3 * hex.R);
            }
            return Origin + new Vector3(x, 0f, -y);
        }

        /// <summary>The hex containing a world position (only X and Z are used), e.g. for mouse picking.</summary>
        public HexCoord WorldToHex(Vector3 world)
        {
            // Inverse of HexToWorld: produces fractional axial coordinates, which CubeRound snaps to a hex.
            var local = world - Origin;
            float x = local.x / Size;
            float y = -local.z / Size;
            float q, r;
            if (Orientation == HexOrientation.PointyTop)
            {
                q = Sqrt3 / 3f * x - 1f / 3f * y;
                r = 2f / 3f * y;
            }
            else
            {
                q = 2f / 3f * x;
                r = -1f / 3f * x + Sqrt3 / 3f * y;
            }
            return HexCoord.CubeRound(q, r, -q - r);
        }

        /// <summary>Corner offset from a hex centre, indexed counter-clockwise.</summary>
        public Vector3 CornerOffset(int corner)
        {
            float startDegrees = Orientation == HexOrientation.PointyTop ? 30f : 0f;
            float angle = (startDegrees + 60f * corner) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Size;
        }

        /// <summary>Unit world-space vector for a hex direction, e.g. for rotating units or arrows.</summary>
        public Vector3 DirectionToWorld(HexDirection direction) =>
            (HexToWorld(direction.ToOffset()) - HexToWorld(HexCoord.Zero)).normalized;
    }
}
