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
    private float TickDuration => TickData.TickDuration;

    [SerializeField] [Min(0.1f)] [MaxValue(nameof(TickDuration))]
    private float moveDurationSec;

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
        //Allow movers to do any required setup
        foreach (var movingObject in _movingObjects)
        {
            movingObject.BeginMovePhase();
        }
        //Convert to MoveData
        List<MoveData> movingObjects = _movingObjects.Select(mover => new MoveData(mover)).ToList();
        //Get max value for calculating ratios
        float maxSpeed = movingObjects.Max(mover => mover.RemainingMoves);

        //Set ratio
        for (int i = 0; i < movingObjects.Count; i++)
        {
            MoveData mover = movingObjects[i];
            mover.SetBaseRatio(mover.RemainingMoves / maxSpeed);
            mover.Mover.BeginMovePhase();
            movingObjects[i] = mover;
        }

        var delay = new WaitForSeconds(moveDurationSec / maxSpeed);
        //keep moving while anything can move
        while(movingObjects.Any(mover => mover.CanMove))
        {
            for (int j = 0; j < movingObjects.Count; j++)
            {
                var mover = movingObjects[j];
                mover.IncrementRatio();
                if (!mover.ShouldMove)
                {
                    continue;
                }

                mover.MoveNext();
                movingObjects[j] = mover;
            }
            yield return delay;
        }

        Debug.Log("Allow for movers to cleanup: ");
        //Allow for cleanup
        foreach (var movingObject in _movingObjects)
        {
            movingObject.EndMovePhase();
        }
    }
}
}
