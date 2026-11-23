namespace UnholyTrinity.Hex.Interfaces
{
public interface IAimer
{
    AimDirection CurrentAimDirection { get; }
    HexCoord? SelectedTile { get; }
}
}
