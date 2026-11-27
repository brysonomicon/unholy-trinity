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

        int MoveIndex { get; set; }
        HexTile Tile { get; set; }
        HexCoord Coord { get; }
        List<HexDirection> MoveSteps { get; }
        public bool CanMove => MoveIndex < MoveSteps.Count - 1;
        public int RemainingStepCount => MoveSteps.Count - MoveIndex;

        // ReSharper disable once InconsistentNaming
        GameObject gameObject { get; }

        void BeginMovePhase();
        void EndMovePhase();

        void Move()
        {
            if (MoveIndex >= MoveSteps.Count - 1)
            {
                throw new ArgumentOutOfRangeException(
                    $"{gameObject.name} was asked to move when it had no moves remaining!");
            }

            HexMap map = MapManager.Instance.Map;
            map.Step(Coord, MoveSteps[MoveIndex], this);
            MoveIndex++;
        }
    }
}
