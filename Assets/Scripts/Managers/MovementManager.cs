using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnholyTrinity.Helpers;
using UnholyTrinity.Hex.Interfaces;
using UnityEngine;

namespace UnholyTrinity.Managers
{
    [RequireComponent(typeof(GameManager))]
    public class MovementManager : Singleton<MovementManager>
    {
        private float TickDuration => TurnManager.Instance.TickDuration;

        [SerializeField] [Min(0.1f)] [MaxValue(nameof(TickDuration))]
        private float moveDurationSec;

        public float ActionDuration => moveDurationSec;

        private readonly HashSet<IMovable> _movingObjects = new();

        protected override void Awake()
        {
            base.Awake();
            IMovable.RegisterMover += OnRegisterMover;
            IMovable.UnregisterMover += OnUnregisterMover;
        }

        private void OnDestroy()
        {
            IMovable.RegisterMover -= OnRegisterMover;
            IMovable.UnregisterMover -= OnUnregisterMover;
        }

        private void OnRegisterMover(IMovable obj)
        {
            _movingObjects.Add(obj);
        }

        private void OnUnregisterMover(IMovable obj)
        {
            _movingObjects.Remove(obj);
        }

        public IEnumerator HandleTick()
        {
            List<MoveData> movingObjects = _movingObjects.Select(mover => new MoveData(mover)).ToList();
            float maxSpeed = movingObjects.Max(mover => mover.RemainingMoves);

            for (int i = 0; i < movingObjects.Count; i++)
            {
                MoveData mover = movingObjects[i];
                mover.SetBaseRatio(mover.RemainingMoves / maxSpeed);
                movingObjects[i] = mover;
            }

            var delay = new WaitForSeconds(ActionDuration / maxSpeed);
            for (int i = 0; i < maxSpeed; i++)
            {
                foreach (var mover in movingObjects)
                {
                    mover.IncrementRatio();
                    if (!mover.ShouldMove)
                    {
                        continue;
                    }

                    mover.MoveNext();
                }

                movingObjects.RemoveAll(mover => !mover.CanMove);
                var sharedTiles = movingObjects
                    .GroupBy(mover => mover.Coord)
                    .Where(group => group.Count() > 1)
                    .Select(group => new
                        { Element = group.Key, Counter = group.Count() })
                    .ToList();

                yield return delay;
            }

            //Due to the ratio math, sometimes we have stragglers. Catch them here.
            // [CB] I find it amusing that the slow ones get caught here :)
            while (movingObjects.Any(mover => mover.CanMove))
            {
                foreach (var mover in
                         movingObjects.Where(mover => mover.CanMove))
                {
                    mover.MoveNext();
                }

                movingObjects.RemoveAll(mover => !mover.CanMove);
                yield return delay;
            }
        }
    }
}
