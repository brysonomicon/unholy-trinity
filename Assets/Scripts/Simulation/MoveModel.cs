using UnholyTrinity.Hex;

namespace UnholyTrinity.Simulation
{
    /// <summary>
    /// The one place that turns momentum plus planned input into a tick's movement, and decides the momentum
    /// carried out of it. Previews and resolution both go through here, so the player always sees the real outcome.
    ///
    /// Placeholder until momentum is designed: a planned direction moves one hex, no input stays put, and the
    /// velocity is recorded but not yet fed back in.
    /// </summary>
    public static class MoveModel
    {
        public static UnitMove Resolve(UnitState unit, MoveInput input) =>
            input.Direction is { } direction
                ? new UnitMove(direction, 1)
                : new UnitMove(unit.Velocity.Direction, 0);

        public static HexVelocity VelocityAfter(UnitMove move) =>
            move.IsHalted ? HexVelocity.Zero : new HexVelocity(move.Direction, move.StepsTaken);
    }
}
