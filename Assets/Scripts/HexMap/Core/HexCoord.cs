using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>
    /// Axial hex coordinate (q, r). The implicit third cube coordinate is s = -q - r.
    /// See https://www.redblobgames.com/grids/hexagons/ for the maths.
    /// </summary>
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        [SerializeField] int q;
        [SerializeField] int r;

        public HexCoord(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        public static readonly HexCoord Zero = new HexCoord(0, 0);

        public int Q => q;
        public int R => r;
        public int S => -q - r;

        public HexCoord Neighbor(HexDirection direction) => this + direction.ToOffset();

        public int DistanceTo(HexCoord other) => Distance(this, other);

        /// <summary>Number of single-hex steps between two hexes, ignoring obstacles.</summary>
        public static int Distance(HexCoord a, HexCoord b)
        {
            // Each step changes two cube coordinates by 1, so the step count is half the summed difference.
            var d = a - b;
            return (Math.Abs(d.Q) + Math.Abs(d.R) + Math.Abs(d.S)) / 2;
        }

        /// <summary>Rotates this coordinate around <paramref name="center"/> by 60 degree steps (positive is counter-clockwise).</summary>
        public HexCoord RotateAround(HexCoord center, int steps)
        {
            var v = this - center;
            int x = v.Q, y = v.R, z = v.S;
            for (int i = 0; i < HexDirectionExtensions.Mod(steps); i++)
            {
                // (q, r, s) -> (-s, -q, -r) is one counter-clockwise step.
                int nx = -z, ny = -x, nz = -y;
                x = nx;
                y = ny;
                z = nz;
            }
            return center + new HexCoord(x, y);
        }

        /// <summary>All hexes exactly <paramref name="radius"/> steps away from <paramref name="center"/>.</summary>
        public static IEnumerable<HexCoord> Ring(HexCoord center, int radius)
        {
            if (radius <= 0)
            {
                yield return center;
                yield break;
            }

            // Start at the D4 corner of the ring, then walk `radius` steps along each of the six sides.
            // Walking D0..D5 in order from that corner traces the ring counter-clockwise back to the start.
            var hex = center + HexDirection.D4.ToOffset() * radius;
            for (int side = 0; side < HexDirectionExtensions.Count; side++)
            {
                for (int step = 0; step < radius; step++)
                {
                    yield return hex;
                    hex = hex.Neighbor((HexDirection)side);
                }
            }
        }

        /// <summary>All hexes within <paramref name="radius"/> steps of <paramref name="center"/> (a hexagon-shaped area).</summary>
        public static IEnumerable<HexCoord> Range(HexCoord center, int radius)
        {
            // A hex is in range when |dq|, |dr| and |ds| are all <= radius. Looping dq over that range,
            // the r bounds below are what also keep ds = -dq - dr within it.
            for (int dq = -radius; dq <= radius; dq++)
            {
                int rMin = Math.Max(-radius, -dq - radius);
                int rMax = Math.Min(radius, -dq + radius);
                for (int dr = rMin; dr <= rMax; dr++)
                    yield return center + new HexCoord(dq, dr);
            }
        }

        /// <summary>
        /// Hexes on the straight line from this to <paramref name="target"/>, both ends included.
        /// A tiny nudge breaks ties consistently when the line runs exactly along a hex edge.
        /// </summary>
        public List<HexCoord> LineTo(HexCoord target)
        {
            // Sample n + 1 evenly spaced points between the two hex centres (one per hex the line
            // passes through) and snap each one to its containing hex.
            int n = Distance(this, target);
            var result = new List<HexCoord>(n + 1);
            // The nudge keeps q + r + s = 0 so the points stay valid cube coordinates.
            const double eps = 1e-6;
            double aq = q + eps, ar = r + eps, as_ = S - 2 * eps;
            double bq = target.q + eps, br = target.r + eps, bs = target.S - 2 * eps;
            for (int i = 0; i <= n; i++)
            {
                double t = n == 0 ? 0.0 : (double)i / n;
                result.Add(CubeRound(aq + (bq - aq) * t, ar + (br - ar) * t, as_ + (bs - as_) * t));
            }
            return result;
        }

        /// <summary>
        /// Snaps fractional cube coordinates to the hex that contains them.
        /// Used for world-to-hex picking and line drawing.
        /// </summary>
        public static HexCoord CubeRound(double fq, double fr, double fs)
        {
            // Rounding each coordinate separately can break q + r + s = 0. Throw away the one that
            // moved most while rounding (the least reliable) and rebuild it from the other two.
            double rq = Math.Round(fq), rr = Math.Round(fr), rs = Math.Round(fs);
            double dq = Math.Abs(rq - fq), dr = Math.Abs(rr - fr), ds = Math.Abs(rs - fs);
            if (dq > dr && dq > ds)
                rq = -rr - rs;
            else if (dr > ds)
                rr = -rq - rs;
            // Otherwise s is the one to discard. That needs no code because s is never stored.
            return new HexCoord((int)rq, (int)rr);
        }

        public static HexCoord operator +(HexCoord a, HexCoord b) => new HexCoord(a.q + b.q, a.r + b.r);
        public static HexCoord operator -(HexCoord a, HexCoord b) => new HexCoord(a.q - b.q, a.r - b.r);
        public static HexCoord operator *(HexCoord a, int k) => new HexCoord(a.q * k, a.r * k);
        public static bool operator ==(HexCoord a, HexCoord b) => a.q == b.q && a.r == b.r;
        public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);

        public bool Equals(HexCoord other) => this == other;
        public override bool Equals(object obj) => obj is HexCoord other && this == other;
        public override int GetHashCode() => (q * 397) ^ r;
        public override string ToString() => $"({q}, {r})";
    }
}
