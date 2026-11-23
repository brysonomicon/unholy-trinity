using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnholyTrinity.Hex.Interfaces
{
    public interface IMovable
    {
        static event Action<IMovable> RegisterMover;
        static event Action<IMovable> UnregisterMover;

        public void Register(IMovable mover) => RegisterMover?.Invoke(mover);

        public void Unregister(IMovable mover) =>
            UnregisterMover?.Invoke(mover);

        HexCoord Momentum { get; }
        int MomentumGainPerTick { get; }
        int MoveIndex { get; }
        HexTile Tile { get; set; }
        HexCoord Coord { get; }
        List<HexDirection> MoveSteps { get; set; }
        public bool CanMove => MoveIndex < MoveSteps.Count;
        public int RemainingStepCount => MoveSteps.Count - MoveIndex;
        public HexDirection NextStep => MoveSteps[MoveIndex + 1];

        // ReSharper disable once InconsistentNaming
        GameObject gameObject { get; }

        void BeginMovePhase();
        void EndMovePhase();

        void Move();

        // {
        //     if (MoveIndex >= MoveSteps.Count - 1)
        //     {
        //         throw new ArgumentOutOfRangeException(
        //             $"{gameObject.name} was asked to move when it had no moves remaining!");
        //     }
        //
        //     MoveIndex++;
        //     HexMap map = MapManager.Instance.Map;
        //
        //     HexCoord target = map.Neighbors(Position)
        //         .First(coord => coord == Position);
        //     MapManager.Instance.Map.Step(Position, target, this);
        // }
    }
}
