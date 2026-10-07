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
        /// <summary>Bounced at <see cref="MoveEvent.Coord"/> off the bouncy wall at <see cref="MoveEvent.Other"/>.</summary>
        Bounced,
        /// <summary>
        /// Hit the spiked wall at <see cref="MoveEvent.Other"/> while at <see cref="MoveEvent.Coord"/>.
        /// <see cref="MoveEvent.Amount"/> is the damage; always follows a <see cref="Collided"/> event.
        /// </summary>
        Spiked,
    }

    public readonly struct MoveEvent
    {
        public readonly MoveEventType Type;
        public readonly HexCoord Coord;
        public readonly HexCoord Other;
        public readonly HexVelocity VelocityAfter;
        /// <summary>Damage for <see cref="MoveEventType.Spiked"/>; 0 for other events.</summary>
        public readonly int Amount;

        public MoveEvent(MoveEventType type, HexCoord coord, HexCoord other, HexVelocity velocityAfter, int amount = 0)
        {
            Type = type;
            Coord = coord;
            Other = other;
            VelocityAfter = velocityAfter;
            Amount = amount;
        }

        public override string ToString() =>
            Amount != 0 ? $"{Type} {Amount} at {Coord} (other {Other}) -> {VelocityAfter}" : $"{Type} at {Coord} (other {Other}) -> {VelocityAfter}";
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
    ///   A spiked wall does the same and also reports damage, worked out from the speed at impact.
    /// - A bouncy wall reverses the direction without using a step, so the unit carries on with its
    ///   remaining steps and keeps its speed. Bouncing twice without moving (wedged between two
    ///   bouncy walls) counts as a normal collision.
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
            bool bouncedSinceLastStep = false;

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
                    map.TryGetWall(next, out var wall);

                    if (wall.Kind == WallKind.Bouncy && !bouncedSinceLastStep)
                    {
                        velocity = velocity.WithDirection(velocity.Direction.Opposite());
                        result.Events.Add(new MoveEvent(MoveEventType.Bounced, position, next, velocity));
                        bouncedSinceLastStep = true;
                        continue;
                    }

                    // Damage depends on the speed at impact, so work it out before the speed is zeroed.
                    int damage = wall.DamageFor(velocity.Speed);
                    velocity = velocity.WithSpeed(0);
                    result.Events.Add(new MoveEvent(MoveEventType.Collided, position, next, velocity));
                    if (wall.Kind == WallKind.Spiked)
                        result.Events.Add(new MoveEvent(MoveEventType.Spiked, position, next, velocity, damage));
                    break;
                }

                bouncedSinceLastStep = false;
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
