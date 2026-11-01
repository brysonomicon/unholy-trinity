namespace UnholyTrinity.Simulation
{
    /// <summary>One unit's input for a simulated tick.</summary>
    public readonly struct UnitTickInput
    {
        public readonly int UnitId;
        public readonly MoveInput Move;

        public UnitTickInput(int unitId, MoveInput move)
        {
            UnitId = unitId;
            Move = move;
        }
    }
}
