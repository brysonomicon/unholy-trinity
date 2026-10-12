using System.Collections.Generic;

namespace UnholyTrinity.Hex
{
    /// <summary>Rules for shots that haven't been settled yet. Extend this as design decisions land.</summary>
    public struct ShotTraceOptions
    {
        public bool PassThroughPortals;
        /// <summary>
        /// Shots reverse off bouncy walls instead of stopping. A ricochet uses no range. The shot can come
        /// back through the origin hex, so callers decide whether the shooter can be hit.
        /// </summary>
        public bool RicochetOffBouncyWalls;

        public static ShotTraceOptions Default => new ShotTraceOptions { PassThroughPortals = false, RicochetOffBouncyWalls = false };
    }

    public class ShotTraceResult
    {
        /// <summary>
        /// Hexes the shot travelled through, in order (not including the origin or the blocking hex).
        /// A hex can appear more than once after a ricochet.
        /// </summary>
        public readonly List<HexCoord> Path = new List<HexCoord>();
        /// <summary>Bouncy wall hexes the shot ricocheted off, in order.</summary>
        public readonly List<HexCoord> Ricochets = new List<HexCoord>();
        /// <summary>The wall or off-map hex that stopped the shot, or null if it ran out of range.</summary>
        public HexCoord? BlockedAt;
        /// <summary>Direction the shot was travelling when it ended (portals and ricochets can change it).</summary>
        public HexDirection FinalDirection;
    }

    /// <summary>Line-of-fire queries against the map. Does not know about units; callers check the path for targets.</summary>
    public static class ShotTracer
    {
        /// <summary>Traces a shot along one of the six hex axes for up to <paramref name="maxRange"/> hexes.</summary>
        public static ShotTraceResult Trace(HexMap map, HexCoord origin, HexDirection direction, int maxRange, ShotTraceOptions options)
        {
            var result = new ShotTraceResult();
            var position = origin;
            int moved = 0;
            bool ricochetedSinceLastMove = false;

            while (moved < maxRange)
            {
                var next = position.Neighbor(direction);
                if (map.BlocksLineOfFire(next))
                {
                    // A second ricochet without moving means the shot is wedged between bouncy walls; let it stop.
                    if (options.RicochetOffBouncyWalls && !ricochetedSinceLastMove
                        && map.TryGetWall(next, out var wall) && wall.Kind == WallKind.Bouncy)
                    {
                        direction = direction.Opposite();
                        result.Ricochets.Add(next);
                        ricochetedSinceLastMove = true;
                        continue;
                    }

                    result.BlockedAt = next;
                    break;
                }

                ricochetedSinceLastMove = false;
                moved++;
                position = next;
                result.Path.Add(position);

                if (options.PassThroughPortals && map.TryGetPortalExit(position, out var exit, out int rotation))
                {
                    direction = direction.Rotate(rotation);
                    position = exit;
                    result.Path.Add(position);
                }
            }

            result.FinalDirection = direction;
            return result;
        }

        public static ShotTraceResult Trace(HexMap map, HexCoord origin, HexDirection direction, int maxRange) =>
            Trace(map, origin, direction, maxRange, ShotTraceOptions.Default);

        /// <summary>
        /// True when no hex strictly between <paramref name="from"/> and <paramref name="to"/> blocks line of fire.
        /// Straight line only; ignores portals.
        /// </summary>
        public static bool HasLineOfSight(HexMap map, HexCoord from, HexCoord to)
        {
            var line = from.LineTo(to);
            for (int i = 1; i < line.Count - 1; i++)
                if (map.BlocksLineOfFire(line[i]))
                    return false;
            return true;
        }
    }
}
