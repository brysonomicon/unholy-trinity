using System;
using UnholyTrinity.Hex;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// The one place that turns momentum plus planned input into a tick's movement, and decides the momentum
    /// carried out of it. Previews and resolution both go through here, so the player always sees the real outcome.
    ///
    /// Rules:
    /// - A tick with no plan at all (<see cref="MoveInput.None"/>) doesn't move and keeps the current momentum.
    /// - A planned tick with no direction holds: no move, and momentum drops to zero.
    /// - Moving, aimed along the move (see <see cref="Facing.LinesUpWith"/>) and not reversing: previous speed plus
    ///   <see cref="SimSettings.AimedGain"/>, up to <see cref="SimSettings.AimedMaxSpeed"/>. This applies when
    ///   turning too, so an aimed turn carries speed into the new direction.
    /// - Otherwise, the same direction as the current momentum keeps its speed.
    /// - Anything else, including reversing and starting from a stop, moves at <see cref="SimSettings.BaseSpeed"/>.
    /// - Momentum is the planned velocity even if a wall or another unit stops the move early. Only death zeroes it.
    /// </summary>
    public static class MoveModel
    {
        public static UnitMove Resolve(UnitState unit, MoveInput input, SimSettings settings)
        {
            HexVelocity velocity = unit.Velocity;

            if (!input.IsPlanned)
                return new UnitMove(velocity.Direction, 0, velocity);

            if (input.Direction is not { } direction)
                return new UnitMove(velocity.Direction, 0, HexVelocity.Zero);

            bool isMoving = velocity.IsMoving;
            bool isAimingAlongMove = input.Facing is { } facing
                                     && facing.LinesUpWith(direction, settings.BetweenAimsAccelerate);
            bool isSameDirection = isMoving && velocity.Direction == direction;
            bool isReversing = isMoving && velocity.Direction.Opposite() == direction;

            int speed = settings.BaseSpeed;

            if (isMoving && isAimingAlongMove && !isReversing)
                speed = Math.Min(velocity.Speed + settings.AimedGain, settings.AimedMaxSpeed);
            else if (isSameDirection)
                speed = velocity.Speed;

            return new UnitMove(direction, speed, new HexVelocity(direction, speed));
        }

        public static HexVelocity VelocityAfter(UnitState unit, UnitMove move) =>
            unit.IsDead ? HexVelocity.Zero : move.Momentum;
    }
}
