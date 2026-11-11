using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NaughtyAttributes;
using UnityEngine;

namespace Managers
{
    [RequireComponent(typeof(GameManager))]
    public class MovementManager : MonoBehaviour,
        ITickManager<SAMType, HashSet<MoveData>>
    {
        public SAMType SAMType => SAMType.Move;

        private float _tickDuration => TurnManager.Instance.TickDuration;

        [SerializeField] [Min(0.1f)] [MaxValue(nameof(_tickDuration))]
        private float moveDurationSec;

        public float ActionDuration => moveDurationSec;

        public IEnumerator HandleTick(HashSet<MoveData> movingObjects)
        {
            float maxSpeed = movingObjects.Max(mover => mover.RemainingMoves);
            foreach (MoveData mover in movingObjects)
            {
                mover.SetBaseRatio(mover.RemainingMoves / maxSpeed);
            }

            var delay = new WaitForSeconds(ActionDuration / maxSpeed);
            for (int i = 0; i < maxSpeed; i++)
            {
                foreach (MoveData mover in movingObjects)
                {
                    mover.IncrementRatio();
                    if (!mover.ShouldMove) continue;
                    mover.MoveNext();
                }

                movingObjects.RemoveWhere(mover => !mover.CanMove);
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

                yield return delay;
            }
        }
    }
}