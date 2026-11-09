using System;
using UnholyTrinity.Hex;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// The one place that turns momentum plus planned input into a tick's movement, and decides the momentum
    /// carried out of it. Previews and resolution both go through here, so the player always sees the real outcome.
    ///
    /// Rules:
    /// - No input keeps sliding at the current velocity.
    /// - Input opposite to the current velocity brakes to a stop.
    /// - Input lined up with the unit's aim (see <see cref="Facing.LinesUpWith"/>) starts at base speed, then adds
    ///   1 per tick in the same direction up to <see cref="SimSettings.AimedMaxSpeed"/>.
    /// - Any other input moves at <see cref="SimSettings.BaseSpeed"/> in that direction.
    /// - Anything that halts the move (a wall, another unit, death) zeroes the velocity.
    /// </summary>
    public static class MoveModel
    {
        public static UnitMove Resolve(UnitState unit, MoveInput input, SimSettings settings)
        {
            HexVelocity velocity = unit.Velocity;

            if (input.Direction is not { } direction)
                return new UnitMove(velocity.Direction, velocity.Speed);

            if (velocity.IsMoving && direction == velocity.Direction.Opposite())
                return new UnitMove(direction, 0);

            bool aimed = input.Facing is { } facing && facing.LinesUpWith(direction, settings.BetweenAimsAccelerate);
            if (!aimed)
                return new UnitMove(direction, settings.BaseSpeed);

            int speed = velocity.IsMoving && velocity.Direction == direction
                ? velocity.Speed + 1
                : settings.BaseSpeed;
            return new UnitMove(direction, Math.Min(speed, settings.AimedMaxSpeed));
        }

        public static HexVelocity VelocityAfter(UnitMove move) =>
            move.IsHalted ? HexVelocity.Zero : new HexVelocity(move.Direction, move.StepsTaken);
    }
}
