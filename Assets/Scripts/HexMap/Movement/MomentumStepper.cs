using System.Collections.Generic;

namespace UnholyTrinity.Hex
{
    public enum MoveEventType
    {
        /// <summary>Passed over a boost or friction tile at <see cref="MoveEvent.Coord"/>.</summary>
        Boosted,
        /// <summary>Entered the portal at <see cref="MoveEvent.Coord"/> and came out at <see cref="MoveEvent.Other"/>.</summary>
        Teleported,
        /// <summary>Was stopped at <see cref="MoveEvent.Coord"/> by the wall or map edge at <see cref="MoveEvent.Other"/>.</summary>
        Collided,
        /// <summary>The slide was cut short at <see cref="MoveEvent.Coord"/> by the step cap.</summary>
        StepCapReached,
    }

    public readonly struct MoveEvent
    {
        public readonly MoveEventType Type;
        public readonly HexCoord Coord;
        public readonly HexCoord Other;
        public readonly HexVelocity VelocityAfter;

        public MoveEvent(MoveEventType type, HexCoord coord, HexCoord other, HexVelocity velocityAfter)
        {
            Type = type;
            Coord = coord;
            Other = other;
            VelocityAfter = velocityAfter;
        }

        public override string ToString() => $"{Type} at {Coord} (other {Other}) -> {VelocityAfter}";
    }

    public class SlideResult
    {
        /// <summary>Every hex the unit occupied after leaving the start, in order. Portal entry and exit are both listed.</summary>
        public readonly List<HexCoord> Path = new List<HexCoord>();
        public readonly List<MoveEvent> Events = new List<MoveEvent>();
        public HexCoord End;
        public HexVelocity FinalVelocity;
        /// <summary>Hexes actually stepped through (teleports are free).</summary>
        public int StepsTaken;
    }

    /// <summary>
    /// Moves a single unit through the map under its own momentum. This is the map-side building block
    /// for turn resolution; it knows nothing about other units, occupancy or simultaneous moves.
    ///
    /// Rules:
    /// - Each step moves one hex in the current direction and uses one point of speed.
    /// - A wall or the map edge stops the unit in its current hex and resets speed to 0.
    /// - Entering a portal teleports to its partner (no extra step) and rotates the direction.
    ///   Arriving on the partner does not trigger it again.
    /// - Entering a boost tile changes the velocity straight away, so the change affects the rest
    ///   of this slide as well as the velocity carried into the next turn.
    /// - <c>maxSteps</c> caps the slide so boost chains cannot run forever.
    /// </summary>
    public static class MomentumStepper
    {
        public const int DefaultMaxSteps = 32;

        public static SlideResult Slide(HexMap map, HexCoord start, HexVelocity velocity, int maxSteps = DefaultMaxSteps)
        {
            var result = new SlideResult();
            var position = start;

            while (result.StepsTaken < velocity.Speed)
            {
                if (result.StepsTaken >= maxSteps)
                {
                    result.Events.Add(new MoveEvent(MoveEventType.StepCapReached, position, position, velocity));
                    break;
                }

                var next = position.Neighbor(velocity.Direction);
                if (!map.IsWalkable(next))
                {
                    velocity = velocity.WithSpeed(0);
                    result.Events.Add(new MoveEvent(MoveEventType.Collided, position, next, velocity));
                    break;
                }

                position = next;
                result.StepsTaken++;
                result.Path.Add(position);

                var tile = map[position];
                if (tile.Type == TileType.Portal && map.TryGetPortalExit(position, out var exit, out int rotation))
                {
                    velocity = velocity.WithDirection(velocity.Direction.Rotate(rotation));
                    result.Events.Add(new MoveEvent(MoveEventType.Teleported, position, exit, velocity));
                    position = exit;
                    result.Path.Add(position);
                }
                else if (tile.Type == TileType.Boost)
                {
                    velocity = tile.Boost.Apply(velocity);
                    result.Events.Add(new MoveEvent(MoveEventType.Boosted, position, position, velocity));
                }
            }

            result.End = position;
            result.FinalVelocity = velocity;
            return result;
        }
    }
}
