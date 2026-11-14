using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnholyTrinity.Helpers;
using UnholyTrinity.Managers.Interfaces;
using UnityEngine;

namespace UnholyTrinity.Managers
{
    [RequireComponent(typeof(GameManager))]
    public class MovementManager : Singleton<MovementManager>,
        ITickManager<SAMType, List<MoveData>>
    {
        public SAMType SAMType => SAMType.Move;

        private float _tickDuration => TurnManager.Instance.TickDuration;

        [SerializeField] [Min(0.1f)] [MaxValue(nameof(_tickDuration))]
        private float moveDurationSec;

        public float ActionDuration => moveDurationSec;

        public IEnumerator HandleTick(List<MoveData> movingObjects)
        {
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
                        Debug.Log($"{mover.Name} is waiting to move: {mover._currentRatio}");
                        continue;
                    }
                    Debug.Log($"{mover.Name} is moving.");
                    mover.MoveNext();
                }

                movingObjects.RemoveAll(mover => !mover.CanMove);
                yield return delay;
            }

            //Due to the ratio math, sometimes we have stragglers. Catch them here.
            // [CB] I find it amusing that the slow ones get caught here :)
            while (movingObjects.Any(mover => mover.CanMove))
            {
                foreach (var mover in
                         movingObjects.Where(mover => mover.CanMove))
                {
                    Debug.Log($"{mover.Name} had leftover moves!");
                    mover.MoveNext();
                }

                yield return delay;
            }
        }
    }
}